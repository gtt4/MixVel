﻿using MixVel.Interfaces;
using TestTask;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers
{
    public class ProviderOneConverter : IConverter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>
    {
        public ProviderOneSearchRequest ConvertRequest(SearchRequest request) =>
                        new()
                        {
                            From = request.Origin,
                            To = request.Destination,
                            DateFrom = request.OriginDateTime,
                            DateTo = request.Filters?.DestinationDateTime
                        };


        public Route ConvertRoute(ProviderOneRoute route) => new()
        {
            Destination = route.To,
            Origin = route.From,
            DestinationDateTime = route.DateTo,
            OriginDateTime = route.DateFrom,
            Price = route.Price,
            TimeLimit = route.TimeLimit
        };

        public IEnumerable<Route> ConvertRoutes(ProviderOneSearchResponse? response)
        {
            foreach (var route in response.Routes) {
                yield return ConvertRoute(route);
            }
        }
    }
}
