using System;

namespace MailSender.Domain.DTOs.EntitiesDTO
{
    public class WhiteListDTO
    {
        public Guid? Id { get; set; }
        public string Email { get; set; }
        public string Description { get; set; }
        public DateTime CreateDate { get; set; }
    }
}