using System.Reflection;
using Application;
using Infrastructure;
using MailSender.API;
using MailSender.TestConstants.MockServices.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MailSender.UnitTests
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                .AddJsonFile("appsettings.json")
                .AddJsonFile("appsettings.TEST.json", true)
                .AddUserSecrets<Program>(optional: true)
                .AddEnvironmentVariables()
                .Build();

            services
                .AddApplicationServices(configuration)
                .AddInfrastructureServices(configuration)
                .AddConfiguredProblemDetails();

            ServiceDescriptor[] serviceDescriptor = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToArray();
            if (serviceDescriptor?.Any() == true)
            {
                foreach (ServiceDescriptor service in serviceDescriptor)
                {
                    services.Remove(service);
                }
            }

            services.ConfigureMockServices();

            ServiceProvider sp = services.BuildServiceProvider();

            using IServiceScope scope = sp.CreateScope();
            IDatabaseSeedService databaseSeedService = scope.ServiceProvider.GetRequiredService<IDatabaseSeedService>();
            databaseSeedService.SeedData().GetAwaiter().GetResult();
        }
    }
}