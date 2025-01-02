using Microsoft.Extensions.Options;
using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Cache;

public class RoutesCacheService : IRoutesCacheService, IScheduledJob, IDisposable
{
    
    private readonly ILogger<RoutesCacheService> _logger;
    private readonly SearchFilter _searchFilter;
    private readonly IMetricsService _metricsService;
    private readonly RouteCache _routeCache;
    private readonly ExpirationTracker _expirationTracker;
    private readonly CacheSettings _settings;
    private readonly InvalidationScheduler _invalidationScheduler;
    private int _isInvalidating;
    private bool _disposed = false;


    public RoutesCacheService(ILoggerFactory loggerFactory, IMetricsService metricsService, IOptions<CacheSettings> settings)
    {
        _logger = loggerFactory.CreateLogger<RoutesCacheService>();
        _metricsService = metricsService;

        _searchFilter = new SearchFilter();
        _routeCache = new RouteCache(loggerFactory);
        _expirationTracker = new ExpirationTracker(loggerFactory, TimeSpan.TicksPerMinute);
        _settings = settings.Value;
    }

    public void Add(IEnumerable<Route> routes)
    {
        var addedRoutes = _routeCache.AddRoutes(routes); 
        _expirationTracker.AddRoutes(addedRoutes);
    }

    public IEnumerable<Route> Get(SearchRequest request)
    {
        var routes = _routeCache.TryGetRoutes(request);
        return _searchFilter.ApplyFilters(request.Filters, routes);
    }

    public void Execute(CancellationToken cancellationToken)
    {
        Invalidate(cancellationToken);
    }

    public void Invalidate(CancellationToken cancellationToken, bool force = false)
    {
        if (Interlocked.Exchange(ref _isInvalidating, 1) == 1)
        {
            // Another thread is already running Invalidate; skip execution
            return;
        }

        try
        {
            var now = DateTime.UtcNow;
            _logger.LogInformation($"routes in cache = {_routeCache.Count}");
            _logger.LogInformation($"routes in cache = {_expirationTracker.GetBucketCount()}");

            if (!force && (now.Ticks < GetEarliestTimeLimitTicks() || _routeCache.Count < _settings.MinRoutesToInvalidate)) return;
            var expiredRouteIds = _expirationTracker.GetExpiredRoutes(now);

            int countToRemoved = 0;
            foreach (var routeId in expiredRouteIds)
            {
                if (_routeCache.TryRemoveRoute(routeId))
                    countToRemoved++;
            }

            _logger.LogInformation($"{countToRemoved} routes were removed from cache");
        }
        finally
        {
            Interlocked.Exchange(ref _isInvalidating, 0);
        }
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
            _routeCache.Dispose();
        }

        _disposed = true;
    }

    public long GetEarliestTimeLimitTicks() => _expirationTracker.GetEarliestTimeLimitTicks();
}

