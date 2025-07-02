# План рефакторинга проекта MailSender

## 1. Исправление архитектурных зависимостей

### 1.1 Текущая проблема
```csharp
// ❌ НЕПРАВИЛЬНО: Application.csproj ссылается на Infrastructure
<ProjectReference Include="..\MailSender.Infrastructure\MailSender.Infrastructure.csproj" />
```

### 1.2 Решение - Инверсия зависимостей

**Шаг 1: Создать абстракции в Application слое**

```csharp
// MailSender.Application/Interfaces/IEmailRepository.cs
namespace MailSender.Application.Interfaces
{
    public interface IEmailRepository
    {
        Task<Message> GetByTemplateMessageIdAsync(Guid templateMessageId, CancellationToken cancellationToken);
        Task AddAsync(Message message, CancellationToken cancellationToken);
        Task UpdateAsync(Message message, CancellationToken cancellationToken);
    }

    public interface IEmailUnitOfWork
    {
        IEmailRepository Messages { get; }
        IStatusHistoryRepository StatusHistory { get; }
        IWhiteListRepository WhiteList { get; }
        Task<int> SaveChangesAsync(CancellationToken cancellationToken);
    }
}
```

**Шаг 2: Обновить зависимости проектов**

```xml
<!-- MailSender.Application.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  
  <ItemGroup>
    <ProjectReference Include="..\MailSender.Domain\MailSender.Domain.csproj" />
    <!-- Убрать ссылку на Infrastructure -->
  </ItemGroup>
</Project>

<!-- MailSender.Infrastructure.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\MailSender.Domain\MailSender.Domain.csproj" />
    <ProjectReference Include="..\MailSender.Application\MailSender.Application.csproj" />
  </ItemGroup>
</Project>
```

## 2. Разделение монолитного MailSenderService

### 2.1 Текущая проблема
```csharp
// ❌ ПРОБЛЕМА: Один сервис с множественной ответственностью (314 строк)
public class MailSenderService : IMailSenderService
{
    // Валидация email
    // Проверка дубликатов  
    // Отправка писем
    // Обработка шаблонов
    // Создание уведомлений
}
```

### 2.2 Решение - Разделение по принципу SRP

**Шаг 1: Создать специализированные сервисы**

```csharp
// MailSender.Application/Services/EmailValidationService.cs
public interface IEmailValidationService
{
    Task<string[]> ValidateEmailsAsync(string email, CancellationToken cancellationToken);
    Task CheckWhiteListAsync(string[] emails, CancellationToken cancellationToken);
}

public class EmailValidationService : IEmailValidationService
{
    private readonly IWhiteListService _whiteListService;
    private readonly ILogger<EmailValidationService> _logger;

    public EmailValidationService(
        IWhiteListService whiteListService,
        ILogger<EmailValidationService> logger)
    {
        _whiteListService = whiteListService;
        _logger = logger;
    }

    public async Task<string[]> ValidateEmailsAsync(string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ValidationException(CultureLoc.MailIsEmpty, codeResult: ApplicationConstants.EMEMT);

        var emails = email.Split(ApplicationConstants.EmailSeparate, StringSplitOptions.TrimEntries);
        
        // Валидация формата email
        foreach (var emailAddress in emails)
        {
            if (!IsValidEmail(emailAddress))
                throw new ValidationException($"Invalid email format: {emailAddress}");
        }

        await CheckWhiteListAsync(emails, cancellationToken);
        return emails;
    }

    public async Task CheckWhiteListAsync(string[] emails, CancellationToken cancellationToken)
    {
        await _whiteListService.CheckWhiteList(emails, cancellationToken);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
```

