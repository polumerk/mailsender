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
    public class WhiteListRepository : BaseRepository<WhiteList>, IWhiteListRepository
    {
        public WhiteListRepository(ApplicationContext context) : base(context) { }

        public async Task<WhiteList> GetByEmail(string email, CancellationToken cancellationToken)
        {
            return
                await GetQueryData()
                .Where(x => x.Email.Equals(email))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<WhiteList[]> GetByIds(Guid[] ids, CancellationToken cancellationToken)
        {
            return
                await GetQueryData()
                .Where(x => ids.Contains(x.Id))
                .ToArrayAsync(cancellationToken);
        }

        public new async Task<WhiteList> GetById(Guid id, CancellationToken cancellationToken)
        {
            return
                await GetQueryData()
                .Where(x => x.Id.Equals(id))
                .FirstOrDefaultAsync(cancellationToken);
        }

        public IQueryable<WhiteList> GetQueryData()
        {
            return _context.WhiteList
                .Where(x => !x.IsDeleted);
        }
    }
}