using AutoMapper;
using Dictionaries.DTO;
using MailSender.Domain.DTOs;
using MailSender.Domain.DTOs.EntitiesDTO;
using MailSender.Domain.Entities;

namespace MailSender.Application.Mappings
{
    public class EntitiesProfile : Profile
    {
        public EntitiesProfile()
        {
            CreateMap<WhiteList, WhiteList>().ReverseMap();
            CreateMap<WhiteList, WhiteListDTO>();
            CreateMap<WhiteListCreateDTO, WhiteList>().ReverseMap();
            CreateMap<WhiteListUpdateDTO, WhiteList>().ReverseMap();

            CreateMap<PaginatiedDataDTO<WhiteList>, PaginatiedDataDTO<WhiteListDTO>>()
                .ForMember(dest => dest.Count, opt => opt.MapFrom(src => src.Count))
                .ForMember(dest => dest.Data, opt => opt.MapFrom(src => src.Data));

            CreateMap<MailInfoDTO, Message>()
                .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
                .ForMember(dest => dest.Body, opt => opt.MapFrom(src => src.Body))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.IsImportant, opt => opt.MapFrom(src => src.IsImportant));
        }
    }
}