```csharp
// MailSender.Application/Services/EmailDeliveryService.cs
public interface IEmailDeliveryService
{
    Task SendAsync(MailInfoDTO mailInfo, Guid messageId, CancellationToken cancellationToken);
    Task SendBulkAsync(IEnumerable<MailInfoDTO> emails, CancellationToken cancellationToken);
}

public class EmailDeliveryService : IEmailDeliveryService
{
    private readonly SenderProtocolAbstractService _senderService;
    private readonly IEmailValidationService _validationService;
    private readonly ILogger<EmailDeliveryService> _logger;

    public EmailDeliveryService(
        SenderProtocolAbstractService senderService,
        IEmailValidationService validationService,
        ILogger<EmailDeliveryService> logger)
    {
        _senderService = senderService;
        _validationService = validationService;
        _logger = logger;
    }

    public async Task SendAsync(MailInfoDTO mailInfo, Guid messageId, CancellationToken cancellationToken)
    {
        var validatedEmails = await _validationService.ValidateEmailsAsync(mailInfo.Email, cancellationToken);
        mailInfo.ValidatedEmails = validatedEmails;

        await _senderService.Send(mailInfo, messageId, cancellationToken);
        
        _logger.LogInformation("Email sent successfully to {EmailCount} recipients", validatedEmails.Length);
    }

    public async Task SendBulkAsync(IEnumerable<MailInfoDTO> emails, CancellationToken cancellationToken)
    {
        var tasks = emails.Select(email => SendAsync(email, Guid.NewGuid(), cancellationToken));
        await Task.WhenAll(tasks);
        
        _logger.LogInformation("Bulk email sending completed for {EmailCount} emails", emails.Count());
    }
}
```

```csharp
// MailSender.Application/Services/TemplateProcessingService.cs
public interface ITemplateProcessingService
{
    Task<ProcessedTemplateResult> ProcessTemplateResponseAsync(string message, CancellationToken cancellationToken);
}

public class TemplateProcessingService : ITemplateProcessingService
{
    private readonly IEmailUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly MailSettings _mailSettings;
    private readonly ILogger<TemplateProcessingService> _logger;

    public TemplateProcessingService(
        IEmailUnitOfWork unitOfWork,
        IMapper mapper,
        IOptions<MailSettings> mailSettings,
        ILogger<TemplateProcessingService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _mailSettings = mailSettings.Value;
        _logger = logger;
    }

    public async Task<ProcessedTemplateResult> ProcessTemplateResponseAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var templateModel = JsonConvert.DeserializeObject<KafkaMessageDTO<TemplateServiceDTO>>(message);
            _logger.LogInformation("Processing template response for message {MessageId}", templateModel.MessageInfo.ParentMessageId);

            var mailMessage = await _unitOfWork.Messages.GetByTemplateMessageIdAsync(
                templateModel.MessageInfo.ParentMessageId.Value, cancellationToken);

            if (mailMessage == null)
            {
                throw new NotFoundException(
                    $"Message not found: {templateModel.MessageInfo.ParentMessageId.Value}");
            }

            if (templateModel.Data.ResultCode != ApplicationConstants.SUCCESS)
            {
                await HandleTemplateErrorAsync(mailMessage, templateModel.Data, cancellationToken);
                return ProcessedTemplateResult.Error(templateModel.Data.ResultDescription);
            }

            var processedContent = await ProcessTemplateContentAsync(mailMessage, cancellationToken);
            return ProcessedTemplateResult.Success(processedContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template response");
            return ProcessedTemplateResult.Error(ex.Message);
        }
    }

    private async Task<MailInfoDTO> ProcessTemplateContentAsync(Message mailMessage, CancellationToken cancellationToken)
    {
        var templateOutput = await _unitOfWork.Outputs.GetByMessageIdAsync(
            mailMessage.TemplateMessageId.Value, cancellationToken);

        var content = Encoding.UTF8.GetString(templateOutput.ContentInfo.Content);
        var prefix = string.IsNullOrWhiteSpace(_mailSettings.PrefixTitle) ? string.Empty : _mailSettings.PrefixTitle;

        var title = ExtractTitleFromContent(content);
        var body = ExtractBodyFromContent(content, title);

        return new MailInfoDTO
        {
            Title = prefix + title,
            Body = body,
            IsImportant = mailMessage.IsImportant,
            Attachments = DeserializeAttachments(mailMessage.Attachments),
            Email = mailMessage.Email
        };
    }
}
```

