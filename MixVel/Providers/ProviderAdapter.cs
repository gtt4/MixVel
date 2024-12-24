using Microsoft.AspNetCore.Routing;
using MixVel.Interfaces;
using System;
using TestTask;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers
{
    public class ProviderAdapter<ProviderRequest, ProviderResponse, ProviderRoute> : IProvider
    {
        readonly IProviderClient<ProviderRequest, ProviderResponse> _client;
        readonly IConverter<ProviderRequest, ProviderResponse, ProviderRoute> _converter;

        public ProviderAdapter(IProviderClient<ProviderRequest, ProviderResponse> client, 
            IConverter<ProviderRequest, ProviderResponse, ProviderRoute> converter)
        {
            _client = client;
            _converter = converter;
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            return _client.IsAvailableAsync(cancellationToken);
        }

        public async Task<IEnumerable<Route>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            var providerRequest = _converter.ConvertRequest(request);

            var response = await _client.SearchAsync(providerRequest, cancellationToken);

            var result = new List<Route>();
            //foreach (var route in response.Routes)
            //{
            //    result.Add(_converter.ConvertRoute(route));
            //}

            return _converter.ConvertRoutes(response);
            //return result;
        }
    }
}
