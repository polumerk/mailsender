using MailSender.Domain.Entities;
using MailSender.Infrastructure.Repositories.Interfaces;
using MailSender.TestConstants.MockServices.Interfaces;
using static MailSender.TestConstants.Constants;

namespace MailSender.TestConstants.MockServices
{
    public class DatabaseSeedService : IDatabaseSeedService
    {
        private readonly IUnitOfWork _unitOfWork;

        public DatabaseSeedService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task SeedData(CancellationToken cancellationToken = default)
        {
            await _unitOfWork.MailServiceChangeTrackerClear();

            await _unitOfWork.WhiteLists.AddRange(new[]
            {
                new WhiteList { Id = Guid.NewGuid(), Email = KafkaMessageTest.WhiteListEmail}
            }, cancellationToken);

            await _unitOfWork.MailServiceComplete(cancellationToken);
        }
    }
}