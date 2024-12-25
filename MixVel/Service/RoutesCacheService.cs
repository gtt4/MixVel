using MixVel.Interfaces;
using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Service
{
    public class RoutesCacheService : IRoutesCacheService
    {
        private readonly ConcurrentDictionary<Guid, Route> _routeCache = new ConcurrentDictionary<Guid, Route>();

        public void Add(IEnumerable<Route> routes)
        {
            var now = DateTime.UtcNow; // TODO time kinds 

            foreach (var route in routes)
            {
                if (route.TimeLimit > now)
                {
                    _routeCache[route.Id] = route;
                }
            }
        }

        public IEnumerable<Route> Get(SearchRequest request)
        {
            Invalidate();
            var routes = _routeCache.Values.AsEnumerable();

            routes = routes.Where(route =>
                string.Equals(route.Origin, request.Origin, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(route.Destination, request.Destination, StringComparison.OrdinalIgnoreCase) &&
                route.OriginDateTime.Date == request.OriginDateTime.Date);

            var filters = request.Filters;
            if (filters != null)
            {
                if (filters.DestinationDateTime.HasValue)
                {
                    routes = routes.Where(route =>
                        route.DestinationDateTime.Date == filters.DestinationDateTime.Value.Date);
                }

                if (filters.MaxPrice.HasValue)
                {
                    routes = routes.Where(route =>
                        route.Price <= filters.MaxPrice.Value);
                }

                if (filters.MinTimeLimit.HasValue)
                {
                    routes = routes.Where(route =>
                        route.TimeLimit >= filters.MinTimeLimit.Value);
                }
            }

            return routes.ToList();
        }

        private void Invalidate()
        {
            var now = DateTime.UtcNow;

            foreach (var routeId in _routeCache.Keys)
            {
                if (_routeCache.TryGetValue(routeId, out var route))
                {
                    if (route.TimeLimit <= now)
                    {
                        _routeCache.TryRemove(routeId, out _);
                    }
                }
            }
        }
    }
}
