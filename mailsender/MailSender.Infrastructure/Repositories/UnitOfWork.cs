using System.Threading;
using System.Threading.Tasks;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Repositories.Interfaces;
using MailSender.Infrastructure.Repositories.MailSender;
using MailSender.Infrastructure.Repositories.TemplateService;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace MailSender.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationContext _context;
        private readonly TemplateServiceContext _templateServiceContext;

        public UnitOfWork(ApplicationContext context, TemplateServiceContext templateServiceContext)
        {
            _context = context;
            _templateServiceContext = templateServiceContext;

            WhiteLists = new WhiteListRepository(_context);
            Messages = new MessageRepository(_context);
            Statuses = new StatusRepository(_context);
            StatusHistory = new StatusHistoryRepository(_context);
            Outputs = new OutputRepository(_templateServiceContext);
        }
        public DatabaseFacade ContextDatabase => _context.Database;
        public IWhiteListRepository WhiteLists { get; }
        public IMessageRepository Messages { get; }
        public IStatusRepository Statuses { get; }
        public IStatusHistoryRepository StatusHistory { get; }

        public IOutputRepository Outputs { get; }

        public Task<int> MailServiceComplete(CancellationToken cancellationToken)
        {
            return _context.SaveChangesAsync(cancellationToken);
        }

        public Task MailServiceChangeTrackerClear()
        {
            _context.ChangeTracker.Clear();

            return Task.CompletedTask;
        }

        public Task<int> TemplateServiceComplete(CancellationToken cancellationToken)
        {
            return _templateServiceContext.SaveChangesAsync(cancellationToken);
        }

        public Task TemplateServiceChangeTrackerClear()
        {
            _templateServiceContext.ChangeTracker.Clear();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}