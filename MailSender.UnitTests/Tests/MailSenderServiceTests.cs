using System.Text;
using Dictionaries.DTO;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.Broker.TemplateService;
using MailSender.Domain.Entities.TemplateService;
using MailSender.Domain.Enums;
using MailSender.Infrastructure.Repositories.Interfaces;
using Newtonsoft.Json;
using static MailSender.TestConstants.Constants;

namespace MailSender.UnitTests.Tests
{
    public class MailSenderServiceTests
    {
        private readonly IMailSenderService _mailSenderService;
        private readonly IUnitOfWork _unitOfWork;

        public MailSenderServiceTests(
            IMailSenderService mailSenderService,
            IUnitOfWork unitOfWork)
        {
            _mailSenderService = mailSenderService;
            _unitOfWork = unitOfWork;
        }

        [Fact]
        public async Task ProcessRequest_Success()
        {
            // Arrange
            CancellationToken cancellationToken = default;
            var emailMessage = KafkaMessageTest.GetKafkaMailSenderDataDTO(KafkaMessageTest.WhiteListEmail);
            var messageJson = JsonConvert.SerializeObject(emailMessage);

            // Act
            var brokerMessageRequest = await _mailSenderService.ProcessRequest(messageJson, cancellationToken);
            var lastMessage = await _unitOfWork.Messages.GetByTemplateMessageId(brokerMessageRequest.TemplateNotification.MessageInfo.MessageId, cancellationToken);
            var statusHistory = lastMessage.StatusHistories.FirstOrDefault();

            // Assert
            Assert.NotNull(brokerMessageRequest.TemplateNotification);
            Assert.NotNull(lastMessage);
            Assert.NotNull(statusHistory);
            Assert.Single(lastMessage.StatusHistories);
            Assert.Equal((int)StatusEnum.Created, statusHistory.StatusId);
            Assert.Equal(lastMessage.TemplateMessageId, brokerMessageRequest.TemplateNotification.MessageInfo.MessageId);
            Assert.Equal(ApplicationConstants.ApplicationSystemSource, brokerMessageRequest.TemplateNotification.MessageInfo.SystemSource);
            Assert.Equal(ApplicationConstants.TemplateService, brokerMessageRequest.TemplateNotification.MessageInfo.MessageType);
        }

        [Fact]
        public async Task ProcessResponse_Success()
        {
            // Arrange
            CancellationToken cancellationToken = default;
            var content = "test";
            var emailMessage = KafkaMessageTest.GetKafkaMailSenderDataDTO(KafkaMessageTest.WhiteListEmail);
            var messageJson = JsonConvert.SerializeObject(emailMessage);

            // Act
            var brokerMessageRequest = await _mailSenderService.ProcessRequest(messageJson, cancellationToken);

            var templateResponse = await GetKafkaTemplateResult(brokerMessageRequest.TemplateNotification.MessageInfo.MessageId, content, cancellationToken);
            var brokerMessageResponse = await _mailSenderService.ProcessResponse(JsonConvert.SerializeObject(templateResponse), cancellationToken);

            var lastMessage = await _unitOfWork.Messages.GetByTemplateMessageId(brokerMessageRequest.TemplateNotification.MessageInfo.MessageId, cancellationToken);
            var lastStatusHistory = lastMessage.StatusHistories.LastOrDefault();
            var templateOutput = await _unitOfWork.Outputs.GetByMessageId(lastMessage.TemplateMessageId.Value, cancellationToken);
            var templateOutputContent = Encoding.UTF8.GetString(templateOutput.ContentInfo.Content);

            // Assert
            Assert.NotNull(brokerMessageResponse.Notification);
            Assert.NotNull(templateOutput);
            Assert.NotNull(templateOutput.ContentInfo.Content);
            Assert.NotNull(lastStatusHistory);
            Assert.Equal((int)StatusEnum.EndSended, lastStatusHistory.StatusId);
            Assert.Equal(ApplicationConstants.SUCCESS, brokerMessageResponse.Notification.Data.ResultCode);
            Assert.Equal(ApplicationConstants.ApplicationSystemSource, brokerMessageResponse.SystemSource);
            Assert.Equal(content, templateOutputContent);
        }

        private async Task<KafkaMessageDTO<TemplateServiceDTO>> GetKafkaTemplateResult(Guid messageId, string content, CancellationToken cancellationToken)
        {
            await _unitOfWork.Outputs.Add(new Output
            {
                MessageId = messageId,
                ContentInfo = new ContentInfo
                {
                    Content = Encoding.UTF8.GetBytes(content)
                }
            }, cancellationToken);

            await _unitOfWork.TemplateServiceComplete(cancellationToken);

            return KafkaMessageTest.GetKafkaTemplateServiceDTO(messageId);
        }
    }
}