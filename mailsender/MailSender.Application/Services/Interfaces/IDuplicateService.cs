using MailSender.Domain.DTOs.Broker.Receive;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Application.Services.Interfaces
{
    public interface IDuplicateService
    {
        public Task CheckDuplicate(MailSenderDataDTO model, CancellationToken cancellationToken);
    }
}