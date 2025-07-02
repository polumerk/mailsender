using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.DTOs.Broker;

namespace MailSender.Application.Services.Interfaces
{
    public interface IOrchestratorService
    {
        public Task<BrokerMessage> Process(string message, CancellationToken cancellationToken);
        public Task<BrokerMessage> TemplateServiceProcess(string message, CancellationToken cancellationToken);
    }
}