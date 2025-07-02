using System;
using Dictionaries.DTO;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.DTOs.Broker.Send;
using MailSender.Domain.DTOs.Broker.Send.Internal;
using MailSender.Domain.DTOs.Broker.TemplateService;

namespace MailSender.Application.Services.Interfaces
{
    public interface IBrokerMessageCreatorService
    {
        public KafkaMessageDTO<ErrorDataDTO> CreateError(Exception ex);
        public KafkaMessageDTO<ErrorDataDTO> CreateError(KafkaMessageDTO<MailSenderDataDTO> model, Exception ex);
        public KafkaMessageDTO<ErrorDataDTO> CreateError(KafkaMessageDTO<MailSenderDataDTO> model, string errorCode, string errorDescription);
        public KafkaMessageDTO<NotificationEmailDataDTO> CreateNotification(KafkaMessageDTO<MailSenderDataDTO> model, MailInfoDTO mailInfo = null, Exception ex = null);
        public KafkaMessageDTO<TemplateServiceDTO> CreateTemplateService(KafkaMessageDTO<MailSenderDataDTO> model);
        public KafkaMessageDTO<NotificationEmailDataDTO> CreateNotification(KafkaMessageDTO<MailSenderDataDTO> model, string errorCode, string errorDescription);
    }
}