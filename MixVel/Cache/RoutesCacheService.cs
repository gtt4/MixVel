using MixVel.Interfaces;
using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;

public class RoutesCacheService : IRoutesCacheService
{
    private readonly ConcurrentDictionary<Guid, Route> _routeCache = new();
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _originIndex = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _earliestTimeLimitLock = new();
    private readonly ILogger<RoutesCacheService> _logger;
    private readonly SearchFilter _searchFilter;

    public DateTime EarliestTimeLimit { get; set; } = DateTime.MaxValue;


    public RoutesCacheService(ILogger<RoutesCacheService> logger)
    {
        _logger = logger;
        _searchFilter = new SearchFilter();
    }

    public void Add(IEnumerable<Route> routes)
    {
        var now = DateTime.UtcNow;

        foreach (var route in routes)
        {
            if (route.TimeLimit > now)
            {
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

