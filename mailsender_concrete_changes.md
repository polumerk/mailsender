# Конкретные изменения файлов проекта MailSender

## 1. Изменения в файлах проектов (.csproj)

### 1.1 MailSender.Application.csproj

**Текущее состояние:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <Import Project="..\Project.props" />
  
  <ItemGroup>
    <ProjectReference Include="..\MailSender.Infrastructure\MailSender.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**После рефакторинга:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <Import Project="..\Project.props" />
  
  <ItemGroup>
    <ProjectReference Include="..\MailSender.Domain\MailSender.Domain.csproj" />
  </ItemGroup>
  
  <ItemGroup>
    <PackageReference Include="MediatR" Version="12.4.1" />
    <PackageReference Include="FluentValidation" Version="11.10.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.10.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.16" />
    <PackageReference Include="System.Threading.Channels" Version="8.0.0" />
  </ItemGroup>
</Project>
```

### 1.2 MailSender.Infrastructure.csproj

**Текущее состояние:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <Import Project="..\Project.props" />
  
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.16">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\MailSender.Domain\MailSender.Domain.csproj" />
  </ItemGroup>
</Project>
```

**После рефакторинга:**
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <Import Project="..\Project.props" />
  
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.16">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Polly" Version="8.5.0" />
    <PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />
  </ItemGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\MailSender.Domain\MailSender.Domain.csproj" />
    <ProjectReference Include="..\MailSender.Application\MailSender.Application.csproj" />
  </ItemGroup>
</Project>
```

### 1.3 MailSender.API.csproj

**Добавить новые пакеты:**
```xml
<!-- Добавить в существующие PackageReference -->
<PackageReference Include="MediatR" Version="12.4.1" />
<PackageReference Include="Microsoft.AspNetCore.RateLimiting" Version="8.0.16" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="System.Threading.Channels" Version="8.0.0" />
```

## 2. Изменения конфигурационных файлов

### 2.1 MailSender.API/Program.cs

**Текущий участок кода (строки 90-120):**
```csharp
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
```

**После рефакторинга:**
```csharp
builder.Services
    .AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); })
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.Converters.Add(new StringEnumConverter());
    });

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("EmailSending", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 100;
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 10;
    });
});

builder.Services.AddHealthChecks()
    .AddCheck<EmailHealthCheck>("email_provider")
    .AddCheck<RedisHealthCheck>("redis_cache");

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

// CORS с более строгими настройками
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>())
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});
```

### 2.2 MailSender.API/appsettings.json

**Добавить новые секции конфигурации:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "Redis": "localhost:6379"
  },
  "SmtpSettings": {
    "PrimaryHost": "smtp.primary.com",
    "PrimaryPort": 587,
    "SecondaryHost": "smtp.secondary.com", 
    "SecondaryPort": 587,
    "EnableSsl": true,
    "Username": "#{SmtpUsername}#",
    "Password": "#{SmtpPassword}#",
    "DefaultFromAddress": "noreply@company.com",
    "Timeout": 30000
  },
  "EmailQueueSettings": {
    "MaxCapacity": 1000,
    "MaxRetries": 3,
    "RetryDelaySeconds": [2, 4, 8]
  },
  "CacheSettings": {
    "WhiteListCacheDurationMinutes": 15,
    "DuplicateCheckCacheDurationMinutes": 30,
    "DefaultCacheDurationMinutes": 5
  },
  "AllowedOrigins": [
    "https://app.company.com",
    "https://admin.company.com"
  ],
  "HealthChecks": {
    "EmailProviderTimeoutSeconds": 10,
    "RedisCacheTimeoutSeconds": 5
  }
}
```

## 3. Новые интерфейсы для Application слоя

### 3.1 MailSender.Application/Interfaces/IEmailUnitOfWork.cs

