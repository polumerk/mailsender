using Dictionaries.Attributes;
using MailSender.Domain.DTOs.Broker.Receive;

namespace MailSender.Domain.DTOs.Broker.Send
{
    [Version("1.0.0")]
    public class NotificationEmailContentDataDTO
    {
        public string Email { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public MailAttachmentDTO[] Attachments { get; set; }
    }
}