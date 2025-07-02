using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Domain.Entities
{
    public class Message
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public Guid? TemplateMessageId { get; set; }

        public string Email { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public string Attachments { get; set; }
        public bool IsImportant { get; set; }
        public string OriginMessageInfo { get; set; }

        public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
    }
}