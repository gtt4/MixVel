using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Cache;

public class RouteCache : IDisposable
{
    private readonly ConcurrentDictionary<Guid, Route> _routeCache;
    private readonly ConcurrentDictionary<int, Guid> _routeKeys;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _originIndex;

    private readonly ILogger<RouteCache> _logger;

    public RouteCache(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<RouteCache>();
        var expectedCapacity = 2000;
        _routeCache = new ConcurrentDictionary<Guid, Route>(-1, expectedCapacity);
        _routeKeys = new ConcurrentDictionary<int, Guid>(-1, expectedCapacity);
        _originIndex = new(StringComparer.OrdinalIgnoreCase);
    }

    public List<Route> AddRoutes(IEnumerable<Route> routes)
    {
        var groupedRoutes = routes.GroupBy(route => route.Origin);
        var added = new List<Route>(routes.Count());

        foreach (var group in groupedRoutes)
        {
            var origin = group.Key;
            var originIndex = _originIndex.GetOrAdd(origin, _ => new ConcurrentDictionary<Guid, byte>());

            foreach (var route in group)
            {
                if (TryAddRoute(originIndex, route))
                    added.Add(route);
            }
        }

        return added;

    }


    public List<Route> TryGetRoutes(Interfaces.SearchRequest request)
    {
        var now = DateTime.UtcNow;
        if (!_originIndex.TryGetValue(request.Origin, out var originSet))
        {
            //_metricsService.IncrementCounter("cache_misses", new[] { request.Origin });
            return Enumerable.Empty<Route>().ToList();
        }
        //_metricsService.IncrementCounter("cache_hits", new[] { request.Origin });


        var routes = originSet.Keys
              .Select(id => _routeCache.TryGetValue(id, out var route) ? route : null)
              .Where(route => route != null && route.TimeLimit > now)
              .ToList();

        return routes;
    }

    public bool TryRemoveRoute(Guid routeId)
    {
        if (!_routeCache.TryRemove(routeId, out var route))
        {
            return false;
        }

        var routeKey = GetRouteKey(route);
        _routeKeys.TryRemove(routeKey, out _);

        if (_originIndex.TryGetValue(route.Origin, out var originSet))
        {
            originSet.TryRemove(routeId, out _);
            if (originSet.IsEmpty)
            {
                _originIndex.TryRemove(route.Origin, out _);
            }
        }

        return true;
    }

    public int Count => _routeCache.Count;

    private bool TryAddRoute(ConcurrentDictionary<Guid, byte> originSet, Route route)
    {
        var routeKey = GetRouteKey(route);
        route.Id = Guid.NewGuid(); // watch out

        if (_routeKeys.TryAdd(routeKey, route.Id))
        {
            _routeCache[route.Id] = route;
            originSet[route.Id] = 0;
            return true;
        }

        if (_routeKeys.TryGetValue(routeKey, out var existingRouteId) &&
            _routeCache.TryGetValue(existingRouteId, out var existedRoute))
        {
            _logger.LogWarning($"Route {existingRouteId} has the same key.");
            // TODO logic to handle duplicates
        }

        return false;
    }

    private int GetRouteKey(Route route)
    {
        return HashCode.Combine(route.Origin.ToLowerInvariant(),
                route.Destination.ToLowerInvariant(),
                route.OriginDateTime,
                route.DestinationDateTime,
                route.Price,
                route.TimeLimit
            );
    }

    public void Dispose()
    {
        _originIndex.Clear();
        _routeCache.Clear();
        _routeKeys.Clear();
    }
}

