using System;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.Entities.TemplateService;

namespace MailSender.Infrastructure.Repositories.Interfaces
{
    public interface IOutputRepository : IBaseRepository<Output>
    {
        public Task<Output> GetByMessageId(Guid messageId, CancellationToken cancellationToken);
        public Task<Output[]> GetByMessageIds(Guid[] messageIds, CancellationToken cancellationToken);
    }
}