```csharp
using MailSender.Domain.Entities;

namespace MailSender.Application.Interfaces
{
    public interface IEmailUnitOfWork : IDisposable
    {
        IEmailRepository Messages { get; }
        IStatusHistoryRepository StatusHistory { get; }
        IWhiteListRepository WhiteList { get; }
        IOutputRepository Outputs { get; }
        
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);
        Task CommitTransactionAsync(CancellationToken cancellationToken = default);
        Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
    }

    public interface IEmailRepository : IBaseRepository<Message>
    {
        Task<Message?> GetByTemplateMessageIdAsync(Guid templateMessageId, CancellationToken cancellationToken);
        Task<IEnumerable<Message>> GetByStatusAsync(int statusId, CancellationToken cancellationToken);
        Task<IEnumerable<Message>> GetPendingMessagesAsync(CancellationToken cancellationToken);
    }

    public interface IStatusHistoryRepository : IBaseRepository<StatusHistory>
    {
        Task<IEnumerable<StatusHistory>> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
        Task<StatusHistory?> GetLatestByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
    }

    public interface IWhiteListRepository : IBaseRepository<WhiteList>
    {
        Task<bool> IsEmailAllowedAsync(string email, CancellationToken cancellationToken);
        Task<IEnumerable<string>> GetDisallowedEmailsAsync(string[] emails, CancellationToken cancellationToken);
    }

    public interface IOutputRepository : IBaseRepository<Output>
    {
        Task<Output?> GetByMessageIdAsync(Guid messageId, CancellationToken cancellationToken);
    }
}
```

### 3.2 MailSender.Application/Interfaces/IEmailProvider.cs

```csharp
using MailSender.Domain.DTOs;

namespace MailSender.Application.Interfaces
{
    public interface IEmailProvider
    {
        string Name { get; }
        int Priority { get; }
        bool IsHealthy { get; }
        
        Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
        Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default);
        bool CanHandle(string emailAddress);
        bool IsAvailable();
    }

    public record EmailMessage(
        string[] To,
        string Subject, 
        string Body,
        string? From = null,
        MailAttachmentDTO[]? Attachments = null,
        bool IsImportant = false,
        Dictionary<string, string>? Headers = null);

    public record EmailSendResult(
        bool IsSuccess,
        string? ErrorMessage = null,
        string? MessageId = null,
        DateTime SentAt = default,
        string? ProviderName = null,
        TimeSpan ProcessingTime = default);
}
```

## 4. Изменения существующих сервисов

### 4.1 MailSender.Application/ConfigureServices.cs

**Полная замена файла:**
```csharp
using System.Reflection;
using MailSender.Application.Interfaces;
using MailSender.Application.Services;
using MailSender.Application.Behaviors;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using MediatR;

namespace MailSender.Application
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // MediatR with behaviors
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior<typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>)>();
                cfg.AddBehavior<typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>)>();
                cfg.AddBehavior<typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>)>();
            });
            
            // Validators
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            
            // Core Services - разделенные по ответственности
            services.AddScoped<IEmailValidationService, EmailValidationService>();
            services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
            services.AddScoped<ITemplateProcessingService, TemplateProcessingService>();
            services.AddScoped<IDuplicateCheckService, DuplicateCheckService>();
            services.AddScoped<INotificationService, NotificationService>();
            
            // Caching Services
            services.AddScoped<IDistributedCacheService, DistributedCacheService>();
            services.Decorate<IWhiteListService, CachedWhiteListService>();
            
            // Background Services
            services.AddSingleton<IEmailQueueService, EmailQueueService>();
            services.AddHostedService<EmailProcessingBackgroundService>();
            services.AddHostedService<RetryProcessingBackgroundService>();
            
            // Email Provider Factory
            services.AddScoped<IEmailProviderFactory, EmailProviderFactory>();
            
            // Health Checks
            services.AddScoped<EmailHealthCheck>();
            services.AddScoped<RedisHealthCheck>();
            
            // Caching Infrastructure  
            services.AddMemoryCache();
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = configuration.GetConnectionString("Redis");
                options.InstanceName = "MailSender";
            });
            
            // AutoMapper
            services.AddAutoMapper(Assembly.GetExecutingAssembly());
            
            // Configuration Options
            services.Configure<EmailQueueSettings>(configuration.GetSection("EmailQueueSettings"));
            services.Configure<CacheSettings>(configuration.GetSection("CacheSettings"));
            
            return services;
        }
    }
}
```

