namespace MailSender.TestConstants.MockServices.Interfaces
{
    public interface IDatabaseSeedService
    {
        public Task SeedData(CancellationToken cancellationToken = default);
    }
}