```csharp
// MailSender.Application/Services/DuplicateCheckService.cs
public interface IDuplicateCheckService
{
    Task CheckForDuplicateAsync(MailSenderDataDTO model, CancellationToken cancellationToken);
}

public class DuplicateCheckService : IDuplicateCheckService
{
    private readonly IMemoryCache _cache;
    private readonly IHashService _hashService;
    private readonly ILogger<DuplicateCheckService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);

    public DuplicateCheckService(
        IMemoryCache cache,
        IHashService hashService,
        ILogger<DuplicateCheckService> logger)
    {
        _cache = cache;
        _hashService = hashService;
        _logger = logger;
    }

    public async Task CheckForDuplicateAsync(MailSenderDataDTO model, CancellationToken cancellationToken)
    {
        var hash = _hashService.GenerateHash(model);
        var cacheKey = $"email_duplicate_{hash}";

        if (_cache.TryGetValue(cacheKey, out _))
        {
            _logger.LogWarning("Duplicate email detected for hash {Hash}", hash);
            throw new DuplicateException("Duplicate email request detected");
        }

        _cache.Set(cacheKey, true, _cacheExpiration);
        _logger.LogDebug("Email request cached with hash {Hash}", hash);
    }
}
```

**Шаг 2: Обновленный MailSenderService (Orchestrator)**

```csharp
// MailSender.Application/Services/MailSenderService.cs
public class MailSenderService : IMailSenderService
{
    private readonly IEmailValidationService _validationService;
    private readonly IEmailDeliveryService _deliveryService;
    private readonly ITemplateProcessingService _templateService;
    private readonly IDuplicateCheckService _duplicateService;
    private readonly IBrokerMessageCreatorService _brokerMessageCreator;
    private readonly ILogger<MailSenderService> _logger;

    public MailSenderService(
        IEmailValidationService validationService,
        IEmailDeliveryService deliveryService,
        ITemplateProcessingService templateService,
        IDuplicateCheckService duplicateService,
        IBrokerMessageCreatorService brokerMessageCreator,
        ILogger<MailSenderService> logger)
    {
        _validationService = validationService;
        _deliveryService = deliveryService;
        _templateService = templateService;
        _duplicateService = duplicateService;
        _brokerMessageCreator = brokerMessageCreator;
        _logger = logger;
    }

    public async Task SendAsync(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting email send process for message {MessageId}", messageId);
        
        await _deliveryService.SendAsync(model, messageId, cancellationToken);
        
        _logger.LogInformation("Email send process completed for message {MessageId}", messageId);
    }

    public async Task<BrokerMessage> ProcessRequestAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var model = JsonConvert.DeserializeObject<KafkaMessageDTO<MailSenderDataDTO>>(message);
            
            // Валидация
            await _validationService.ValidateEmailsAsync(model.Data.Email, cancellationToken);
            
            // Проверка дубликатов
            await _duplicateService.CheckForDuplicateAsync(model.Data, cancellationToken);
            
            // Создание шаблона
            var templateNotification = _brokerMessageCreator.CreateTemplateService(model);
            
            return new BrokerMessage { TemplateNotification = templateNotification };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email request");
            return new BrokerMessage { Error = _brokerMessageCreator.CreateError(ex) };
        }
    }

    public async Task<BrokerMessage> ProcessResponseAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _templateService.ProcessTemplateResponseAsync(message, cancellationToken);
            
            if (result.IsSuccess)
            {
                await _deliveryService.SendAsync(result.MailInfo, Guid.NewGuid(), cancellationToken);
                return new BrokerMessage { Notification = _brokerMessageCreator.CreateNotification(result.MailInfo) };
            }
            
            return new BrokerMessage { Error = _brokerMessageCreator.CreateError(result.ErrorMessage) };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing template response");
            return new BrokerMessage { Error = _brokerMessageCreator.CreateError(ex) };
        }
    }
}
```

## 3. Внедрение CQRS паттерна

### 3.1 Создание команд и запросов

