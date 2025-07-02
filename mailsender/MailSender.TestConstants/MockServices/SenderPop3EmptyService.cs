using AutoMapper;
using Dictionaries;
using MailSender.Application.Services.AbstractClasses;
using MailSender.Domain.Configuration;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs;
using MailSender.Domain.Enums;
using MailSender.Domain.Exceptions;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace MailSender.TestConstants.MockServices
{
    public class SenderPop3EmptyService : SenderProtocolAbstractService
    {
        public override EmailSenderTypeEnum Type => EmailSenderTypeEnum.POP3;
        private readonly MailSettings _mailSettings;

        public SenderPop3EmptyService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IOptions<MailSettings> mailSettings) : base(mapper, unitOfWork)
        {
            _mailSettings = mailSettings.Value;
        }

        private async Task Action(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken)
        {
            var retryCount = _mailSettings.RetryCount > 0 ? _mailSettings.RetryCount : 1;
            var retryDelay = _mailSettings.RetryDelay > 0 ? _mailSettings.RetryDelay : 500;

            while (retryCount > 0)
            {
                try
                {
                    if (model.Title.Contains(CodeResults.SYSER))
                    {
                        throw new Exception(CodeResults.SYSER);
                    }

                    break;
                }
                catch (Exception ex)
                {
                    retryCount--;

                    if (retryCount == 0)
                    {
                        throw new ValidationException(ex.Message, innerException: ex?.InnerException ?? ex, codeResult: ApplicationConstants.SMTP);
                    }

                    await base.RetryWork(messageId, ex, cancellationToken);

                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
        }

        public override async Task Send(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken)
        {
            await base.PreProcess(model, messageId, cancellationToken: cancellationToken);

            try
            {
                await base.Process(messageId, cancellationToken: cancellationToken);

                await Action(model, messageId, cancellationToken: cancellationToken);

                await base.PostProcess(messageId, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                await base.PostProcess(messageId, ex, cancellationToken: cancellationToken);
                throw;
            }
        }
    }
}