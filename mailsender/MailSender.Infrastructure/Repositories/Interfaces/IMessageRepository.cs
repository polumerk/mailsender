using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.Entities;

namespace MailSender.Infrastructure.Repositories.Interfaces
{
    public interface IMessageRepository : IBaseRepository<Message>
    {
        public IQueryable<Message> GetQueryData();
        public Task<Message> GetByTemplateMessageId(Guid messageId, CancellationToken cancellationToken);
    }
}