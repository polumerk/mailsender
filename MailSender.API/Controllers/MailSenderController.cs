using System;
using System.Threading;
using System.Threading.Tasks;
using Dictionaries.DTO;
using MailSender.Application.Services.Interfaces;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.Broker.Receive;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace MailSender.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class MailSenderController : ControllerBase
    {
        private readonly IOrchestratorService _orchestratorService;
        private readonly IMailSenderService _mailSenderService;

        public MailSenderController(
            IOrchestratorService orchestratorService,
            IMailSenderService mailSenderService)
        {
            _orchestratorService = orchestratorService;
            _mailSenderService = mailSenderService;
        }

        [HttpPost("ProcessMessage")]
        public async Task<IActionResult> Process([FromBody] KafkaMessageDTO<MailSenderDataDTO> model, CancellationToken cancellationToken = default)
        {
            var result = await _orchestratorService.Process(JsonConvert.SerializeObject(model), cancellationToken);

            if (result.Error != null)
            {
                return BadRequest(result.Error.Data.ErrorDescription);
            }

            return Ok();
        }

        [HttpPost("SendEmail")]
        public async Task<IActionResult> SendEmail([FromBody] MailInfoDTO model, CancellationToken cancellationToken = default)
        {
            await _mailSenderService.Send(model, Guid.NewGuid(), cancellationToken);

            return Ok();
        }
    }
}