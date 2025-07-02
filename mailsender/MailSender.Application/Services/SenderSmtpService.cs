using AutoMapper;
using MailSender.Application.Services.AbstractClasses;
using MailSender.Domain.Configuration;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.Enums;
using MailSender.Domain.Exceptions;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Application.Services
{
    public class SenderSmtpService : SenderProtocolAbstractService
    {
        public override EmailSenderTypeEnum Type => EmailSenderTypeEnum.SMTP;

        private readonly MailSettings _mailSettings;
        private readonly SMTPSettings _smtpSettings;

        public SenderSmtpService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IOptions<MailSettings> mailSettings,
            IOptions<SMTPSettings> smtpSettings) : base(mapper, unitOfWork)
        {
            _mailSettings = mailSettings.Value;
            _smtpSettings = smtpSettings.Value;
        }

        private Task<Attachment[]> GetAttachments(MailAttachmentDTO[] mailAttachments)
        {
            List<Attachment> attachmentList = new();

            List<string> attachmentsError = new();

            if (mailAttachments?.Any() == true)
            {
                try
                {
                    foreach (MailAttachmentDTO attachment in mailAttachments)
                    {
                        byte[] file = Convert.FromBase64String(attachment.File.FileBody);

                        attachmentList.Add(new Attachment(new MemoryStream(file), $"{attachment.File.FileName}.{attachment.File.FileExt}"));
                    }
                }
                catch (Exception ex)
                {
                    attachmentsError.Add(ex.Message);
                }
            }

            if (attachmentsError.Any())
            {
                throw new ValidationException(string.Join(";", attachmentsError), codeResult: ApplicationConstants.ATFNO);
            }

            return Task.FromResult(attachmentList.ToArray());
        }

        private async Task SendSMTP(
            bool isReserve,
            Guid messageId,
            string subject,
            string body,
            bool isImportant,
            string[] emails,
            Attachment[] attachments,
            CancellationToken cancellationToken)
        {
            var retryCount = _mailSettings.RetryCount > 0 ? _mailSettings.RetryCount : 1;
            var retryDelay = _mailSettings.RetryDelay > 0 ? _mailSettings.RetryDelay : 500;

            var settings = isReserve ? _smtpSettings.Reserve : _smtpSettings.Common;

            using MailMessage msg = new()
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
                Priority = isImportant ? MailPriority.High : MailPriority.Normal
            };

            msg.From = new MailAddress(settings.From, settings.FromDisplayAddress);

            if (attachments != null)
            {
                foreach (Attachment attachment in attachments)
                {
                    msg.Attachments.Add(attachment);
                }
            }

            foreach (var email in emails)
            {
                msg.To.Add(new MailAddress(email));
            }

            while (retryCount > 0)
            {
                try
                {
                    using SmtpClient smtp = new(settings.Server, settings.Port);

                    if (settings.UseDefaultCredentials)
                    {
                        smtp.UseDefaultCredentials = settings.UseDefaultCredentials;
                    }
                    else
                    {
                        smtp.UseDefaultCredentials = settings.UseDefaultCredentials;
                        smtp.Credentials = new System.Net.NetworkCredential(settings.UserName, settings.Password);
                    }

                    await smtp.SendMailAsync(msg, cancellationToken: cancellationToken);

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
                Attachment[] attachments = await GetAttachments(model.Attachments);

                await base.Process(messageId, cancellationToken: cancellationToken);

                (var commonEmails, var reserveEmails) = _smtpSettings.GetSeparatedEmails(model.ValidatedEmails);

                if (commonEmails.Any())
                {
                    await base.WriteStatus(
                        messageId,
                        StatusEnum.StartSendedViaCommon,
                        string.Join(ApplicationConstants.EmailSeparate, commonEmails),
                        cancellationToken);

                    await SendSMTP(
                        isReserve: false,
                        messageId,
                        model.Title,
                        model.Body,
                        model.IsImportant,
                        commonEmails,
                        attachments,
                        cancellationToken);

                    await base.WriteStatus(
                        messageId,
                        StatusEnum.EndSendedViaCommon,
                        null,
                        cancellationToken);
                }

                if (reserveEmails.Any())
                {
                    await base.WriteStatus(
                        messageId, 
                        StatusEnum.StartSendedViaReserve, 
                        string.Join(ApplicationConstants.EmailSeparate, reserveEmails),
                        cancellationToken);

                    await SendSMTP(
                        isReserve: true,
                        messageId,
                        model.Title,
                        model.Body,
                        model.IsImportant,
                        reserveEmails,
                        attachments,
                        cancellationToken);

                    await base.WriteStatus(
                        messageId,
                        StatusEnum.EndSendedViaReserve,
                        null,
                        cancellationToken);
                }

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