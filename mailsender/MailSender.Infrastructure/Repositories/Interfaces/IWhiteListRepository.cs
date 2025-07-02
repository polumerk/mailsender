using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.Entities;

namespace MailSender.Infrastructure.Repositories.Interfaces
{
    public interface IWhiteListRepository : IBaseRepository<WhiteList>
    {
        public Task<WhiteList> GetByEmail(string email, CancellationToken cancellationToken);
        public Task<WhiteList[]> GetByIds(Guid[] ids, CancellationToken cancellationToken);
        public new Task<WhiteList> GetById(Guid id, CancellationToken cancellationToken);
        public IQueryable<WhiteList> GetQueryData();
    }
}