```csharp
// MailSender.Application/Features/Commands/SendEmailCommand.cs
public record SendEmailCommand(
    string Email,
    string Title,
    string Body,
    bool IsImportant = false,
    MailAttachmentDTO[]? Attachments = null) : IRequest<SendEmailResult>;

public record SendEmailResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    Guid? MessageId = null);

public class SendEmailCommandHandler : IRequestHandler<SendEmailCommand, SendEmailResult>
{
    private readonly IEmailDeliveryService _deliveryService;
    private readonly ILogger<SendEmailCommandHandler> _logger;

    public SendEmailCommandHandler(
        IEmailDeliveryService deliveryService,
        ILogger<SendEmailCommandHandler> logger)
    {
        _deliveryService = deliveryService;
        _logger = logger;
    }

    public async Task<SendEmailResult> Handle(SendEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var messageId = Guid.NewGuid();
            var mailInfo = new MailInfoDTO
            {
                Email = request.Email,
                Title = request.Title,
                Body = request.Body,
                IsImportant = request.IsImportant,
                Attachments = request.Attachments
            };

            await _deliveryService.SendAsync(mailInfo, messageId, cancellationToken);
            
            _logger.LogInformation("Email sent successfully with message ID {MessageId}", messageId);
            
            return new SendEmailResult(true, MessageId: messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email");
            return new SendEmailResult(false, ex.Message);
        }
    }
}
```

```csharp
// MailSender.Application/Features/Commands/ProcessTemplateCommand.cs
public record ProcessTemplateCommand(string KafkaMessage) : IRequest<BrokerMessage>;

public class ProcessTemplateCommandHandler : IRequestHandler<ProcessTemplateCommand, BrokerMessage>
{
    private readonly ITemplateProcessingService _templateService;
    private readonly IBrokerMessageCreatorService _brokerMessageCreator;

    public ProcessTemplateCommandHandler(
        ITemplateProcessingService templateService,
        IBrokerMessageCreatorService brokerMessageCreator)
    {
        _templateService = templateService;
        _brokerMessageCreator = brokerMessageCreator;
    }

    public async Task<BrokerMessage> Handle(ProcessTemplateCommand request, CancellationToken cancellationToken)
    {
        var result = await _templateService.ProcessTemplateResponseAsync(request.KafkaMessage, cancellationToken);
        
        if (result.IsSuccess)
        {
            return new BrokerMessage 
            { 
                Notification = _brokerMessageCreator.CreateNotification(result.MailInfo) 
            };
        }
        
        return new BrokerMessage 
        { 
            Error = _brokerMessageCreator.CreateError(result.ErrorMessage) 
        };
    }
}
```

```csharp
// MailSender.Application/Features/Queries/GetEmailStatusQuery.cs
public record GetEmailStatusQuery(Guid MessageId) : IRequest<EmailStatusResult>;

public record EmailStatusResult(
    Guid MessageId,
    EmailStatus Status,
    string? ErrorMessage = null,
    DateTime CreatedAt = default,
    DateTime? SentAt = null);

public class GetEmailStatusQueryHandler : IRequestHandler<GetEmailStatusQuery, EmailStatusResult>
{
    private readonly IEmailUnitOfWork _unitOfWork;

    public GetEmailStatusQueryHandler(IEmailUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<EmailStatusResult> Handle(GetEmailStatusQuery request, CancellationToken cancellationToken)
    {
        var message = await _unitOfWork.Messages.GetByIdAsync(request.MessageId, cancellationToken);
        
        if (message == null)
            throw new NotFoundException($"Message {request.MessageId} not found");

        var latestStatus = message.StatusHistories.OrderByDescending(s => s.CreatedAt).First();
        
        return new EmailStatusResult(
            message.Id,
            (EmailStatus)latestStatus.StatusId,
            latestStatus.Description,
            message.CreatedAt,
            latestStatus.StatusId == (int)StatusEnum.Sent ? latestStatus.CreatedAt : null);
    }
}
```

### 3.2 Обновленный контроллер

```csharp
// MailSender.API/Controllers/MailSenderController.cs
[Authorize]
[ApiController]
[Route("[controller]")]
public class MailSenderController : ControllerBase
{
    private readonly IMediator _mediator;

    public MailSenderController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendEmail([FromBody] SendEmailCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.IsSuccess)
            return Ok(new { MessageId = result.MessageId });
            
        return BadRequest(result.ErrorMessage);
    }

    [HttpPost("process-template")]
    public async Task<IActionResult> ProcessTemplate([FromBody] ProcessTemplateCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        
        if (result.Error != null)
            return BadRequest(result.Error);
            
        return Ok();
    }

    [HttpGet("status/{messageId}")]
    public async Task<IActionResult> GetEmailStatus(Guid messageId, CancellationToken cancellationToken)
    {
        var query = new GetEmailStatusQuery(messageId);
        var result = await _mediator.Send(query, cancellationToken);
        
        return Ok(result);
    }
}
```

