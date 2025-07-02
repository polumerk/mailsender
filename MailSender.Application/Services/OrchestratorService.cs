using System.Threading;
using System.Threading.Tasks;
using Dictionaries.Services.Interfaces;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Configuration;
using MailSender.Domain.DTOs.Broker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MailSender.Application.Services
{
    public class OrchestratorService : IOrchestratorService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IExternalMessageQueueService _externalMessageQueueService;
        private readonly IInternalMessageQueueService _internalMessageQueueService;
        private readonly MessageQueueExternalSettings _messageQueueExternalSettings;
        private readonly MessageQueueInternalSettings _messageQueueInternalSettings;

        public OrchestratorService(
            IServiceScopeFactory scopeFactory,
            IExternalMessageQueueService externalMessageQueueService,
            IInternalMessageQueueService internalMessageQueueService,
            IOptions<MessageQueueExternalSettings> messageQueueExternalSettings,
            IOptions<MessageQueueInternalSettings> messageQueueInternalSettings)
        {
            _scopeFactory = scopeFactory;
            _externalMessageQueueService = externalMessageQueueService;
            _internalMessageQueueService = internalMessageQueueService;
            _messageQueueExternalSettings = messageQueueExternalSettings.Value;
            _messageQueueInternalSettings = messageQueueInternalSettings.Value;
        }

        public async Task<BrokerMessage> Process(string message, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IMailSenderService service = scope.ServiceProvider.GetService<IMailSenderService>();
            BrokerMessage brokerMessage = await service.ProcessRequest(message, cancellationToken);

            if (brokerMessage.TemplateNotification != null)
            {
                await _internalMessageQueueService.Send(
                   brokerMessage.TemplateNotification,
                   _messageQueueInternalSettings.TemplateSendQueue,
                   brokerMessage.TemplateNotification.MessageInfo.MessageType,
                   cancellationToken);
            }

            if (brokerMessage.Notification != null)
            {
                var externalQueue = _messageQueueExternalSettings.GetNotificationQueueBySystem(brokerMessage.SystemSource);
                if (!string.IsNullOrEmpty(externalQueue))
                {
                    await _externalMessageQueueService.Send(
                        message: brokerMessage.Notification,
                        queue: externalQueue,
                        exchange: brokerMessage.Notification.MessageInfo.MessageType,
                        cancellationToken: cancellationToken);
                }

                var internalQueue = _messageQueueInternalSettings.GetNotificationQueueBySystem(brokerMessage.SystemSource);

                if (!string.IsNullOrEmpty(internalQueue))
                {
                    await _internalMessageQueueService.Send(
                        message: brokerMessage.Notification,
                        queue: internalQueue,
                        exchange: brokerMessage.Notification.MessageInfo.MessageType,
                        cancellationToken: cancellationToken);
                }
            }

            if (brokerMessage.Error != null)
            {
                await _internalMessageQueueService.Send(
                    message: brokerMessage.Error,
                    queue: _messageQueueInternalSettings.ErrorQueue,
                    exchange: brokerMessage.Error.MessageInfo.MessageType,
                    cancellationToken: cancellationToken);
            }

            return brokerMessage;
        }

        public async Task<BrokerMessage> TemplateServiceProcess(string message, CancellationToken cancellationToken)
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IMailSenderService service = scope.ServiceProvider.GetService<IMailSenderService>();
            BrokerMessage brokerMessage = await service.ProcessResponse(message, cancellationToken);

            if (brokerMessage.Notification != null)
            {
                var externalQueue = _messageQueueExternalSettings.GetNotificationQueueBySystem(brokerMessage.SystemSource);
                if (!string.IsNullOrEmpty(externalQueue))
                {
                    await _externalMessageQueueService.Send(
                        message: brokerMessage.Notification,
                        queue: externalQueue,
                        exchange: brokerMessage.Notification.MessageInfo.MessageType,
                        cancellationToken: cancellationToken);
                }

                var internalQueue = _messageQueueInternalSettings.GetNotificationQueueBySystem(brokerMessage.SystemSource);

                if (!string.IsNullOrEmpty(internalQueue))
                {
                    await _internalMessageQueueService.Send(
                        message: brokerMessage.Notification,
                        queue: internalQueue,
                        exchange: brokerMessage.Notification.MessageInfo.MessageType,
                        cancellationToken: cancellationToken);
                }
            }

            if (brokerMessage.Error != null)
            {
                await _internalMessageQueueService.Send(
                    message: brokerMessage.Error,
                    queue: _messageQueueInternalSettings.ErrorQueue,
                    exchange: brokerMessage.Error.MessageInfo.MessageType,
                    cancellationToken: cancellationToken);
            }

            return brokerMessage;
        }
    }
}