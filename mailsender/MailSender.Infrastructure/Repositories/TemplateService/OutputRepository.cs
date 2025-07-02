using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.Entities.TemplateService;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Repositories.TemplateService
{
    public class OutputRepository : BaseTemplateServiceRepository<Output>, IOutputRepository
    {
        public OutputRepository(TemplateServiceContext context) : base(context) { }

        public Task<Output> GetByMessageId(Guid messageId, CancellationToken cancellationToken)
        {
            return _context.Outputs
                .Include(x => x.ContentInfo)
                .Where(x => x.MessageId.Equals(messageId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public Task<Output[]> GetByMessageIds(Guid[] messageIds, CancellationToken cancellationToken)
        {
            return _context.Outputs
                .Include(x => x.ContentInfo)
                .Where(x => messageIds.Contains(x.MessageId))
                .ToArrayAsync(cancellationToken);
        }
    }
}