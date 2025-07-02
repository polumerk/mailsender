using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Dictionaries;
using Dictionaries.DTO;
using Dictionaries.Extensions;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Configuration;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.EntitiesDTO;
using MailSender.Domain.Entities;
using MailSender.Domain.Exceptions;
using MailSender.Domain.Localization;
using MailSender.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Options;

namespace MailSender.Application.Services
{
    public class WhiteListService : IWhiteListService
    {
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly MailSettings _mailSettings;

        public WhiteListService(
            IMapper mapper,
            IUnitOfWork unitOfWork,
            IOptions<MailSettings> mailSettings)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
            _mailSettings = mailSettings.Value;
        }

        public async Task CheckWhiteList(string[] emails, CancellationToken cancellationToken)
        {
            if (_mailSettings.UseWhiteList)
            {
                List<string> whiteListBlockers = new();

                foreach (string email in emails)
                {
                    WhiteList whiteListEmail = await _unitOfWork.WhiteLists.GetByEmail(email, cancellationToken);

                    if (whiteListEmail == null)
                    {
                        whiteListBlockers.Add(email);
                    }
                }

                if (whiteListBlockers.Any())
                {
                    throw new ValidationException(string.Format(CultureLoc.ValidationExceptionWhiteList, string.Join("; ", whiteListBlockers)), codeResult: ApplicationConstants.WLUAT);
                }
            }
        }

        public async Task<PaginatiedDataDTO<WhiteListDTO>> GetAll(QueryDataDTO model, CancellationToken cancellationToken)
        {
            return await _unitOfWork.WhiteLists.GetQueryData()
                .ProjectTo<WhiteListDTO>(_mapper.ConfigurationProvider)
                .AppendFilterAndPagination(model, cancellationToken);
        }

        public async Task<WhiteListDTO> GetById(Guid id, CancellationToken cancellationToken)
        {
            WhiteList result = await _unitOfWork.WhiteLists.GetById(id, cancellationToken);

            if (result == null)
            {
                throw new NotFoundException(string.Format(CultureLoc.NotFoundException, nameof(WhiteList), id), codeResult: CodeResults.SYSER);
            }
            return _mapper.Map<WhiteListDTO>(result);
        }

        public async Task Update(Guid id, WhiteListUpdateDTO model, CancellationToken cancellationToken)
        {
            WhiteList whiteListDb = await _unitOfWork.WhiteLists.GetById(id, cancellationToken);

            if (whiteListDb == null)
            {
                throw new NotFoundException(string.Format(CultureLoc.NotFoundException, nameof(WhiteList), id), codeResult: CodeResults.SYSER);
            }

            var existEmail = await _unitOfWork.WhiteLists.GetByEmail(model.Email.Trim(), cancellationToken);

            if (existEmail != null && existEmail.Id != whiteListDb.Id)
            {
                throw new ValidationException(CultureLoc.ValidationExceptionEmailExist, codeResult: CodeResults.SYSER);
            }
            _mapper.Map(model, whiteListDb);

            whiteListDb.Email = whiteListDb.Email.Trim();

            await _unitOfWork.MailServiceComplete(cancellationToken);
        }

        public async Task<Guid> Create(WhiteListCreateDTO model, CancellationToken cancellationToken)
        {
            var existEmail = await _unitOfWork.WhiteLists.GetByEmail(model.Email.Trim(), cancellationToken);
            if(existEmail != null)
            {
                throw new ValidationException(CultureLoc.ValidationExceptionEmailExist, codeResult: CodeResults.SYSER);
            }
            WhiteList obj = _mapper.Map<WhiteList>(model);

            obj.Email = obj.Email.Trim();

            await _unitOfWork.WhiteLists.Add(obj, cancellationToken);

            await _unitOfWork.MailServiceComplete(cancellationToken);

            return obj.Id;

        }

        public async Task Delete(Guid[] ids, CancellationToken cancellationToken)
        {
            var objs = await _unitOfWork.WhiteLists.GetByIds(ids, cancellationToken);

            if (objs?.Any() != true)
            {
                throw new ValidationException(CultureLoc.ValidationExceptionDeleted, codeResult: CodeResults.SYSER);
            }

            foreach (var obj in objs)
            {
                obj.IsDeleted = true;

                await _unitOfWork.WhiteLists.Update(obj, cancellationToken);
            }

            await _unitOfWork.MailServiceComplete(cancellationToken);
        }
    }
}