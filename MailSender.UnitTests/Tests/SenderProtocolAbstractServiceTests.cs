using Dictionaries;
using MailSender.Application.Services.AbstractClasses;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs;
using MailSender.Domain.Enums;
using MailSender.Infrastructure.Repositories.Interfaces;
using static MailSender.TestConstants.Constants;

namespace MailSender.UnitTests.Tests
{
    public class SenderProtocolAbstractServiceTests
    {
        private readonly SenderProtocolAbstractService _senderProtocolAbstractService;
        private readonly IUnitOfWork _unitOfWork;

        public SenderProtocolAbstractServiceTests(
            SenderProtocolAbstractService senderProtocolAbstractService,
            IUnitOfWork unitOfWork)
        {
            _senderProtocolAbstractService = senderProtocolAbstractService;
            _unitOfWork = unitOfWork;
        }

        [Theory]
        [InlineData(ApplicationConstants.SUCCESS, false,
            new[] {
                (int)StatusEnum.Created,
                (int)StatusEnum.StartSended,
                (int)StatusEnum.EndSended
            })]
        [InlineData(CodeResults.SYSER, true,
            new[] {
                (int)StatusEnum.Created,
                (int)StatusEnum.StartSended,
                (int)StatusEnum.Retry,
                (int)StatusEnum.Error
            })]
        public async Task Random_GenerateDifferenceValue_Success(string messageText, bool isHaveError, int[] statuses)
        {
            CancellationToken cancellationToken = default;

            Guid messageId = Guid.NewGuid();

            var model = new MailInfoDTO
            {
                Email = KafkaMessageTest.WhiteListEmail,
                Title = messageText
            };

            try
            {
                await _senderProtocolAbstractService.Send(model, messageId, cancellationToken);
            }
            catch { }

            var message = await _unitOfWork.Messages.GetById(messageId, cancellationToken);

            Assert.NotNull(message);
            Assert.Equal(statuses, message.StatusHistories.Select(x => x.StatusId).ToArray());
            Assert.Equal(isHaveError, message.StatusHistories.Where(x => !string.IsNullOrEmpty(x.Description)).Any(x => x.Description.Contains(CodeResults.SYSER)));
        }
    }
}