### 4.2 Новый Infrastructure/ConfigureServices.cs

**Добавить к существующему методу:**
```csharp
// В конец метода AddInfrastructureServices
namespace Infrastructure
{
    public static class ConfigureServices
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // ... существующий код ...

            // Email Providers с приоритетами
            services.AddScoped<IEmailProvider, SmtpEmailProvider>();
            services.AddScoped<IEmailProvider, BackupSmtpEmailProvider>();
            // Можно добавить другие провайдеры: SendGrid, AWS SES, etc.
            
            // HTTP Clients с Polly для resilience
            services.AddHttpClient<SmtpEmailProvider>()
                .AddPolicyHandler(GetRetryPolicy())
                .AddPolicyHandler(GetCircuitBreakerPolicy());
            
            // Configuration Options
            services.Configure<SmtpSettings>(configuration.GetSection("SmtpSettings"));
            
            return services;
        }
        
        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        var logger = context.GetLogger();
                        logger?.LogWarning("Retry {RetryCount} for {OperationKey} in {Delay}ms", 
                            retryCount, context.OperationKey, timespan.TotalMilliseconds);
                    });
        }
        
        private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    handledEventsAllowedBeforeBreaking: 3,
                    durationOfBreak: TimeSpan.FromMinutes(1),
                    onBreak: (exception, duration) =>
                    {
                        // Log circuit breaker opened
                    },
                    onReset: () =>
                    {
                        // Log circuit breaker closed
                    });
        }
    }
}
```

## 5. Обновление контроллеров

### 5.1 MailSender.API/Controllers/MailSenderController.cs

**Полная замена файла:**
```csharp
using MediatR;
using MailSender.Application.Features.Commands;
using MailSender.Application.Features.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.ComponentModel.DataAnnotations;

namespace MailSender.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    [EnableRateLimiting("EmailSending")]
    public class MailSenderController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<MailSenderController> _logger;

        public MailSenderController(IMediator mediator, ILogger<MailSenderController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        /// <summary>
        /// Отправить email сообщение
        /// </summary>
        /// <param name="command">Данные для отправки email</param>
        /// <param name="cancellationToken">Токен отмены</param>
        /// <returns>Результат отправки с ID сообщения</returns>
        [HttpPost("send")]
        [ProducesResponseType(typeof(SendEmailResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
        public async Task<IActionResult> SendEmail([FromBody] SendEmailCommand command, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Received email send request for {Email}", command.Email);
            
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Email queued successfully with ID {MessageId}", result.MessageId);
                return Ok(result);
            }
            
            _logger.LogWarning("Email send failed: {Error}", result.ErrorMessage);
            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Отправить массовые email сообщения
        /// </summary>
        [HttpPost("send-bulk")]
        [ProducesResponseType(typeof(SendBulkEmailResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendBulkEmail([FromBody] SendBulkEmailCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.IsSuccess)
                return Ok(result);
                
            return BadRequest(result.ErrorMessage);
        }

        /// <summary>
        /// Обработать сообщение от Kafka (для совместимости)
        /// </summary>
        [HttpPost("process-message")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ProcessMessage([FromBody] ProcessKafkaMessageCommand command, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(command, cancellationToken);
            
            if (result.Error != null)
                return BadRequest(result.Error);
                
            return Ok();
        }

        /// <summary>
        /// Получить статус отправки email
        /// </summary>
        [HttpGet("status/{messageId}")]
        [ProducesResponseType(typeof(EmailStatusResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetEmailStatus([Required] Guid messageId, CancellationToken cancellationToken)
        {
            var query = new GetEmailStatusQuery(messageId);
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(result);
        }

        /// <summary>
        /// Получить статистику отправки email
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(EmailStatisticsResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmailStatistics([FromQuery] EmailStatisticsQuery query, CancellationToken cancellationToken)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Получить список провайдеров email
        /// </summary>
        [HttpGet("providers")]
        [ProducesResponseType(typeof(IEnumerable<EmailProviderInfo>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEmailProviders(CancellationToken cancellationToken)
        {
            var query = new GetEmailProvidersQuery();
            var result = await _mediator.Send(query, cancellationToken);
            
            return Ok(result);
        }
    }
}
```

