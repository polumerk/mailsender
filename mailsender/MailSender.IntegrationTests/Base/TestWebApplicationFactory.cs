using MailSender.Application.Services.AbstractClasses;
using MailSender.TestConstants.MockServices;
using MailSender.TestConstants.MockServices.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MailSender.IntegrationTests.Base
{
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "TEST");
            builder
                .ConfigureServices(async services =>
                {
                    services.AddScoped<IDatabaseSeedService, DatabaseSeedService>();
                    services.TryAddScoped<SenderProtocolAbstractService, SenderPop3EmptyService>();

                    ServiceProvider sp = services.BuildServiceProvider();

                    using IServiceScope scope = sp.CreateScope();
                    IDatabaseSeedService databaseSeedService = scope.ServiceProvider.GetRequiredService<IDatabaseSeedService>();
                    await databaseSeedService.SeedData();
                });
        }
    }
}