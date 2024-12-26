using AutoMapper;
using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers.ProviderTwo
{
    public class ProviderTwoMappingProfile : Profile
    {
        public ProviderTwoMappingProfile()
        {
            CreateMap<SearchRequest, ProviderTwoSearchRequest>()
                .ForMember(dest => dest.Departure, opt => opt.MapFrom(src => src.Origin))
                .ForMember(dest => dest.Arrival, opt => opt.MapFrom(src => src.Destination))
                .ForMember(dest => dest.DepartureDate, opt => opt.MapFrom(src => src.OriginDateTime))
                .ForMember(dest => dest.MinTimeLimit, opt => opt.MapFrom(src => src.Filters != null ? src.Filters.MinTimeLimit : null));

            CreateMap<ProviderTwoRoute, Route>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(_ => Guid.NewGuid()))
                .ForMember(dest => dest.Origin, opt => opt.MapFrom(src => src.Departure.Point))
                .ForMember(dest => dest.Destination, opt => opt.MapFrom(src => src.Arrival.Point))
                .ForMember(dest => dest.OriginDateTime, opt => opt.MapFrom(src => src.Departure.Date))
                .ForMember(dest => dest.DestinationDateTime, opt => opt.MapFrom(src => src.Arrival.Date))
                .ForMember(dest => dest.Price, opt => opt.MapFrom(src => src.Price))
                .ForMember(dest => dest.TimeLimit, opt => opt.MapFrom(src => src.TimeLimit));
        }
    }

}
