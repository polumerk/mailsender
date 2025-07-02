using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Configuration;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.DTOs.Cache;
using MailSender.Domain.Exceptions;
using MailSender.Domain.Localization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Application.Services
{
    public class DuplicateService : IDuplicateService
    {
        private readonly IMemoryCache _cache;
        private readonly IHashService _hashService;
        private readonly MailSettings _mailSettings;

        public DuplicateService(
            IMemoryCache cache,
            IHashService hashService,
            IOptions<MailSettings> mailSettings)
        {
            _cache = cache;
            _hashService = hashService;
            _mailSettings = mailSettings.Value;
        }

        public Task CheckDuplicate(MailSenderDataDTO model, CancellationToken cancellationToken)
        {
            if (!_mailSettings.MaxDuplicatesPerPeriod.HasValue || !_mailSettings.DuplicatesPeriod.HasValue)
            {
                return Task.CompletedTask;
            }

            // текущая дата обработки сообщения
            var currentDateTime = DateTime.UtcNow;
            // период, за который будем искать в кеше
            var periodDateTime = currentDateTime.AddSeconds(Math.Abs(_mailSettings.DuplicatesPeriod.Value) * (-1));
            // через сколько кеш будет сброшен
            var cacheExpiredDateTime = currentDateTime.AddSeconds(Math.Abs(_mailSettings.DuplicatesPeriod.Value));

            // получаем хеш сообщения, а именно блока даты
            var hash = _hashService.Hash(model);
            // унифицируем ключ кеша именно дубликата
            var cacheKey = string.Format(ApplicationConstants.CachePrefixForDuplicates, hash);

            // если найден уже запись с таким хешем
            if (_cache.TryGetValue(cacheKey, out List<DuplicateDTO> cacheValue))
            {
                cacheValue ??= new List<DuplicateDTO>();

                var resultCache = cacheValue.Where(x => x.CreateDate >= periodDateTime).ToList();

                resultCache.Add(new DuplicateDTO() { CreateDate = currentDateTime });

                _cache.Set(cacheKey, resultCache, cacheExpiredDateTime);

                if (resultCache.Count > _mailSettings.MaxDuplicatesPerPeriod)
                {
                    throw new ValidationException(string.Format(CultureLoc.ValidationExceptionDuplicates, model.Email), ApplicationConstants.DUPLC);
                }
            }
            else
            {
                _cache.Set(cacheKey, new List<DuplicateDTO>() { new() { CreateDate = currentDateTime } }, cacheExpiredDateTime);
            }

            return Task.CompletedTask;
        }
    }
}