using System;
using System.Threading;
using System.Threading.Tasks;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.Broker;

namespace MailSender.Application.Services.Interfaces
{
    public interface IMailSenderService
    {
        public Task<BrokerMessage> ProcessRequest(string message, CancellationToken cancellationToken);
        public Task<BrokerMessage> ProcessResponse(string message, CancellationToken cancellationToken);
        public Task Send(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken);
    }
}