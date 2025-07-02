using MailSender.Application.Services.Interfaces;
using MailSender.Domain.DTOs.Broker.Receive;

namespace MailSender.UnitTests.Tests
{
    public class HashServiceTests
    {
        private readonly IHashService _hashService;

        public HashServiceTests(
            IHashService hashService)
        {
            _hashService = hashService;
        }

        [Theory]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(10000, 1)]
        [InlineData(100000, 1)]
        [InlineData(1000000, 1)]
        [InlineData(10000000, 1)]
        public void GenerateHash(int len, int differentSymbols)
        {
            static string GenerateNewPassword(int passwordLength)
            {
                const string _chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

                var stringChars = new char[passwordLength];
                var random = new Random();

                for (int i = 0; i < stringChars.Length; i++)
                {
                    stringChars[i] = _chars[random.Next(_chars.Length)];
                }

                return new string(stringChars);
            }

            var firstMessage = GenerateNewPassword(len);
            var secondMessageDiff = GenerateNewPassword(differentSymbols);
            var secondMessage = string.Concat(firstMessage[..^secondMessageDiff.Length], secondMessageDiff);

            var hashFirst = _hashService.Hash(new MailSenderDataDTO()
            {
                MailTemplateTitle = firstMessage
            });

            var hashSecond = _hashService.Hash(new MailSenderDataDTO()
            {
                MailTemplateTitle = secondMessage
            });

            Assert.NotNull(hashFirst);
            Assert.NotNull(hashSecond);
            Assert.Equal(firstMessage.Length, secondMessage.Length);
            Assert.NotEqual(hashFirst, hashSecond);
        }
    }
}