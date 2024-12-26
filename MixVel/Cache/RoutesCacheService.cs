using MixVel.Interfaces;
using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

public class RoutesCacheService : IRoutesCacheService
{
    private readonly ConcurrentDictionary<Guid, Route> _routeCache = new();
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _originIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _earliestTimeLimitLock = new();
    private readonly ILogger<RoutesCacheService> _logger;

    public DateTime EarliestTimeLimit { get; set; } = DateTime.MaxValue;


    public RoutesCacheService(ILogger<RoutesCacheService> logger)
    {
            _logger = logger;
    }

    public void Add(IEnumerable<Route> routes)
    {
        var now = DateTime.UtcNow;

        foreach (var route in routes)
        {
            if (route.TimeLimit > now)
            {
                //route.Id = Guid.NewGuid();
                _routeCache[route.Id] = route;

                UpdateEarliestTimeLimit(route.TimeLimit);

                var originSet = _originIndex.GetOrAdd(route.Origin, _ => new HashSet<Guid>());
                lock (originSet)
                {
                    originSet.Add(route.Id);
                }
            }
        }
    }

    public IEnumerable<Route> Get(SearchRequest request)
    {
        var now = DateTime.UtcNow;

        if (!_originIndex.TryGetValue(request.Origin, out var originSet))
        {
            return Enumerable.Empty<Route>();
        }

        IEnumerable<Guid> routeIds;
        lock (originSet)
        {
            routeIds = originSet.ToList(); // TODO try to avoid 
        }

        var routes = routeIds
            .Select(id => _routeCache.TryGetValue(id, out var route) ? route : null)
            .Where(route => route != null && route.TimeLimit > now);

        if (request.Filters != null)
            routes = ApplyFilters(request.Filters, routes);

        return routes.ToList();
    }

    private static IEnumerable<Route?> ApplyFilters(SearchFilters filters, IEnumerable<Route?> routes)
    {

        //if (filters.DestinationDateTime.HasValue)
        //{
        //    routes = routes.Where(route => route.DestinationDateTime.Date == filters.DestinationDateTime.Value.Date);
        //}

        if (filters.MaxPrice.HasValue)
        {
            routes = routes.Where(route => route.Price <= filters.MaxPrice.Value);
        }

        //if (filters.MinTimeLimit.HasValue)
        //{
        //    routes = routes.Where(route => route.TimeLimit >= filters.MinTimeLimit.Value);
        //}


        return routes;
    }

    public void Invalidate()
    {
        var now = DateTime.UtcNow;
        var expiredRouteIds = new List<Guid>();

        if (now < EarliestTimeLimit) {
            _logger.LogInformation("Nothing to remove less them earliest");
            return; };

        foreach (var kvp in _routeCache)
        {
            var route = kvp.Value;
            if (route.TimeLimit <= now)
            {
                expiredRouteIds.Add(route.Id);
            }
        }

        foreach (var routeId in expiredRouteIds)
        {
            if (!_routeCache.TryRemove(routeId, out var route))
            {
                continue;
            }

            _logger.LogInformation($"Remove{route.TimeLimit} at {DateTime.UtcNow}");
            if (_originIndex.TryGetValue(route.Origin, out var originSet))
            {
                lock (originSet)
                {
                    originSet.Remove(routeId);
                    if (originSet.Count == 0)
                    {
                        _originIndex.TryRemove(route.Origin, out _);
                    }
                }
            }
        }

        UpdateEarliestTimeLimit();
    }

    private void UpdateEarliestTimeLimit(DateTime newTimeLimit)
    {
        lock (_earliestTimeLimitLock)
        {
            if (newTimeLimit < EarliestTimeLimit)
            {
                EarliestTimeLimit = newTimeLimit;
            }
        }
    }

    private void UpdateEarliestTimeLimit()
    {
        lock (_earliestTimeLimitLock)
        {
            if (_routeCache.IsEmpty)
            {
                EarliestTimeLimit = DateTime.MaxValue;
            }
            else
            {
                EarliestTimeLimit = _routeCache.Values.Min(route => route.TimeLimit);
            }
        }
    }
}

