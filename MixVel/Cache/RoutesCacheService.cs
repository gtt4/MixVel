using MixVel.Interfaces;
using Prometheus;
using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

public class RoutesCacheService : IRoutesCacheService
{
    private readonly ConcurrentDictionary<Guid, Route> _routeCache = new();
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _originIndex = new(StringComparer.OrdinalIgnoreCase);
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

        // Group valid routes by Origin
        var groupedRoutes = routes
            .Where(route => route.TimeLimit > now) // Filter valid routes
            .GroupBy(route => route.Origin);

        foreach (var group in groupedRoutes)
        {
            var origin = group.Key;
            var routeIds = new List<Guid>();

            // Update the cache and collect route IDs
            foreach (var route in group)
            {
                _routeCache[route.Id] = route; // Add to cache
                routeIds.Add(route.Id); // Collect IDs
                UpdateEarliestTimeLimit(route.TimeLimit); // Update time limit
            }

            // Lock once per origin and update the set
            var originSet = _originIndex.GetOrAdd(origin, _ => new HashSet<Guid>());
            _readerLockSlim.EnterWriteLock();
            try
            {
                foreach (var routeId in routeIds)
                {
                    originSet.Add(routeId);
                }
            }
            finally
            {
                _readerLockSlim.ExitWriteLock();
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
        List<Route> routes;

        _readerLockSlim.EnterReadLock();
        try
        { 
            routes = originSet.Select(id => _routeCache.TryGetValue(id, out Route route) ? route : null).ToList();
        }
        finally { _readerLockSlim.ExitReadLock(); }
        


        return _searchFilter.ApplyFilters(request.Filters, routes.Where(route => route != null && route.TimeLimit > now));
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

            _logger.LogInformation($"Remove{route.TimeLimit} at {DateTime.UtcNow}");
            bool needToRemove = false;
            if (_originIndex.TryGetValue(route.Origin, out var originSet))
            {
                _readerLockSlim.EnterWriteLock();
                try
                {
                    originSet.Remove(routeId);
                    if (originSet.Count == 0)
                    {
                        needToRemove = true;
                        
                    }
                }
                finally
                {
                    _readerLockSlim.ExitWriteLock();
                }

                if (needToRemove)
                    _originIndex.TryRemove(route.Origin, out _);
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

