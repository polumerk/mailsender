using System.Linq;
using MailSender.Domain.Entities;

namespace MailSender.Infrastructure.Repositories.Interfaces
{
    public interface IStatusRepository : IBaseRepository<Status>
    {
        public IQueryable<Status> GetQueryData();
    }
}