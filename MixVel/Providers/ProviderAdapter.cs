using AutoMapper;
using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers
{
    public class ProviderAdapter<ProviderRequest, ProviderRoute> : IProvider
    {
        private readonly IProviderClient<ProviderRequest, ProviderRoute> _client;
        private readonly IMapper _mapper;
        private readonly ILogger<ProviderAdapter<ProviderRequest, ProviderRoute>> _logger;

        public ProviderAdapter(
            IProviderClient<ProviderRequest, ProviderRoute> client,
            IMapper mapper, ILogger<ProviderAdapter<ProviderRequest, ProviderRoute>> logger)
        {
            _client = client;
            _mapper = mapper;
            _logger = logger;
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return _client.IsAvailableAsync(cancellationToken);
        }

        public async Task<IEnumerable<Route>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            var providerRequest = _mapper.Map<ProviderRequest>(request);

            var routes = await _client.SearchAsync(providerRequest, cancellationToken);
            _logger.LogDebug($"Search completed using provider client '{_client.GetType().Name}'. {routes.Count()} routes found.", _client.GetType().Name);

            return _mapper.Map<IEnumerable<Route>>(routes);
        }
    }

}
