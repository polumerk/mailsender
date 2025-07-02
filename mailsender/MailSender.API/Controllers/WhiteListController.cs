using System;
using System.Threading;
using System.Threading.Tasks;
using Dictionaries.DTO;
using Dictionaries.Enums;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.EntitiesDTO;
using MailSender.Domain.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MailSender.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class WhiteListController : ControllerBase
    {
        private readonly IWhiteListService _whiteListService;

        public WhiteListController(IWhiteListService whiteListService)
        {
            _whiteListService = whiteListService;
        }

        [Authorize(Policy = ApplicationConstants.PolicyRequireReadWhitelist)]
        [HttpPost("Get")]
        public async Task<IActionResult> GetAll([FromBody] QueryDataDTO model, CancellationToken cancellationToken = default)
        {
            var result = await _whiteListService.GetAll(model, cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = ApplicationConstants.PolicyRequireReadWhitelist)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
        {
            var result = await _whiteListService.GetById(id, cancellationToken);

            return Ok(result);
        }

        [Authorize(Policy = ApplicationConstants.PolicyRequireDeleteWhitelist)]
        [HttpDelete]
        public async Task<IActionResult> Delete(Guid[] ids, CancellationToken cancellationToken = default)
        {
            await _whiteListService.Delete(ids, cancellationToken);

            return Ok(new ResponseDTO<string>
            {
                Message = CultureLoc.MailIsDeleted,
                MessageType = MessageType.Success,
                Data = null
            });
        }

        [Authorize(Policy = ApplicationConstants.PolicyRequireCreateWhitelist)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] WhiteListCreateDTO model, CancellationToken cancellationToken = default)
        {
            var id = await _whiteListService.Create(model, cancellationToken);

            return Ok(new ResponseDTO<Guid>
            {
                Message = CultureLoc.MailIsAdded,
                MessageType = MessageType.Success,
                Data = id
            });
        }

        [Authorize(Policy = ApplicationConstants.PolicyRequireUpdateWhitelist)]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePut(Guid id, [FromBody] WhiteListUpdateDTO model, CancellationToken cancellationToken = default)
        {
            await _whiteListService.Update(id, model, cancellationToken);

            return Ok(new ResponseDTO<string>
            {
                Message = CultureLoc.MailIsUpdated,
                MessageType = MessageType.Success,
                Data = null
            });
        }
    }
}