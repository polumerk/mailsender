using System;
using System.Collections.Generic;
using Dictionaries.Attributes;
using MailSender.Domain.Enums;

namespace MailSender.Domain.DTOs.Broker.TemplateService
{
    [Version("1.0.0")]
    public class TemplateServiceDTO
    {
        public string ResultCode { get; set; }
        public string ResultDescription { get; set; }
        public Guid? Id { get; set; }
        public int? Version { get; set; }
        public string Title { get; set; }
        public string Extension { get; set; }
        public TypeEnum[] AccessableTypes { get; set; }
        public Dictionary<string, object> ReplaceFields { get; set; }
    }
}