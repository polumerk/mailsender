using MailSender.Application.Services.Interfaces;
using MailSender.Domain.DTOs.Broker.Receive;
using Newtonsoft.Json;
using System;

namespace MailSender.Application.Services
{
    public class HashService : IHashService
    {
        public string Hash(MailSenderDataDTO model)
        {
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(model));
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return BitConverter.ToString(hashBytes);
        }
    }
}