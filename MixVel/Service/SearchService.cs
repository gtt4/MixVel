using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Service
{
    public class SearchService: ISearchService
    {
        List<IProvider> _providers;
        private readonly IRoutesCacheService _cacheService;
        private readonly ILogger<SearchService> _logger;
        private readonly SearchAggregator _aggregator;

        public SearchService(List<IProvider> providers, IRoutesCacheService cacheService, ILogger<SearchService> logger) 
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aggregator = new SearchAggregator();
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            var availabilityTasks = _providers.Select(provider => provider.IsAvailableAsync(cancellationToken));
            var results = await Task.WhenAll(availabilityTasks);
            return results.Any(isAvailable => isAvailable);
        }

        public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (request.Filters?.OnlyCached == true)
            {
                var routes = _cacheService.Get(request);
                return _aggregator.Aggregate(routes);
            }

            var providerTasks = _providers.Select(async provider =>
            {
                try
                {
                    var routes = await provider.SearchAsync(request, cancellationToken);
                    _cacheService.Add(routes);
                    return routes;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error fetching routes from provider {Provider}", provider.GetType().Name);
                    return Enumerable.Empty<Route>();
                }
            });

            var routesResults = await Task.WhenAll(providerTasks);
            var allRoutes = routesResults.SelectMany(routes => routes);

            return _aggregator.Aggregate(allRoutes);
        }
    }

    //private SearchResponse MergeRouteAggregates(RoutesAggregate[] partialAggregates)
    //    {
    //        var searchResponse = new SearchResponse();
    //        var notEmptyPartialAggregates = partialAggregates.Where(x => x.HaveResult);

    //        if (!notEmptyPartialAggregates.Any()) return new SearchResponse();
    //        searchResponse.Routes = notEmptyPartialAggregates.Select(x => x.Routes).SelectMany(x => x).ToArray();
    //        searchResponse.MinMinutesRoute = notEmptyPartialAggregates.Min(x => x.MinTime);
    //        searchResponse.MinPrice = notEmptyPartialAggregates.Min(x => x.MinPrice);
    //        return searchResponse;
    //    }

    //    private RoutesAggregate Aggregate(IEnumerable<Route> routes)
    //    {
    //        if (!routes.Any()) return new RoutesAggregate();

    //        var first = routes.FirstOrDefault();
    //        var minPrice = first.Price;
    //        var minTime = first.DestinationDateTime - first.OriginDateTime;

    //        foreach (var item in routes)
    //        {
    //            if (minPrice > item.Price)
    //                minPrice = item.Price;

    //            if (minTime > item.DestinationDateTime - item.OriginDateTime)
    //                minTime = item.DestinationDateTime - item.OriginDateTime;
    //        }

    //        return new RoutesAggregate()
    //        {
    //            Routes = routes,
    //            MinPrice = minPrice,
    //            MinTime = (int)minTime.TotalMinutes
    //        };
    //    }


    //    private async Task<IEnumerable<Route>> GetRoutesFromProviderAsync(IProvider client, SearchRequest request, CancellationToken cancellationToken)
    //    {
    //        try
    //        {
    //            return await client.SearchAsync(request, cancellationToken);
    //        }
    //        catch (Exception ex)
    //        {
    //            return Enumerable.Empty<Route>();
    //        }
    //    }
    }

