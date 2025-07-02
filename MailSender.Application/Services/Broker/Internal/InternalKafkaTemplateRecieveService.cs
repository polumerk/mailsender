using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Dictionaries.Extensions;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MailSender.Application.Services.Broker.Internal
{
    public class InternalKafkaTemplateRecieveService : BackgroundService
    {
        private readonly IOrchestratorService _orchestratorService;
        private readonly MessageQueueInternalSettings _messageQueueInternalSettings;
        private readonly ILogger<InternalKafkaTemplateRecieveService> _logger;

        private readonly IConsumer<string, string> _consumer;

        public InternalKafkaTemplateRecieveService(
            IOrchestratorService orchestratorService,
            IOptions<MessageQueueInternalSettings> messageQueueInternalSettings,
            ILogger<InternalKafkaTemplateRecieveService> logger)
        {
            _orchestratorService = orchestratorService;
            _messageQueueInternalSettings = messageQueueInternalSettings.Value;
            _logger = logger;

            ConsumerConfig config = new()
            {
                GroupId = _messageQueueInternalSettings.GroupId,
                BootstrapServers = _messageQueueInternalSettings.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                SaslUsername = _messageQueueInternalSettings.UserName,
                SaslPassword = _messageQueueInternalSettings.Password,
                SaslMechanism = _messageQueueInternalSettings.SaslMechanism,
                SecurityProtocol = _messageQueueInternalSettings.SecurityProtocol,
                PartitionAssignmentStrategy = _messageQueueInternalSettings.PartitionAssignmentStrategy,
                SslCaLocation = _messageQueueInternalSettings.SslCaLocation,
                ClientId = $"Client_{_messageQueueInternalSettings.GroupId}"
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        public async Task SubscribeOnKafkaTopic(string topic, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(topic))
            {
                _logger.LogInformation($"Kafka Receive empty.");
                return;
            }
            _consumer.Subscribe(topic);
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    ConsumeResult<string, string> response = _consumer.Consume(cancellationToken);
                    if (response != null)
                    {
                        await _orchestratorService.TemplateServiceProcess(response.Message.Value, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.GetMessageFromException());
                }
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(() => SubscribeOnKafkaTopic(_messageQueueInternalSettings.TemplateRecieveQueue, default), stoppingToken);
        }
    }
}