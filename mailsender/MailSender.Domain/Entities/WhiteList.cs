using System;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Domain.Entities
{
    public class WhiteList
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;
        [MaxLength(256)]
        public string Email { get; set; }
        [MaxLength(512)]
        public string Description { get; set; }
        public bool IsDeleted { get; set; } = false;
    }
}