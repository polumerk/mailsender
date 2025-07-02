using System;
using Dictionaries.Attributes;

namespace MailSender.Domain.DTOs.Broker.Send.Internal
{
    [Version("1.0.0")]
    public class ErrorDataDTO
    {
        public string ErrorCode { get; set; }
        public string SystemSource { get; set; }
        public DateTime DateTimeCreate { get; set; }
        public string ErrorDescription { get; set; }
        public string ErrorCodeExternal { get; set; }
        public int? CrmClientId { get; set; }
        public string UserId { get; set; }
    }
}