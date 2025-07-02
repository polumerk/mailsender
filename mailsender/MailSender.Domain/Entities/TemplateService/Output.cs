using System;
using System.ComponentModel.DataAnnotations;

namespace MailSender.Domain.Entities.TemplateService
{
    /// <summary>
    /// Сообщения, которые были сконвертированы
    /// </summary>
    public class Output
    {
        [Key]
        public Guid Id { get; set; }
        public DateTime CreateDate { get; set; } = DateTime.UtcNow;

        public Guid MessageId { get; set; }
        public Guid? ContentInfoId { get; set; }
        public ContentInfo ContentInfo { get; set; }
    }
}