## 4. Внедрение кэширования

### 4.1 Кэширование WhiteList проверок

```csharp
// MailSender.Application/Services/CachedWhiteListService.cs
public class CachedWhiteListService : IWhiteListService
{
    private readonly IWhiteListService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedWhiteListService> _logger;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public CachedWhiteListService(
        IWhiteListService innerService,
        IMemoryCache cache,
        ILogger<CachedWhiteListService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task CheckWhiteList(string[] emails, CancellationToken cancellationToken)
    {
        var uncachedEmails = new List<string>();
        
        foreach (var email in emails)
        {
            var cacheKey = $"whitelist_{email.ToLowerInvariant()}";
            
            if (!_cache.TryGetValue(cacheKey, out bool isAllowed))
            {
                uncachedEmails.Add(email);
            }
            else if (!isAllowed)
            {
                throw new ValidationException($"Email {email} is not in whitelist");
            }
        }

        if (uncachedEmails.Any())
        {
            try
            {
                await _innerService.CheckWhiteList(uncachedEmails.ToArray(), cancellationToken);
                
                // Кэшируем разрешенные email
                foreach (var email in uncachedEmails)
                {
                    var cacheKey = $"whitelist_{email.ToLowerInvariant()}";
                    _cache.Set(cacheKey, true, _cacheExpiration);
                }
                
                _logger.LogDebug("Cached whitelist status for {EmailCount} emails", uncachedEmails.Count);
            }
            catch (ValidationException)
            {
                // Кэшируем запрещенные email
                foreach (var email in uncachedEmails)
                {
                    var cacheKey = $"whitelist_{email.ToLowerInvariant()}";
                    _cache.Set(cacheKey, false, _cacheExpiration);
                }
                throw;
            }
        }
    }
}
```

### 4.2 Распределенное кэширование с Redis

```csharp
// MailSender.Application/Services/DistributedCacheService.cs
public interface IDistributedCacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}

public class DistributedCacheService : IDistributedCacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(
        IDistributedCache distributedCache,
        ILogger<DistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cached = await _distributedCache.GetStringAsync(key, cancellationToken);
            
            if (cached == null)
                return null;

            return JsonConvert.DeserializeObject<T>(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serialized = JsonConvert.SerializeObject(value);
            var options = new DistributedCacheEntryOptions();
            
            if (expiration.HasValue)
                options.SetAbsoluteExpiration(expiration.Value);

            await _distributedCache.SetStringAsync(key, serialized, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _distributedCache.RemoveAsync(key, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key {Key}", key);
        }
    }
}
```

## 5. Внедрение Background Services для асинхронной обработки

### 5.1 Email Queue Service

```csharp
// MailSender.Application/Services/EmailQueueService.cs
public interface IEmailQueueService
{
    Task EnqueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default);
    Task<EmailQueueItem?> DequeueAsync(CancellationToken cancellationToken = default);
}

public record EmailQueueItem(
    Guid MessageId,
    MailInfoDTO MailInfo,
    int RetryCount = 0,
    DateTime EnqueuedAt = default);

public class EmailQueueService : IEmailQueueService
{
    private readonly Channel<EmailQueueItem> _channel;
    private readonly ChannelWriter<EmailQueueItem> _writer;
    private readonly ChannelReader<EmailQueueItem> _reader;

    public EmailQueueService(int capacity = 1000)
    {
        var options = new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        };

        _channel = Channel.CreateBounded<EmailQueueItem>(options);
        _writer = _channel.Writer;
        _reader = _channel.Reader;
    }

    public async Task EnqueueAsync(EmailQueueItem item, CancellationToken cancellationToken = default)
    {
        await _writer.WriteAsync(item, cancellationToken);
    }

    public async Task<EmailQueueItem?> DequeueAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _reader.ReadAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
```

