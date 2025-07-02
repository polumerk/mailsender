using System;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Domain.Entities.TemplateService
{
    public class ContentInfo
    {
        [Key]
        public Guid Id { get; set; }

        public byte[] Content { get; set; }
    }
}