## 6. Новые модели конфигурации

### 6.1 MailSender.Domain/Configuration/SmtpSettings.cs

```csharp
namespace MailSender.Domain.Configuration
{
    public class SmtpSettings
    {
        public string PrimaryHost { get; set; } = string.Empty;
        public int PrimaryPort { get; set; } = 587;
        public string SecondaryHost { get; set; } = string.Empty;
        public int SecondaryPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string DefaultFromAddress { get; set; } = string.Empty;
        public int Timeout { get; set; } = 30000;
        public int MaxRetries { get; set; } = 3;
        public bool UseBackupOnFailure { get; set; } = true;
    }

    public class EmailQueueSettings
    {
        public int MaxCapacity { get; set; } = 1000;
        public int MaxRetries { get; set; } = 3;
        public int[] RetryDelaySeconds { get; set; } = { 2, 4, 8 };
        public int ProcessingDelayMs { get; set; } = 100;
        public int MaxConcurrentProcessing { get; set; } = 10;
    }

    public class CacheSettings
    {
        public int WhiteListCacheDurationMinutes { get; set; } = 15;
        public int DuplicateCheckCacheDurationMinutes { get; set; } = 30;
        public int DefaultCacheDurationMinutes { get; set; } = 5;
        public bool EnableDistributedCache { get; set; } = true;
        public string CacheKeyPrefix { get; set; } = "MailSender";
    }
}
```

## 7. Новые Health Checks

### 7.1 MailSender.Application/HealthChecks/EmailHealthCheck.cs

```csharp
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MailSender.Application.Interfaces;

namespace MailSender.Application.HealthChecks
{
    public class EmailHealthCheck : IHealthCheck
    {
        private readonly IEmailProviderFactory _providerFactory;
        private readonly ILogger<EmailHealthCheck> _logger;

        public EmailHealthCheck(IEmailProviderFactory providerFactory, ILogger<EmailHealthCheck> logger)
        {
            _providerFactory = providerFactory;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var providers = _providerFactory.GetAllProviders();
                var healthyProviders = new List<string>();
                var unhealthyProviders = new List<string>();

                foreach (var provider in providers)
                {
                    try
                    {
                        var isHealthy = await provider.HealthCheckAsync(cancellationToken);
                        if (isHealthy)
                            healthyProviders.Add(provider.Name);
                        else
                            unhealthyProviders.Add(provider.Name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Health check failed for provider {ProviderName}", provider.Name);
                        unhealthyProviders.Add(provider.Name);
                    }
                }

                var data = new Dictionary<string, object>
                {
                    { "healthy_providers", healthyProviders },
                    { "unhealthy_providers", unhealthyProviders },
                    { "total_providers", providers.Count() }
                };

                if (healthyProviders.Any())
                {
                    return HealthCheckResult.Healthy("At least one email provider is healthy", data);
                }

                return HealthCheckResult.Unhealthy("No email providers are healthy", data: data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email health check failed");
                return HealthCheckResult.Unhealthy("Email health check failed", ex);
            }
        }
    }

    public class RedisHealthCheck : IHealthCheck
    {
        private readonly IDistributedCacheService _cacheService;
        private readonly ILogger<RedisHealthCheck> _logger;

        public RedisHealthCheck(IDistributedCacheService cacheService, ILogger<RedisHealthCheck> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var testKey = "health_check_" + Guid.NewGuid();
                var testValue = DateTime.UtcNow.ToString();

                await _cacheService.SetAsync(testKey, testValue, TimeSpan.FromMinutes(1), cancellationToken);
                var retrievedValue = await _cacheService.GetAsync<string>(testKey, cancellationToken);
                await _cacheService.RemoveAsync(testKey, cancellationToken);

                if (retrievedValue == testValue)
                {
                    return HealthCheckResult.Healthy("Redis cache is working correctly");
                }

                return HealthCheckResult.Degraded("Redis cache is not working as expected");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis health check failed");
                return HealthCheckResult.Unhealthy("Redis cache is not available", ex);
            }
        }
    }
}
```

