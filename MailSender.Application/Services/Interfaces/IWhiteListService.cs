using System;
using System.Threading;
using System.Threading.Tasks;
using Dictionaries.DTO;
using MailSender.Domain.DTOs.EntitiesDTO;

namespace MailSender.Application.Services.Interfaces
{
    public interface IWhiteListService
    {
        public Task CheckWhiteList(string[] emails, CancellationToken cancellationToken);
        public Task Delete(Guid[] ids, CancellationToken cancellationToken);
        public Task<Guid> Create(WhiteListCreateDTO model, CancellationToken cancellationToken);
        public Task Update(Guid id, WhiteListUpdateDTO model, CancellationToken cancellationToken);
        public Task<WhiteListDTO> GetById(Guid id, CancellationToken cancellationToken);
        public Task<PaginatiedDataDTO<WhiteListDTO>> GetAll(QueryDataDTO model, CancellationToken cancellationToken);
    }
}