using AutoMapper;
using Dictionaries.Extensions;
using MailSender.Domain.DTOs;
using MailSender.Domain.Entities;
using MailSender.Domain.Enums;
using MailSender.Infrastructure.Repositories.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MailSender.Application.Services.AbstractClasses
{
    public abstract class SenderProtocolAbstractService
    {
        public abstract EmailSenderTypeEnum Type { get; }

        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;

        public SenderProtocolAbstractService(
            IMapper mapper,
            IUnitOfWork unitOfWork)
        {
            _mapper = mapper;
            _unitOfWork = unitOfWork;
        }

        public virtual async Task PreProcess(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken)
        {
            var message = await _unitOfWork.Messages.GetById(messageId, cancellationToken);
            if (message == null)
            {
                message = _mapper.Map<Message>(model);

                message.Id = messageId;

                message.StatusHistories.Add(new StatusHistory
                {
                    StatusId = (int)StatusEnum.Created
                });

                await _unitOfWork.Messages.Add(message, cancellationToken);

                await _unitOfWork.MailServiceComplete(cancellationToken);
            }
            else
            {
                message.Body = model.Body;
                message.Title = model.Title;

                await _unitOfWork.Messages.Update(message, cancellationToken);

                await WriteStatus(messageId, StatusEnum.TemplateReceived, null, cancellationToken);
            }
        }

        public virtual Task Process(Guid messageId, CancellationToken cancellationToken = default)
        {
            return WriteStatus(messageId, StatusEnum.StartSended, null, cancellationToken);
        }

        public virtual Task PostProcess(Guid messageId, Exception ex = null, CancellationToken cancellationToken = default)
        {
            if (ex == null)
            {
                return WriteStatus(messageId, StatusEnum.EndSended, null, cancellationToken);
            }
            else
            {
                return WriteStatus(messageId, StatusEnum.Error, ex.GetMessageFromException(), cancellationToken);
            }
        }

        public virtual Task RetryWork(Guid messageId, Exception ex = null, CancellationToken cancellationToken = default)
        {
            return WriteStatus(messageId, StatusEnum.Retry, ex?.GetMessageFromException(), cancellationToken);
        }

        public virtual async Task WriteStatus(
            Guid messageId,
            StatusEnum status,
            string description,
            CancellationToken cancellationToken = default)
        {
            await _unitOfWork.StatusHistory.Add(new StatusHistory
            {
                StatusId = (int)status,
                Description = description,
                MessageId = messageId
            }, cancellationToken);

            await _unitOfWork.MailServiceComplete(cancellationToken);
        }

        public abstract Task Send(MailInfoDTO model, Guid messageId, CancellationToken cancellationToken);
    }
}