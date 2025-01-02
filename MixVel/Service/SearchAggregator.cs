using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Service
{
    public class SearchAggregator
    {
        public SearchResponse Aggregate(IEnumerable<Route> routes)
        {
            if (!routes.Any())
            {
                return new SearchResponse
                {
                    Routes = Array.Empty<Route>(),
                    MinPrice = 0,
                    MaxPrice = 0,
                    MinMinutesRoute = 0,
                    MaxMinutesRoute = 0
                };
            }

            decimal minPrice = decimal.MaxValue, maxPrice = decimal.MinValue;
            double minDuration = double.MaxValue, maxDuration = double.MinValue;

            foreach (var route in routes)
            {
                var price = route.Price;
                var duration = (route.DestinationDateTime - route.OriginDateTime).TotalMinutes;

                if (price < minPrice) minPrice = price;
                if (price > maxPrice) maxPrice = price;
                if (duration < minDuration) minDuration = duration;
                if (duration > maxDuration) maxDuration = duration;
            }

            return new SearchResponse
            {
                Routes = routes.ToArray(),
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinMinutesRoute = (int)minDuration,
                MaxMinutesRoute = (int)maxDuration
            };
        }
    }
}