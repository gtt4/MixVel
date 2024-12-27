using MixVel.Interfaces;
using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

public class RoutesCacheService : IRoutesCacheService, IPeriodicTask, IDisposable
{
    private readonly ConcurrentDictionary<Guid, Route> _routeCache = new();
    private readonly ConcurrentDictionary<string, Guid> _routeKeys = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _originIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<RoutesCacheService> _logger;
    private readonly SearchFilter _searchFilter;
    private readonly IMetricsService _metricsService;
    private bool _disposed = false;
    private long _earliestTimeLimitTicks;

    public RoutesCacheService(ILogger<RoutesCacheService> logger, IMetricsService metricsService)
    {
        _logger = logger;
        _searchFilter = new SearchFilter();
        _metricsService = metricsService;
    }

    public void Add(IEnumerable<Route> routes)
    {
        var now = DateTime.UtcNow; // TODO Clarify requirments. Assume timelimit is in UTC

        var groupedRoutes = routes
            .Where(route => route.TimeLimit > now)
            .GroupBy(route => route.Origin);

        foreach (var group in groupedRoutes)
        {
            var origin = group.Key;
            var originSet = _originIndex.GetOrAdd(origin, _ => new ConcurrentDictionary<Guid, byte>());

            foreach (var route in group)
            {
                var routeKey = GetRouteKey(route);

                if (!_routeKeys.TryAdd(routeKey, route.Id))
                {
                    // Route already exists, skip adding
                    _logger.LogInformation($"Duplicate route {routeKey} was not added");
                    continue;
                }
                route.Id = Guid.NewGuid();  
                _routeCache[route.Id] = route;
                originSet[route.Id] = 0;
                UpdateEarliestTimeLimit(route.TimeLimit);
            }
        }
    }

    private string GetRouteKey(Route route)
    {
        return $"{route.Origin}_{route.Destination}_{route.TimeLimit}"; // TODO 
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

    public void Execute(CancellationToken cancellationToken)
    {
        Invalidate();
    }

    public void Invalidate() 
    {
        var now = DateTime.UtcNow;

        if (now.Ticks < Interlocked.Read(ref _earliestTimeLimitTicks)) return;
            
        var expiredRouteIds = new List<Guid>();
        
        foreach (var kvp in _routeCache) 
        {
            var route = kvp.Value;
            if (route.TimeLimit <= now)
            {
                expiredRouteIds.Add(route.Id);
            }
        }
        int countToRemoved = 0;
        foreach (var routeId in expiredRouteIds)
        {
            if (!_routeCache.TryRemove(routeId, out var route))
            {
                continue;
            }
            countToRemoved++;
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
        }

        _logger.LogInformation($"{countToRemoved} routes was removed from cache"); 

        UpdateEarliestTimeLimit();
    }

    private void UpdateEarliestTimeLimit(DateTime newTimeLimit)
    {
        var newTicks = newTimeLimit.Ticks;
        long oldTicks;
        do
        {
            oldTicks = Interlocked.Read(ref _earliestTimeLimitTicks);
            if (newTicks >= oldTicks) break;
        } while (Interlocked.CompareExchange(ref _earliestTimeLimitTicks, newTicks, oldTicks) != oldTicks);
    }

    private void UpdateEarliestTimeLimit()
    {
        if (_routeCache.IsEmpty)
        {
            Interlocked.Exchange(ref _earliestTimeLimitTicks, DateTime.MaxValue.Ticks);
            return;
        }

        var minTimeLimitTicks = _routeCache.Values.Min(route => route.TimeLimit.Ticks);
        UpdateEarliestTimeLimit(new DateTime(minTimeLimitTicks));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _routeCache.Clear();
            _routeKeys.Clear();
            _originIndex.Clear();
        }

        _disposed = true;
    }

    public long GetEarliestTimeLimitTicks() => Interlocked.Read(ref _earliestTimeLimitTicks);
}