### 5.2 Background Email Processing Service

```csharp
// MailSender.Application/Services/EmailProcessingBackgroundService.cs
public class EmailProcessingBackgroundService : BackgroundService
{
    private readonly IEmailQueueService _queueService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EmailProcessingBackgroundService> _logger;

    public EmailProcessingBackgroundService(
        IEmailQueueService queueService,
        IServiceProvider serviceProvider,
        ILogger<EmailProcessingBackgroundService> logger)
    {
        _queueService = queueService;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Email processing background service started");

        await foreach (var item in GetQueueItemsAsync(stoppingToken))
        {
            await ProcessEmailAsync(item, stoppingToken);
        }

        _logger.LogInformation("Email processing background service stopped");
    }

    private async IAsyncEnumerable<EmailQueueItem> GetQueueItemsAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var item = await _queueService.DequeueAsync(cancellationToken);
            if (item != null)
                yield return item;
        }
    }

    private async Task ProcessEmailAsync(EmailQueueItem item, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var deliveryService = scope.ServiceProvider.GetRequiredService<IEmailDeliveryService>();

        try
        {
            _logger.LogInformation("Processing email for message {MessageId}", item.MessageId);
            
            await deliveryService.SendAsync(item.MailInfo, item.MessageId, cancellationToken);
            
            _logger.LogInformation("Email processed successfully for message {MessageId}", item.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing email for message {MessageId}", item.MessageId);
            
            if (item.RetryCount < 3)
            {
                var retryItem = item with { RetryCount = item.RetryCount + 1 };
                await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, item.RetryCount)), cancellationToken);
                await _queueService.EnqueueAsync(retryItem, cancellationToken);
                
                _logger.LogInformation("Email requeued for retry {RetryCount} for message {MessageId}", 
                    retryItem.RetryCount, item.MessageId);
            }
            else
            {
                _logger.LogError("Max retry attempts reached for message {MessageId}", item.MessageId);
                // Send to dead letter queue or alert
            }
        }
    }
}
```

## 6. Внедрение абстракции Email провайдеров

### 6.1 Абстракция провайдеров

```csharp
// MailSender.Application/Interfaces/IEmailProvider.cs
public interface IEmailProvider
{
    string Name { get; }
    Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
    bool CanHandle(string emailAddress);
}

public record EmailMessage(
    string[] To,
    string Subject,
    string Body,
    string? From = null,
    MailAttachmentDTO[]? Attachments = null,
    bool IsImportant = false);

public record EmailSendResult(
    bool IsSuccess,
    string? ErrorMessage = null,
    string? MessageId = null,
    DateTime SentAt = default);
```

### 6.2 Реализация SMTP провайдера

```csharp
// MailSender.Infrastructure/Providers/SmtpEmailProvider.cs
public class SmtpEmailProvider : IEmailProvider
{
    public string Name => "SMTP";
    
    private readonly SmtpSettings _smtpSettings;
    private readonly ILogger<SmtpEmailProvider> _logger;

    public SmtpEmailProvider(
        IOptions<SmtpSettings> smtpSettings,
        ILogger<SmtpEmailProvider> logger)
    {
        _smtpSettings = smtpSettings.Value;
        _logger = logger;
    }

    public async Task<EmailSendResult> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var smtpClient = CreateSmtpClient();
            using var mailMessage = CreateMailMessage(message);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            
            _logger.LogInformation("Email sent via SMTP to {Recipients}", string.Join(", ", message.To));
            
            return new EmailSendResult(true, MessageId: Guid.NewGuid().ToString(), SentAt: DateTime.UtcNow);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email via SMTP");
            return new EmailSendResult(false, ex.Message);
        }
    }

    public bool CanHandle(string emailAddress)
    {
        // Проверяем по домену или другим критериям
        return !string.IsNullOrEmpty(emailAddress);
    }

    private SmtpClient CreateSmtpClient()
    {
        return new SmtpClient(_smtpSettings.Host, _smtpSettings.Port)
        {
            EnableSsl = _smtpSettings.EnableSsl,
            Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password)
        };
    }

    private MailMessage CreateMailMessage(EmailMessage message)
    {
        var mailMessage = new MailMessage
        {
            From = new MailAddress(message.From ?? _smtpSettings.DefaultFromAddress),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = true,
            Priority = message.IsImportant ? MailPriority.High : MailPriority.Normal
        };

        foreach (var to in message.To)
        {
            mailMessage.To.Add(to);
        }

        if (message.Attachments != null)
        {
            foreach (var attachment in message.Attachments)
            {
                var stream = new MemoryStream(attachment.Content);
                mailMessage.Attachments.Add(new Attachment(stream, attachment.FileName, attachment.ContentType));
            }
        }

        return mailMessage;
    }
}
```

