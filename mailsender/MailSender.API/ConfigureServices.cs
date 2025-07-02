using System;
using System.Threading.Tasks;
using Dictionaries.Extensions;
using Hellang.Middleware.ProblemDetails;
using MailSender.Domain.Exceptions;
using MailSender.Infrastructure.Context.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace MailSender.API
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddConfiguredProblemDetails(this IServiceCollection services)
        {
            services.AddProblemDetails(options =>
            {
                options.IncludeExceptionDetails = (ctx, ex) => false;

                options.Map<NotFoundException>(ex => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = ex.GetMessageFromException(), Detail = ex.StackTrace });
                options.Map<ValidationException>(ex => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = ex.GetMessageFromException(), Detail = ex.StackTrace });
                options.Map<Exception>(ex => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = ex.GetMessageFromException(), Detail = ex.StackTrace });
            });

            return services;
        }

        public static async Task AddDatabaseInitializer(this IApplicationBuilder app)
        {
            using IServiceScope serviceScope = app.ApplicationServices
                .GetRequiredService<IServiceScopeFactory>()
                .CreateScope();

            IDatabaseInitializer initializer = serviceScope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

            await initializer.InitialiseAsync();
        }
    }
}