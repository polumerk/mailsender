using AutoMapper;
using MailSender.Domain.Constants;
using MailSender.Domain.DTOs.Broker.Receive;
using MailSender.Domain.DTOs.Broker.Send;
using MailSender.Domain.DTOs.Broker.TemplateService;
using MailSender.Domain.Enums;

namespace MailSender.Application.Mappings
{
    public class BrokerProfile : Profile
    {
        public BrokerProfile()
        {
            CreateMap<MailSenderDataDTO, NotificationEmailDataDTO>()
                .ForMember(dest => dest.NotificationType, opt => opt.MapFrom(src => ApplicationConstants.ApplicationNotificationType))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src));

            CreateMap<MailSenderDataDTO, NotificationEmailContentDataDTO>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.Attachments, opt => opt.MapFrom(src => src.Attachments));

            // TemlateService
            CreateMap<MailSenderDataDTO, TemplateServiceDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.MailTemplateId))
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.MailTemplateTitle))
                .ForMember(dest => dest.AccessableTypes, opt => opt.MapFrom(src => new[] { TypeEnum.HtmlEmail }))
                .ForMember(dest => dest.ReplaceFields, opt => opt.MapFrom(src => src.ReplaceFields))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version));

            CreateMap<TemplateServiceDTO, MailSenderDataDTO>()
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.Attachments, opt => opt.Ignore())
                .ForMember(dest => dest.MailTemplateId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.MailTemplateTitle, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.ReplaceFields, opt => opt.MapFrom(src => src.ReplaceFields))
                .ForMember(dest => dest.Version, opt => opt.MapFrom(src => src.Version));
        }
    }
}