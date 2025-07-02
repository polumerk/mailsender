using System.Reflection;
using Dictionaries.Extensions;
using MailSender.Domain.Constants;
using MailSender.Infrastructure.Context;
using MailSender.Infrastructure.Context.Interfaces;
using MailSender.Infrastructure.Repositories;
using MailSender.Infrastructure.Repositories.Interfaces;
using MailSender.Infrastructure.Repositories.MailSender;
using MailSender.Infrastructure.Repositories.TemplateService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddDbContextByType<ApplicationContext>(
                    configuration,
                    Assembly.GetExecutingAssembly(),
                    ApplicationConstants.ConnectionStringService,
                    ApplicationConstants.ConnectionStringDatabaseType,
                    ApplicationConstants.ApplicationSystemSource)
                .AddDbContextByType<TemplateServiceContext>(
                    configuration,
                    Assembly.GetExecutingAssembly(),
                    "TemplateService",
                    "TemplateServiceDatabaseType",
                    ApplicationConstants.ApplicationTemplateServiceScheme);

            services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();

            // ApplicationContext
            services.AddTransient<IMessageRepository, MessageRepository>();
            services.AddTransient<IStatusHistoryRepository, StatusHistoryRepository>();
            services.AddTransient<IStatusRepository, StatusRepository>();
            services.AddTransient<IWhiteListRepository, WhiteListRepository>();

            // TemplateServiceContext
            services.AddTransient<IOutputRepository, OutputRepository>();

            // Shared Contexts
            services.AddTransient(typeof(IBaseRepository<>), typeof(BaseRepository<>));
            services.AddTransient(typeof(IBaseRepository<>), typeof(BaseTemplateServiceRepository<>));
            services.AddTransient<IUnitOfWork, UnitOfWork>();

            return services;
        }
    }
}