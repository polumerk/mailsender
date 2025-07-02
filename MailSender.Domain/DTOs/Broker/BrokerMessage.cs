using Dictionaries.DTO;
using MailSender.Domain.DTOs.Broker.Send;
using MailSender.Domain.DTOs.Broker.Send.Internal;
using MailSender.Domain.DTOs.Broker.TemplateService;

namespace MailSender.Domain.DTOs.Broker
{
    public class BrokerMessage
    {
        public KafkaMessageDTO<TemplateServiceDTO> TemplateNotification { get; set; }
        public KafkaMessageDTO<NotificationEmailDataDTO> Notification { get; set; }
        public KafkaMessageDTO<ErrorDataDTO> Error { get; set; }

        public string SystemSource { get; set; }
    }
}