using System.Linq;
using MailSender.Domain.Entities;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Repositories.Interfaces;

namespace MailSender.Infrastructure.Repositories.MailSender
{
    public class StatusRepository : BaseRepository<Status>, IStatusRepository
    {
        public StatusRepository(ApplicationContext context) : base(context) { }

        public IQueryable<Status> GetQueryData()
        {
            return _context.Statuses;
        }
    }
}