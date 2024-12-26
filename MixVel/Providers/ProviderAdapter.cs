using AutoMapper;
using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers
{
    public class ProviderAdapter<ProviderRequest, ProviderRoute> : IProvider
    {
        private readonly IProviderClient<ProviderRequest, ProviderRoute> _client;
        private readonly IMapper _mapper;

        public ProviderAdapter(
            IProviderClient<ProviderRequest, ProviderRoute> client,
            IMapper mapper)
        {
            _client = client;
            _mapper = mapper;
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return _client.IsAvailableAsync(cancellationToken);
        }

        public async Task<IEnumerable<Route>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            var providerRequest = _mapper.Map<ProviderRequest>(request);

            var routes = await _client.SearchAsync(providerRequest, cancellationToken);

            return _mapper.Map<IEnumerable<Route>>(routes);
        }
    }

}
