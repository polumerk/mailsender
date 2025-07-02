using System;
using System.Collections.Generic;
using Dictionaries.Attributes;

namespace MailSender.Domain.DTOs.Broker.Receive
{
    [Version("1.0.2")]
    public class MailSenderDataDTO
    {
        public Guid? MailTemplateId { get; set; }
        public string MailTemplateTitle { get; set; }
        public Dictionary<string, object> ReplaceFields { get; set; }
        public int? Version { get; set; }
        public string Email { get; set; }
        public bool? IsImportant { get; set; }
        public MailAttachmentDTO[] Attachments { get; set; }
    }
}