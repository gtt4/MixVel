using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Service
{
    public class SearchAggregator
    {
        public SearchResponse Aggregate(IEnumerable<Route> routes)
        {
            var routeList = routes.ToList();

            if (!routeList.Any())
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

            var minPrice = routeList.Min(r => r.Price);
            var maxPrice = routeList.Max(r => r.Price);
            var minDuration = routeList.Min(r => (r.DestinationDateTime - r.OriginDateTime).TotalMinutes);
            var maxDuration = routeList.Max(r => (r.DestinationDateTime - r.OriginDateTime).TotalMinutes);

            return new SearchResponse
            {
                Routes = routeList.ToArray(),
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                MinMinutesRoute = (int)minDuration,
                MaxMinutesRoute = (int)maxDuration
            };
        }
    }
}