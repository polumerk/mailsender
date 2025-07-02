using System.Text;
using Dictionaries;
using Dictionaries.DTO;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.DTOs.Broker.TemplateService;

namespace MailSender.TestConstants
{
    public static class Constants
    {
        public const string JsonMediaType = "application/json";
        public const string JsonMediaTypePatch = "application/json-patch+json";
        public const string ProblemDetailsJson = "application/problem+json; charset=utf-8";
        public const string Json = "application/json; charset=utf-8";

        public static class KafkaMessageTest
        {
            public const string WhiteListEmail = "test@test.ru";

            public static KafkaMessageDTO<MailSenderDataDTO> GetKafkaMailSenderDataDTO(string email)
            {
                return new KafkaMessageDTO<MailSenderDataDTO>
                {
                    MessageInfo = new MessageInfoDTO
                    {
                        JsonVersion = "1.0.0",
                        MessageId = Guid.NewGuid(),
                        MasterMessageId = Guid.NewGuid(),
                        ParentMessageId = null,
                        SystemSource = ApplicationConstants.ApplicationSystemSource,
                        DateTimeCreate = DateTime.UtcNow,
                        ActionType = ActionTypes.INS
                    },
                    Data = new MailSenderDataDTO
                    {
                        MailTemplateTitle = "Title_SUCCESS",
                        Email = email,
                        Attachments = new MailAttachmentDTO[]
                        {
                            new()
                            {
                                File = new()
                                {
                                    FileName = Guid.NewGuid().ToString(),
                                    FileExt = ".html",
                                    FileBody = Convert.ToBase64String(Encoding.UTF8.GetBytes("Test text"))
                                }
                            }
                        }
                    }
                };
            }

            public static KafkaMessageDTO<TemplateServiceDTO> GetKafkaTemplateServiceDTO(Guid messageId)
            {
                return new KafkaMessageDTO<TemplateServiceDTO>
                {
                    MessageInfo = new MessageInfoDTO
                    {
                        JsonVersion = "1.0.0",
                        MessageId = Guid.NewGuid(),
                        MasterMessageId = Guid.NewGuid(),
                        ParentMessageId = messageId,
                        SystemSource = ApplicationConstants.ApplicationSystemSource,
                        DateTimeCreate = DateTime.UtcNow,
                        ActionType = ActionTypes.INS,
                        MessageType = ApplicationConstants.TemplateService
                    },

                    Data = new TemplateServiceDTO
                    {
                        ResultCode = ApplicationConstants.SUCCESS
                    }
                };
            }
        }
    }
}