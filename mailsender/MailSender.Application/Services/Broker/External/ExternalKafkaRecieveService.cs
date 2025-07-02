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

namespace MailSender.Application.Services.Broker.External
{
    public class ExternalKafkaRecieveService : BackgroundService
    {
        private readonly IOrchestratorService _orchestratorService;
        private readonly MessageQueueExternalSettings _messageQueueExternalSettings;
        private readonly ILogger<ExternalKafkaRecieveService> _logger;

        private readonly IConsumer<string, string> _consumer;

        public ExternalKafkaRecieveService(
            IOrchestratorService orchestratorService,
            IOptions<MessageQueueExternalSettings> messageQueueExternalSettings,
            ILogger<ExternalKafkaRecieveService> logger)
        {
            _orchestratorService = orchestratorService;
            _messageQueueExternalSettings = messageQueueExternalSettings.Value;
            _logger = logger;

            ConsumerConfig config = new()
            {
                MessageMaxBytes = _messageQueueExternalSettings.MessageMaxBytes,
                GroupId = _messageQueueExternalSettings.GroupId,
                BootstrapServers = _messageQueueExternalSettings.BootstrapServers,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                SaslUsername = _messageQueueExternalSettings.UserName,
                SaslPassword = _messageQueueExternalSettings.Password,
                SaslMechanism = _messageQueueExternalSettings.SaslMechanism,
                SecurityProtocol = _messageQueueExternalSettings.SecurityProtocol,
                PartitionAssignmentStrategy = _messageQueueExternalSettings.PartitionAssignmentStrategy,
                SslCaLocation = _messageQueueExternalSettings.SslCaLocation,
                ClientId = $"Client_{_messageQueueExternalSettings.GroupId}"
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
                        await _orchestratorService.Process(response.Message.Value, cancellationToken);
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
            return Task.Run(() => SubscribeOnKafkaTopic(_messageQueueExternalSettings.RecieveQueue, default), stoppingToken);
        }
    }
}