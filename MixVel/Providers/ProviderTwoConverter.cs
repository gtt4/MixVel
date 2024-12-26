﻿using MixVel.Interfaces;
using System;
using TestTask;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers
{
    public class ProviderTwoConverter : IConverter<ProviderTwoSearchRequest, ProviderTwoSearchResponse, ProviderTwoRoute>
    {
        public ProviderTwoSearchRequest ConvertRequest(SearchRequest request) =>
            new()
            {
                Departure = request.Origin,
                Arrival = request.Destination,
                DepartureDate = request.OriginDateTime,
                MinTimeLimit = request.Filters?.MinTimeLimit
            };

        public Route ConvertRoute(ProviderTwoRoute route) => new ()
        {
            Origin = route.Departure.Point,
            Destination = route.Arrival.Point,
            OriginDateTime = route.Departure.Date,
            DestinationDateTime = route.Arrival.Date,
            Price = route.Price,
            TimeLimit = route.TimeLimit
        };

        public IEnumerable<Route> ConvertRoutes(ProviderTwoSearchResponse? response)
        {
            foreach (var route in response.Routes)
            {
                yield return ConvertRoute(route);
            }
        }
    }
}
