using Dictionaries.Attributes;

namespace MailSender.Domain.DTOs.Broker.Send
{
    [Version("1.0.0")]
    public class NotificationEmailDataDTO
    {
        public string NotificationType { get; set; }
        public string ResultCode { get; set; }
        public string ResultDescription { get; set; }
        public NotificationEmailContentDataDTO Content { get; set; }
    }
}