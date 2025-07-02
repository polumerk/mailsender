using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json.Serialization;
using Application;
using Dictionaries.Extensions;
using Dictionaries.Middlewares;
using HealthChecks.Prometheus.Metrics;
using HealthChecks.UI.Client;
using Hellang.Middleware.ProblemDetails;
using Infrastructure;
using MailSender.API;
using MailSender.Domain.Constants;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;
using Serilog;

string env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "DEV";

IConfigurationRoot configuration = new ConfigurationBuilder()
    .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{env}.json", true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables()
    .Build();

Serilog.Core.Logger _logger = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration)
    .CreateLogger();

_logger.Information($"Env value: {env}");
_logger.Information("Starting Service...");

try
{
    // default
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog(_logger);

    builder.Services.AddSwaggerGen(config =>
    {
        config.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = $"{env} | {Assembly.GetExecutingAssembly().GetApplicationInfoWithVersions()}",
            Description = "Send mail using SMTP",
            Version = "v1"
        });

        var filePath = Path.Combine(AppContext.BaseDirectory, "MailSender.API.xml");
        config.IncludeXmlComments(filePath);

        filePath = Path.Combine(AppContext.BaseDirectory, "MailSender.Domain.xml");
        config.IncludeXmlComments(filePath);

        config.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description =
@"<p>JWT Authorization header using the Bearer scheme.</p>
<p>Enter 'Bearer' [space] and then your token in the text input below.</p>
<p>Example: 'Bearer 12345abcdef'</p>",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        config.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    },
                    Scheme = "oauth2",
                    Name = "Bearer",
                    In = ParameterLocation.Header,
                },
                new List<string>()
            }
        });
    });

    builder.Services
        .AddControllers()
        .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); })
        .AddNewtonsoftJson(options =>
        {
            options.SerializerSettings.Converters.Add(new StringEnumConverter());
        });

    builder.Services.AddHealthChecks();

    builder.Services.Configure<RouteOptions>(options =>
    {
        options.LowercaseUrls = true;
    });
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy",
            builder =>
            {
                builder
                  .AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
            });
    });

    // custom
    builder.Services
        .AddLogging()
        .AddApplicationServices(configuration)
        .AddInfrastructureServices(configuration)
        .AddConfiguredProblemDetails();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedPrefix |
            ForwardedHeaders.XForwardedHost |
            ForwardedHeaders.XForwardedProto;
    });

    // default
    WebApplication app = builder.Build();
    app.UseHttpsRedirection();
    app.UseRouting();
    app.UseCors("CorsPolicy");
    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<AuditMiddleware>(configuration, ApplicationConstants.ApplicationSystemSource);
    app.UseProblemDetails();
    app.MapControllers();
    app.UseMiddleware<CultureMiddleware>();
    app.UseSwagger(c =>
    {
        c.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
        {
            if (httpReq.Headers.TryGetValue("X-Forwarded-Prefix", out var prefix) &&
                httpReq.Headers.TryGetValue("X-Forwarded-Host", out var host) &&
                httpReq.Headers.TryGetValue("X-Forwarded-Proto", out var proto))
            {
                swaggerDoc.Servers =
                [
                    new OpenApiServer { Url = $"{proto}://{host}{prefix}" }
                ];
            }
        });
    });
    app.UseSwaggerUI();
    app.UseHealthChecks("/health/api", new HealthCheckOptions
    {
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
    app.UseHealthChecks("/health/prometheus", new HealthCheckOptions
    {
        ResponseWriter = PrometheusResponseWriter.WritePrometheusResultText
    });

    // custom
    await app.AddDatabaseInitializer();

    await app.RunAsync();
}
catch (Exception ex)
{
    _logger.Fatal(ex, "Unhandled exception");
    throw;
}
finally
{
    _logger.Information("Service stopped");
    _logger.Dispose();
}

// for accessing from IntegrationTests project
public partial class Program
{
}