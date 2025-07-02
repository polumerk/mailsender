using MailSender.Domain.DTOs.Broker.Receive;

namespace MailSender.Application.Services.Interfaces
{
    public interface IHashService
    {
        public string Hash(MailSenderDataDTO model);
    }
}