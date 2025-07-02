using MailSender.Domain.Entities;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Repositories.Interfaces;

namespace MailSender.Infrastructure.Repositories.MailSender
{
    public class StatusHistoryRepository : BaseRepository<StatusHistory>, IStatusHistoryRepository
    {
        public StatusHistoryRepository(ApplicationContext context) : base(context) { }
    }
}