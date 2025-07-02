using System;
using System.Linq;
using AutoMapper;
using Dictionaries;
using Dictionaries.Attributes;
using Dictionaries.DTO;
using Dictionaries.Extensions;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.DTOs.Broker.Send;
using MailSender.Domain.DTOs.Broker.Send.Internal;
using MailSender.Domain.DTOs.Broker.TemplateService;

namespace MailSender.Application.Services
{
    public class BrokerMessageCreatorService : IBrokerMessageCreatorService
    {
        private readonly IMapper _mapper;

        public BrokerMessageCreatorService(IMapper mapper)
        {
            _mapper = mapper;
        }

        public KafkaMessageDTO<ErrorDataDTO> CreateError(Exception ex)
        {
            var newid = Guid.NewGuid();

            MessageInfoDTO messageInfo = new()
            {
                MessageId = newid,
                ParentMessageId = null,
                MasterMessageId = newid,
                SystemSource = ApplicationConstants.ApplicationSystemSource,
                DateTimeCreate = DateTime.UtcNow,
                ActionType = ActionTypes.INS,
                MessageType = ApplicationConstants.Error,
                JsonVersion = AttributeExtensions.GetAttributeValue<ErrorDataDTO, VersionAttribute>().Version
            };

            ErrorDataDTO data = new()
            {
                ErrorCode = ex.GetResultCodeFromException(),
                SystemSource = ApplicationConstants.ApplicationSystemSource,
                DateTimeCreate = DateTime.UtcNow,
                ErrorDescription = ex.GetMessageFromException(),
                ErrorCodeExternal = null,
                CrmClientId = null,
                UserId = null
            };

            return new KafkaMessageDTO<ErrorDataDTO>
            {
                MessageInfo = messageInfo,
                Data = data
            };
        }

        public KafkaMessageDTO<ErrorDataDTO> CreateError(KafkaMessageDTO<MailSenderDataDTO> model, Exception ex)
        {
            MessageInfoDTO messageInfo = _mapper.Map<MessageInfoDTO>(model.MessageInfo);
            messageInfo.MessageType = ApplicationConstants.Error;
            messageInfo.JsonVersion = AttributeExtensions.GetAttributeValue<ErrorDataDTO, VersionAttribute>().Version;

            ErrorDataDTO data = new()
            {
                ErrorCode = ex.GetResultCodeFromException(),
                SystemSource = ApplicationConstants.ApplicationSystemSource,
                DateTimeCreate = DateTime.UtcNow,
                ErrorDescription = ex.GetMessageFromException(),
                ErrorCodeExternal = null,
                CrmClientId = null,
                UserId = null
            };

            return new KafkaMessageDTO<ErrorDataDTO>
            {
                MessageInfo = messageInfo,
                Data = data
            };
        }

        public KafkaMessageDTO<ErrorDataDTO> CreateError(KafkaMessageDTO<MailSenderDataDTO> model, string errorCode, string errorDescription)
        {
            MessageInfoDTO messageInfo = _mapper.Map<MessageInfoDTO>(model.MessageInfo);
            messageInfo.MessageType = ApplicationConstants.Error;
            messageInfo.JsonVersion = AttributeExtensions.GetAttributeValue<ErrorDataDTO, VersionAttribute>().Version;

            ErrorDataDTO data = new()
            {
                ErrorCode = errorCode,
                SystemSource = ApplicationConstants.ApplicationSystemSource,
                DateTimeCreate = DateTime.UtcNow,
                ErrorDescription = errorDescription,
                ErrorCodeExternal = null,
                CrmClientId = null,
                UserId = null
            };

            return new KafkaMessageDTO<ErrorDataDTO>
            {
                MessageInfo = messageInfo,
                Data = data
            };
        }

        public KafkaMessageDTO<NotificationEmailDataDTO> CreateNotification(KafkaMessageDTO<MailSenderDataDTO> model, MailInfoDTO mailInfo = null, Exception ex = null)
        {
            MessageInfoDTO messageInfo = _mapper.Map<MessageInfoDTO>(model.MessageInfo);
            messageInfo.MessageType = ApplicationConstants.Notification;
            messageInfo.JsonVersion = AttributeExtensions.GetAttributeValue<NotificationEmailDataDTO, VersionAttribute>().Version;

            NotificationEmailDataDTO data = _mapper.Map<NotificationEmailDataDTO>(model.Data);

            if (data?.Content?.Attachments?.Any() == true)
            {
                foreach (var attach in data.Content.Attachments)
                {
                    if (attach?.File?.FileBody != null)
                    {
                        attach.File.FileBody = null;
                    }
                }
            }

            if (ex != null)
            {
                data.ResultCode = ex.GetResultCodeFromException();

                if (data.ResultCode.Equals(CodeResults.SYSER))
                {
                    data.ResultDescription = null;
                }
                else
                {
                    data.ResultDescription = ex.GetMessageFromException();
                }
            }
            else
            {
                data.ResultCode = ApplicationConstants.SUCCESS;
                data.ResultDescription = "Сообщение отправлено";
                data.Content.Title = mailInfo.Title;
                data.Content.Message = mailInfo.Body;
            }

            return new KafkaMessageDTO<NotificationEmailDataDTO>
            {
                MessageInfo = messageInfo,
                Data = data
            };
        }

        public KafkaMessageDTO<NotificationEmailDataDTO> CreateNotification(KafkaMessageDTO<MailSenderDataDTO> model, string errorCode, string errorDescription)
        {
            MessageInfoDTO messageInfo = _mapper.Map<MessageInfoDTO>(model.MessageInfo);
            messageInfo.MessageType = ApplicationConstants.Notification;
            messageInfo.JsonVersion = AttributeExtensions.GetAttributeValue<NotificationEmailDataDTO, VersionAttribute>().Version;

            NotificationEmailDataDTO data = _mapper.Map<NotificationEmailDataDTO>(model.Data);

            if (data?.Content?.Attachments?.Any() == true)
            {
                foreach (var attach in data.Content.Attachments)
                {
                    if (attach?.File?.FileBody != null)
                    {
                        attach.File.FileBody = null;
                    }
                }
            }

            data.ResultCode = errorCode;
            data.ResultDescription = errorDescription;

            return new KafkaMessageDTO<NotificationEmailDataDTO>
            {
                MessageInfo = messageInfo,
                Data = data
            };
        }

        public KafkaMessageDTO<TemplateServiceDTO> CreateTemplateService(KafkaMessageDTO<MailSenderDataDTO> model)
        {
            MessageInfoDTO messageInfo = _mapper.Map<MessageInfoDTO>(model.MessageInfo);
            messageInfo.SystemSource = ApplicationConstants.ApplicationSystemSource;
            messageInfo.MessageType = ApplicationConstants.TemplateService;
            messageInfo.JsonVersion = AttributeExtensions.GetAttributeValue<TemplateServiceDTO, VersionAttribute>().Version;

            TemplateServiceDTO data = _mapper.Map<TemplateServiceDTO>(model.Data);

            return new KafkaMessageDTO<TemplateServiceDTO>
            {
                MessageInfo = messageInfo,
                Data = data
            };
        }
    }
}