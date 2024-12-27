using AutoMapper;
using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers.ProviderOne
{
    public class ProviderOneMappingProfile : Profile
    {
        public ProviderOneMappingProfile()
        {

            CreateMap<SearchRequest, ProviderOneSearchRequest>()
                .ForMember(dest => dest.From, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.To, opt => opt.MapFrom(src => src.Destination))
                .ForMember(dest => dest.DateFrom, opt => opt.MapFrom(src => src.OriginDateTime))
                .ForMember(dest => dest.DateTo, opt => opt.MapFrom(src => src.Filters != null ? src.Filters.DestinationDateTime : null))
                .ForMember(dest => dest.MaxPrice, opt => opt.MapFrom(src => src.Filters != null ? src.Filters.MaxPrice : null));

            CreateMap<ProviderOneRoute, Route>()
                        .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.From))
                        .ForMember(dest => dest.Destination, opt => opt.MapFrom(src => src.To))
                        .ForMember(dest => dest.OriginDateTime, opt => opt.MapFrom(src => src.DateFrom))
                        .ForMember(dest => dest.DestinationDateTime, opt => opt.MapFrom(src => src.DateTo))
                        .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                        .ForMember(dest => dest.TimeLimit, opt => opt.MapFrom(src => src.TimeLimit));
        }
    }
}