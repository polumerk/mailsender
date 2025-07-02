using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MailSender.Infrastructure.Repositories.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        DatabaseFacade ContextDatabase { get; }
        IWhiteListRepository WhiteLists { get; }
        IMessageRepository Messages { get; }
        IStatusRepository Statuses { get; }
        IStatusHistoryRepository StatusHistory { get; }
        IOutputRepository Outputs { get; }

        public Task<int> MailServiceComplete(CancellationToken cancellationToken);
        public Task MailServiceChangeTrackerClear();
        public Task<int> TemplateServiceComplete(CancellationToken cancellationToken);
        public Task TemplateServiceChangeTrackerClear();
    }
}