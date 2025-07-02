using Dictionaries;
using Dictionaries.DTO;
using Dictionaries.Extensions;
using MailSender.Application.Mappings;
using MailSender.Application.Services;
using MailSender.Application.Services.AbstractClasses;
using MailSender.Application.Services.Broker.External;
using MailSender.Application.Services.Broker.Internal;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Configuration;
using MailSender.Domain.Constants;
using MailSender.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpContextAccessor();

            services.AddMemoryCache();

            services
                .Configure<MessageQueueExternalSettings>(configuration.GetSection(nameof(MessageQueueExternalSettings)))
                .Configure<MessageQueueInternalSettings>(configuration.GetSection(nameof(MessageQueueInternalSettings)))
                .Configure<MailSettings>(configuration.GetSection(nameof(MailSettings)))
                .Configure<SMTPSettings>(configuration.GetSection(nameof(SMTPSettings)));

            services.AddAutomapperWithDefaultProfile(
               ApplicationConstants.ApplicationSystemSource,
               typeof(EntitiesProfile),
               typeof(BrokerProfile));

            var policies = new PolicyInfoDTO[]
            {
                new(ApplicationConstants.PolicyRequireReadWhitelist,BaseIdentity.ReadAccess, 6),
                new(ApplicationConstants.PolicyRequireUpdateWhitelist,BaseIdentity.UpdateAccess, 6),
                new(ApplicationConstants.PolicyRequireCreateWhitelist,BaseIdentity.CreateAccess, 6),
                new(ApplicationConstants.PolicyRequireDeleteWhitelist,BaseIdentity.DeleteAccess, 6)
            };

            services.AddCustomAuthentication(configuration, policies);

            services.AddKafkaSendServices<MessageQueueInternalSettings, MessageQueueExternalSettings>(configuration,
                () => services.AddHostedService<ExternalKafkaRecieveService>(),
                () => services.AddHostedService<InternalKafkaRecieveService>(),
                () => services.AddHostedService<InternalKafkaTemplateRecieveService>()
            );

            // add sender service
            ServiceProvider serviceProvider = services.BuildServiceProvider();
            MailSettings mailSettings = serviceProvider.GetRequiredService<IOptions<MailSettings>>().Value;

            switch (mailSettings.MailSenderType)
            {
                case EmailSenderTypeEnum.SMTP:
                    {
                        services.AddScoped<SenderProtocolAbstractService, SenderSmtpService>();
                        break;
                    }
                case EmailSenderTypeEnum.POP3:
                    {
                        break;
                    }
            }

            services.AddTransient<IHashService, HashService>();
            services.AddTransient<IBrokerMessageCreatorService, BrokerMessageCreatorService>();
            services.AddScoped<IMailSenderService, MailSenderService>();
            services.AddScoped<IWhiteListService, WhiteListService>();
            services.AddScoped<IDuplicateService, DuplicateService>();
            services.AddSingleton<IOrchestratorService, OrchestratorService>();

            return services;
        }
    }
}