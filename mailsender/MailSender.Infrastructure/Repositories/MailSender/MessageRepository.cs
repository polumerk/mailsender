using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.Entities;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MailSender.Infrastructure.Repositories.MailSender
{
    public class MessageRepository : BaseRepository<Message>, IMessageRepository
    {
        public MessageRepository(ApplicationContext context) : base(context) { }

        public new async Task<Message> GetById(Guid id, CancellationToken cancellationToken)
        {
            return
                await GetQueryData()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Message> GetByTemplateMessageId(Guid messageId, CancellationToken cancellationToken)
        {
            return
               await GetQueryData()
                .Where(x => x.TemplateMessageId.Equals(messageId))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public IQueryable<Message> GetQueryData()
        {
            return _context.Messages
                .Include(x => x.StatusHistories)
                    .ThenInclude(x => x.Status);
        }
    }
}