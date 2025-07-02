using MailSender.Application.Services.Interfaces;
using MailSender.Domain.DTOs.Broker.Receive;

namespace MailSender.UnitTests.Tests
{
    public class DuplicateEmptyServiceTests
    {
        private readonly IDuplicateService _duplicateService;

        public DuplicateEmptyServiceTests(
            IDuplicateService duplicateService)
        {
            _duplicateService = duplicateService;
        }

        [Theory]
        [InlineData(1, false)]
        [InlineData(2, false)]
        [InlineData(3, false)]
        [InlineData(4, true)]
        [InlineData(5, true)]
        public async Task CheckDuplicate(int messageCount, bool isHaveError)
        {
            CancellationToken cancellationToken = default;

            var model = new MailSenderDataDTO()
            {
                Email = Guid.NewGuid().ToString(),
                MailTemplateId = Guid.NewGuid()
            };

            for (int i = 0; i < messageCount; i++)
            {
                try
                {
                    await _duplicateService.CheckDuplicate(model, cancellationToken);

                    if (isHaveError)
                    {
                        Assert.Fail();
                    }
                }
                catch
                {
                    if (!isHaveError)
                    {
                        Assert.Fail();
                    }
                }
            }
        }
    }
}