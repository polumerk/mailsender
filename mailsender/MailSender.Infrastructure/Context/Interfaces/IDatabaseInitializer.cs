using System.Threading.Tasks;

namespace MailSender.Infrastructure.Context.Interfaces
{
    public interface IDatabaseInitializer
    {
        public Task InitialiseAsync();
    }
}
