using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.Entities;
using MailSender.Domain.Exceptions;
using MailSender.Domain.Localization;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MailSender.TestConstants.MockServices
{
    public class DuplicateEmptyService : IDuplicateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHashService _hashService;
        private readonly int _maxDuplicatesPerPeriod = 3;
        private readonly int _duplicatesPeriod = 0;

        public DuplicateEmptyService(
            IUnitOfWork unitOfWork,
            IHashService hashService)
        {
            _unitOfWork = unitOfWork;
            _hashService = hashService;
        }

        public async Task CheckDuplicate(MailSenderDataDTO model, CancellationToken cancellationToken)
        {
            // текущая дата обработки сообщения
            var currentDateTime = DateTime.UtcNow;
            // период, за который будем искать в кеше
            var periodDateTime = currentDateTime.AddSeconds(Math.Abs(_duplicatesPeriod) * (-1));
            // через сколько кеш будет сброшен
            var cacheExpiredDateTime = currentDateTime.AddSeconds(Math.Abs(_duplicatesPeriod));

            // получаем хеш сообщения, а именно блока даты
            var hash = _hashService.Hash(model);
            // унифицируем ключ кеша именно дубликата
            var cacheKey = string.Format(ApplicationConstants.CachePrefixForDuplicates, hash);

            var cacheImitation = await _unitOfWork.Messages.GetQueryData().Where(x => x.Title == cacheKey).ToListAsync(cancellationToken);

            var newMessage = new Message()
            {
                CreateDate = currentDateTime,
                Title = cacheKey,
                Email = model.Email
            };

            await _unitOfWork.Messages.Add(newMessage, cancellationToken);

            await _unitOfWork.MailServiceComplete(cancellationToken);

            // если найден уже запись с таким хешем
            if (cacheImitation.Any())
            {
                var resultCache = cacheImitation.Where(x => x.CreateDate >= periodDateTime).ToList();

                resultCache.Add(newMessage);

                if (resultCache.Count > _maxDuplicatesPerPeriod)
                {
                    throw new ValidationException(string.Format(CultureLoc.ValidationExceptionDuplicates, model.Email), ApplicationConstants.DUPLC);
                }
            }
        }
    }
}