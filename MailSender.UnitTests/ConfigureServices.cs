using MailSender.Application.Services.AbstractClasses;
using MailSender.Application.Services.Interfaces;
using MailSender.TestConstants.MockServices;
using MailSender.TestConstants.MockServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MailSender.UnitTests
{
    public static class ConfigureServices
    {
        public static IServiceCollection ConfigureMockServices(this IServiceCollection services)
        {
            services.TryAddScoped<IDatabaseSeedService, DatabaseSeedService>();
            services.TryAddScoped<SenderProtocolAbstractService, SenderPop3EmptyService>();
            services.AddScoped<IDuplicateService, DuplicateEmptyService>();

            return services;
        }
    }
}