### 6.3 Email Provider Factory

```csharp
// MailSender.Application/Services/EmailProviderFactory.cs
public interface IEmailProviderFactory
{
    IEmailProvider GetProvider(string emailAddress);
    IEmailProvider GetProvider(string providerName);
}

public class EmailProviderFactory : IEmailProviderFactory
{
    private readonly IEnumerable<IEmailProvider> _providers;
    private readonly ILogger<EmailProviderFactory> _logger;

    public EmailProviderFactory(
        IEnumerable<IEmailProvider> providers,
        ILogger<EmailProviderFactory> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    public IEmailProvider GetProvider(string emailAddress)
    {
        var provider = _providers.FirstOrDefault(p => p.CanHandle(emailAddress));
        
        if (provider == null)
        {
            _logger.LogWarning("No provider found for email {Email}, using default", emailAddress);
            provider = _providers.First(); // Default provider
        }

        _logger.LogDebug("Selected provider {ProviderName} for email {Email}", provider.Name, emailAddress);
        return provider;
    }

    public IEmailProvider GetProvider(string providerName)
    {
        var provider = _providers.FirstOrDefault(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
        
        if (provider == null)
            throw new InvalidOperationException($"Provider {providerName} not found");

        return provider;
    }
}
```

## 7. Регистрация сервисов

### 7.1 Обновленный ConfigureServices в Application

```csharp
// MailSender.Application/ConfigureServices.cs
public static class ConfigureServices
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        // Validators
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        
        // Core Services
        services.AddScoped<IEmailValidationService, EmailValidationService>();
        services.AddScoped<IEmailDeliveryService, EmailDeliveryService>();
        services.AddScoped<ITemplateProcessingService, TemplateProcessingService>();
        services.AddScoped<IDuplicateCheckService, DuplicateCheckService>();
        services.AddScoped<IDistributedCacheService, DistributedCacheService>();
        
        // Decorators
        services.Decorate<IWhiteListService, CachedWhiteListService>();
        
        // Background Services
        services.AddSingleton<IEmailQueueService, EmailQueueService>();
        services.AddHostedService<EmailProcessingBackgroundService>();
        
        // Email Providers
        services.AddScoped<IEmailProvider, SmtpEmailProvider>();
        services.AddScoped<IEmailProviderFactory, EmailProviderFactory>();
        
        // Caching
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });
        
        // AutoMapper
        services.AddAutoMapper(Assembly.GetExecutingAssembly());
        
        return services;
    }
}
```

## 8. Ожидаемые результаты рефакторинга

### 8.1 Архитектурные улучшения
- ✅ Исправлены зависимости между слоями
- ✅ Разделение обязанностей (SRP)
- ✅ Внедрена инверсия зависимостей (DIP)
- ✅ Применен CQRS паттерн

### 8.2 Производительность
- ✅ Кэширование WhiteList проверок
- ✅ Асинхронная обработка очередей
- ✅ Распределенное кэширование с Redis
- ✅ Background Services для длительных операций

### 8.3 Масштабируемость
- ✅ Абстракция email провайдеров
- ✅ Factory паттерн для выбора провайдеров
- ✅ Очередь сообщений с retry механизмом
- ✅ Горизонтальное масштабирование

### 8.4 Поддерживаемость
- ✅ Четкое разделение ответственностей
- ✅ Легкость тестирования
- ✅ Простота добавления новых функций
- ✅ Снижение технического долга