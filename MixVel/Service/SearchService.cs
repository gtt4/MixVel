using MixVel.Interfaces;
using MixVel.Providers;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Service
{
    public class SearchService: ISearchService
    {
        List<IProvider> _providers;
        private readonly IRoutesCacheService _cacheService;
        private readonly ILogger<SearchService> _logger;
        private readonly SearchAggregator _aggregator;
        private readonly SearchFilter _searchFilter;

        public SearchService(List<IProvider> providers, IRoutesCacheService cacheService, ILogger<SearchService> logger) 
        {
            _providers = providers ?? throw new ArgumentNullException(nameof(providers));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _aggregator = new SearchAggregator();
            _searchFilter = new SearchFilter();
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
                    return _searchFilter.ApplyFilters(request.Filters, routes); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error fetching routes from provider {provider.GetType().Name}");
                    return Enumerable.Empty<Route>(); // TODO Clarify requirments
                }
            });

            var routesResults = await Task.WhenAll(providerTasks);
            var allRoutes = routesResults.SelectMany(routes => routes);

            return _aggregator.Aggregate(allRoutes);
        }
    }
}

