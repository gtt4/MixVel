using MixVel.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Route = MixVel.Interfaces.Route;

public class RoutesCacheService : IRoutesCacheService
{
    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Task _backgroundTask;
    private readonly ConcurrentDictionary<Guid, Route> _routeCache;
    private int _isInvalidating = 0; // 0 = false, 1 = true
    private DateTime _earliestTimeLimit;

    public RoutesCacheService()
    {
        _backgroundTask = Task.Run(InvalidatePeriodicallyAsync);
    }

    private async Task InvalidatePeriodicallyAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), _cts.Token);
                InvalidateIfNecessary();
            }
        }
        catch (OperationCanceledException)
        {
            // 
        }
    }

    private void InvalidateIfNecessary()
    {
        // Check if already invalidating
        if (Interlocked.Exchange(ref _isInvalidating, 1) == 1)
            return;

        try
        {
            var now = DateTime.UtcNow;

            if (now >= _earliestTimeLimit)
            {
                Invalidate();
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isInvalidating, 0);
        }
    }

    private void Invalidate()
    {
        var now = DateTime.UtcNow;

        foreach (var routeId in _routeCache.Keys)
        {
            if (_routeCache.TryGetValue(routeId, out var route) && route.TimeLimit <= now)
            {
                _routeCache.TryRemove(routeId, out _);
            }
        }

        if (!_routeCache.IsEmpty)
        {
            _earliestTimeLimit = _routeCache.Values.Min(route => route.TimeLimit);
        }
        else
        {
            _earliestTimeLimit = DateTime.MaxValue;
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _backgroundTask.Wait();
        _cts.Dispose();
    }

    public void Add(IEnumerable<Route> routes)
    {
        var now = DateTime.UtcNow;

        foreach (var route in routes)
        {
            if (route.TimeLimit > now)
            {
                _routeCache[route.Id] = route;

                if (route.TimeLimit < _earliestTimeLimit)
                {
                    _earliestTimeLimit = route.TimeLimit;
                }
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
}