## 8. Docker улучшения

### 8.1 Dockerfile

**Текущий Dockerfile:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "MailSender.API.dll"]
```

**Улучшенный Dockerfile:**
```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY *.sln ./
COPY */*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p ${file%.*}/ && mv $file ${file%.*}/; done
RUN dotnet restore

# Copy source code and build
COPY . .
RUN dotnet build -c Release --no-restore

# Publish stage  
FROM build AS publish
RUN dotnet publish "MailSender.API/MailSender.API.csproj" -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
RUN apk add --no-cache curl

# Create non-root user
RUN addgroup -g 1001 -S appgroup && adduser -S appuser -u 1001 -G appgroup
USER appuser

WORKDIR /app
COPY --from=publish --chown=appuser:appgroup /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:5000/health/api || exit 1

EXPOSE 5000
ENTRYPOINT ["dotnet", "MailSender.API.dll"]
```

### 8.2 docker-compose.yaml

**Улучшенный docker-compose.yaml:**
```yaml
version: '3.8'

services:
  mailsender:
    restart: unless-stopped
    image: IMAGE_NAME
    container_name: SERVICE_NAME
    command: dotnet MailSender.API.dll
    ports:
      - '61792:5000'
    environment:
      ASPNETCORE_ENVIRONMENT: DEPLOY_ENVIRONMENT
      ConnectionStrings__Redis: "redis:6379"
    logging:
      driver: json-file
      options:
        max-file: "5"
        max-size: 256m
    volumes:
      - /app/certs:/app/certs:ro
      - /app/SERVICE_NAME/secrets.json:/app/secrets.json:ro
      - /app/SERVICE_NAME/logs:/app/logs:rw
    depends_on:
      - redis
    networks:
      - mailsender-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:5000/health/api"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  redis:
    image: redis:7-alpine
    container_name: SERVICE_NAME_redis
    restart: unless-stopped
    ports:
      - '6379:6379'
    volumes:
      - redis_data:/data
    networks:
      - mailsender-network
    healthcheck:
      test: ["CMD", "redis-cli", "ping"]
      interval: 30s
      timeout: 10s
      retries: 3

volumes:
  redis_data:

networks:
  mailsender-network:
    driver: bridge
```

## Итоговые изменения

### Количественные показатели рефакторинга:

1. **Файлы для изменения:** 8 существующих файлов
2. **Новые файлы:** ~25 новых классов и интерфейсов  
3. **Удаленный код:** ~200 строк из монолитного сервиса
4. **Добавленный код:** ~1500 строк структурированного кода
5. **Новые зависимости:** 6 NuGet пакетов

### Последовательность внедрения:

1. **Фаза 1:** Изменить зависимости проектов и добавить интерфейсы
2. **Фаза 2:** Разделить MailSenderService на специализированные сервисы  
3. **Фаза 3:** Внедрить CQRS и MediatR
4. **Фаза 4:** Добавить кэширование и Background Services
5. **Фаза 5:** Внедрить абстракцию провайдеров и улучшить DevOps

Каждая фаза может быть выполнена независимо с сохранением работоспособности системы.