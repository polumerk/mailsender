using System;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Domain.Entities
{
    public class StatusHistory
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        public string Description { get; set; }
        public Guid MessageId { get; set; }
        public int StatusId { get; set; }
        public Status Status { get; set; }
    }
}