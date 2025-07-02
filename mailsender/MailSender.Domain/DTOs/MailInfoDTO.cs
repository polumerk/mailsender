using MailSender.Domain.DTOs.Broker.Receive;

namespace MailSender.Domain.DTOs
{
    public class MailInfoDTO
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public string Email { get; set; }
        public string[] ValidatedEmails { get; set; }
        public bool IsImportant { get; set; }
        public MailAttachmentDTO[] Attachments { get; set; }
    }
}