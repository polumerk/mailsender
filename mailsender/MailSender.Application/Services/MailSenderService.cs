using AutoMapper;
using Dictionaries;
using Dictionaries.DTO;
using Dictionaries.Extensions;
using MailSender.Application.Services.AbstractClasses;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Configuration;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.Broker;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.DTOs.Broker.TemplateService;
using MailSender.Domain.Entities;
using MailSender.Domain.Enums;
using MailSender.Domain.Exceptions;
using MailSender.Domain.Localization;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Application.Services
{
    public class MailSenderService : IMailSenderService
    {
        private readonly IBrokerMessageCreatorService _brokerMessageCreatorService;
        private readonly IWhiteListService _whiteListService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly SenderProtocolAbstractService _senderService;
        private readonly IDuplicateService _duplicateService;
        private readonly MailSettings _mailSettings;
        private readonly ILogger<MailSenderService> _logger;

        public MailSenderService(
            IBrokerMessageCreatorService brokerMessageCreatorService,
            IWhiteListService whiteListService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            SenderProtocolAbstractService senderService,
            IDuplicateService duplicateService,
            IOptions<MailSettings> mailSettingsOptions,
            ILogger<MailSenderService> logger)
        {
            _brokerMessageCreatorService = brokerMessageCreatorService;
            _whiteListService = whiteListService;
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _senderService = senderService;
            _duplicateService = duplicateService;
            _mailSettings = mailSettingsOptions.Value;
            _logger = logger;
        }

        private string[] GetEmails(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                return email.Split(ApplicationConstants.EmailSeparate, StringSplitOptions.TrimEntries);
            }

            throw new ValidationException(CultureLoc.MailIsEmpty, codeResult: ApplicationConstants.EMEMT);
        }

        private async Task<string[]> ValidationChecker(string email, CancellationToken cancellationToken)
        {
            string[] emails = GetEmails(email);
            await _whiteListService.CheckWhiteList(emails, cancellationToken);

            return emails;
        }

        private async Task DuplicateChecker(MailSenderDataDTO model, CancellationToken cancellationToken)
        {
            await _duplicateService.CheckDuplicate(model, cancellationToken);
        }

        public async Task Send(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken)
        {
            string[] emails = await ValidationChecker(model.Email, cancellationToken);

            model.ValidatedEmails = emails;

            await _senderService.Send(model, messageId, cancellationToken);
        }

        public async Task<BrokerMessage> ProcessResponse(string message, CancellationToken cancellationToken)
        {
            KafkaMessageDTO<TemplateServiceDTO> templateModel;
            try
            {
                templateModel = JsonConvert.DeserializeObject<KafkaMessageDTO<TemplateServiceDTO>>(message);
                _logger.KafkaLogInformation(templateModel);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(message);
                return new BrokerMessage
                {
                    Error = _brokerMessageCreatorService.CreateError(ex)
                };
            }

            KafkaMessageDTO<MailSenderDataDTO> mailModel = new KafkaMessageDTO<MailSenderDataDTO>
            {
                Data = _mapper.Map<MailSenderDataDTO>(templateModel.Data)
            };
            try
            {
                var mailMessage = await _unitOfWork.Messages.GetByTemplateMessageId(templateModel.MessageInfo.ParentMessageId.Value, cancellationToken) ??
                    throw new NotFoundException(string.Format(CultureLoc.NotFoundException, nameof(Message), templateModel.MessageInfo.ParentMessageId.Value), codeResult: CodeResults.SYSER);

                mailModel.MessageInfo = JsonConvert.DeserializeObject<MessageInfoDTO>(mailMessage.OriginMessageInfo);
                mailModel.Data.Attachments = string.IsNullOrEmpty(mailMessage.Attachments) ?
                    null :
                    JsonConvert.DeserializeObject<MailAttachmentDTO[]>(mailMessage.Attachments);
                mailModel.Data.Email = mailMessage.Email;
                mailModel.Data.IsImportant = mailMessage.IsImportant;

                if (templateModel.Data.ResultCode != ApplicationConstants.SUCCESS)
                {
                    await _unitOfWork.StatusHistory.Add(new StatusHistory
                    {
                        StatusId = (int)StatusEnum.Error,
                        Description = $"{templateModel.Data.ResultCode}: {templateModel.Data.ResultDescription}",
                        MessageId = mailMessage.Id,
                    }, cancellationToken);

                    await _unitOfWork.MailServiceComplete(cancellationToken);

                    return new BrokerMessage
                    {
                        Notification = _brokerMessageCreatorService.CreateNotification(mailModel, templateModel.Data.ResultCode, templateModel.Data.ResultDescription),
                        Error = _brokerMessageCreatorService.CreateError(mailModel, templateModel.Data.ResultCode, templateModel.Data.ResultDescription),
                        SystemSource = mailModel.MessageInfo.SystemSource
                    };
                }

                var templateOutput = await _unitOfWork.Outputs.GetByMessageId(mailMessage.TemplateMessageId.Value, cancellationToken);

                var content = Encoding.UTF8.GetString(templateOutput.ContentInfo.Content);

                var prefix = string.IsNullOrWhiteSpace(_mailSettings.PrefixTitle) ? string.Empty : _mailSettings.PrefixTitle;

                string title = Regex.Match(content, @$"{ApplicationConstants.HtmlTitleOpenSymbol}\s*(?<Title>[\s\S]*?){ApplicationConstants.HtmlTitleCloseSymbol}", RegexOptions.IgnoreCase).Groups["Title"].Value;

                string body = content.Replace($"{ApplicationConstants.HtmlTitleOpenSymbol}{title}{ApplicationConstants.HtmlTitleCloseSymbol}", string.Empty, StringComparison.OrdinalIgnoreCase).Trim(Environment.NewLine.ToCharArray());

                var mailInfo = new MailInfoDTO
                {
                    Title = prefix + title,
                    Body = body,
                    IsImportant = mailMessage.IsImportant,
                    Attachments = mailModel.Data.Attachments,
                    Email = mailMessage.Email
                };

                await Send(mailInfo, mailMessage.Id, cancellationToken);

                return new BrokerMessage
                {
                    Notification = _brokerMessageCreatorService.CreateNotification(mailModel, mailInfo),
                    SystemSource = mailModel.MessageInfo.SystemSource
                };
            }
            catch (Exception ex)
            {
                return new BrokerMessage
                {
                    Notification = _brokerMessageCreatorService.CreateNotification(mailModel, null, ex),
                    Error = _brokerMessageCreatorService.CreateError(mailModel, ex),
                    SystemSource = mailModel.MessageInfo.SystemSource
                };
            }
        }
        
        public async Task<BrokerMessage> ProcessRequest(string message, CancellationToken cancellationToken)
        {
            KafkaMessageDTO<MailSenderDataDTO> model;
            try
            {
                model = JsonConvert.DeserializeObject<KafkaMessageDTO<MailSenderDataDTO>>(message);
                _logger.KafkaLogInformation(model);
            }
            catch (Exception ex)
            {
                _logger.LogInformation(message);
                return new BrokerMessage
                {
                    Error = _brokerMessageCreatorService.CreateError(ex)
                };
            }

            try
            {
                // Проверка Email
                try
                {
                    await ValidationChecker(model.Data.Email, cancellationToken);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.Messages.Add(new Message
                    {
                        TemplateMessageId = Guid.NewGuid(),
                        OriginMessageInfo = JsonConvert.SerializeObject(model.MessageInfo),
                        Email = model.Data?.Email,
                        Attachments = model.Data?.Attachments?.Any() == true ?
                            JsonConvert.SerializeObject(model.Data.Attachments) :
                            null,
                        IsImportant = model.Data?.IsImportant ?? false,
                        StatusHistories = new List<StatusHistory>{
                            new StatusHistory
                            {
                                StatusId = (int)StatusEnum.Created
                            },
                            new StatusHistory
                            {
                                StatusId = (int)StatusEnum.Error,
                                Description = ex.GetMessageFromException()
                            }
                        }
                    }, cancellationToken);

                    await _unitOfWork.MailServiceComplete(cancellationToken);

                    throw;
                }

                // Проверка на дубликаты
                try
                {
                    await DuplicateChecker(model.Data, cancellationToken);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.Messages.Add(new Message
                    {
                        TemplateMessageId = Guid.NewGuid(),
                        OriginMessageInfo = JsonConvert.SerializeObject(model.MessageInfo),
                        Email = model.Data?.Email,
                        Attachments = model.Data?.Attachments?.Any() == true ?
                            JsonConvert.SerializeObject(model.Data.Attachments) :
                            null,
                        IsImportant = model.Data?.IsImportant ?? false,
                        StatusHistories = new List<StatusHistory>{
                            new StatusHistory
                            {
                                StatusId = (int)StatusEnum.Created
                            },
                            new StatusHistory
                            {
                                StatusId = (int)StatusEnum.Duplicated
                            },
                            new StatusHistory
                            {
                                StatusId = (int)StatusEnum.Error,
                                Description = ex.GetMessageFromException()
                            }
                        }
                    }, cancellationToken);

                    await _unitOfWork.MailServiceComplete(cancellationToken);

                    throw;
                }

                // Начало обычной отправки после проверок
                var templateNotification = _brokerMessageCreatorService.CreateTemplateService(model);

                await _unitOfWork.Messages.Add(new Message
                {
                    TemplateMessageId = templateNotification.MessageInfo.MessageId,
                    OriginMessageInfo = JsonConvert.SerializeObject(model.MessageInfo),
                    Email = model.Data?.Email,
                    Attachments = model.Data?.Attachments?.Any() == true ? 
                        JsonConvert.SerializeObject(model.Data.Attachments) : 
                        null,
                    IsImportant = model.Data?.IsImportant ?? false,
                    StatusHistories = new List<StatusHistory>{
                        new StatusHistory
                        {
                            StatusId = (int)StatusEnum.Created
                        }
                    }
                }, cancellationToken);

                await _unitOfWork.MailServiceComplete(cancellationToken);

                return new BrokerMessage
                {
                    TemplateNotification = templateNotification
                };

            }
            catch (Exception ex)
            {
                return new BrokerMessage
                {
                    Notification = _brokerMessageCreatorService.CreateNotification(model, null, ex),
                    Error = _brokerMessageCreatorService.CreateError(model, ex),
                    SystemSource = model.MessageInfo.SystemSource
                };
            }
        }
    }
}