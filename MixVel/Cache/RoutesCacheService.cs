using MixVel.Interfaces;
using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

public class RoutesCacheService : IRoutesCacheService
{
    private readonly ConcurrentDictionary<Guid, Route> _routeCache = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _originIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _earliestTimeLimitLock = new();
    private readonly ILogger<RoutesCacheService> _logger;
    private readonly SearchFilter _searchFilter;
    private readonly IMetricsService _metricsService;
    private readonly ReaderWriterLockSlim _readerLockSlim = new ReaderWriterLockSlim();

    public DateTime EarliestTimeLimit { get; set; } = DateTime.MaxValue;


    public RoutesCacheService(ILogger<RoutesCacheService> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _searchFilter = new SearchFilter();
        _metricsService = metricsService;
    }

    public void Add(IEnumerable<Route> routes)
    {
        var now = DateTime.UtcNow;

        var groupedRoutes = routes
            .Where(route => route.TimeLimit > now) 
            .GroupBy(route => route.Origin);

        foreach (var group in groupedRoutes)
        {
            var origin = group.Key;
            var originSet = _originIndex.GetOrAdd(origin, _ => new ConcurrentDictionary<Guid, byte>());

            foreach (var route in group)
            {
                _routeCache[route.Id] = route;
                originSet[route.Id] = 0;
                UpdateEarliestTimeLimit(route.TimeLimit); 
            }
        }
    }



    public IEnumerable<Route> Get(SearchRequest request)
    {
        var now = DateTime.UtcNow;

        if (!_originIndex.TryGetValue(request.Origin, out var originSet))
        {
            _metricsService.IncrementCounter("cache_misses", new[] { request.Origin });
            return Enumerable.Empty<Route>();
        }
        _metricsService.IncrementCounter("cache_hits", new[] { request.Origin });
 

        var routes = originSet.Keys
              .Select(id => _routeCache.TryGetValue(id, out var route) ? route : null)
              .Where(route => route != null && route.TimeLimit > now)
              .ToList();

        return _searchFilter.ApplyFilters(request.Filters, routes);
    }



    public void Invalidate()
    {
        var now = DateTime.UtcNow;
        var expiredRouteIds = new List<Guid>();

        if (now < EarliestTimeLimit)
        {
            return;
        };

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

            if (_originIndex.TryGetValue(route.Origin, out var originSet))
            {
                originSet.TryRemove(routeId, out _);
                if (originSet.IsEmpty)
                {
                    _originIndex.TryRemove(route.Origin, out _);
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

