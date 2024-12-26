using MixVel.Interfaces;
using MixVel.Settings;
using Polly;

namespace MixVel.Providers.ProviderOne
{
    public class ProviderOneClient : IProviderClient<ProviderOneSearchRequest, ProviderOneRoute>
    {
        private readonly HttpClient _httpClient;
        private readonly AsyncPolicy<HttpResponseMessage> _policy;

        private readonly string ProviderBaseUri;

        public ProviderOneClient(HttpClient httpClient, IProviderUriResolver providerUriResolver)
        {
            ProviderBaseUri = providerUriResolver.GetProviderUri("ProviderOne");

            _httpClient = httpClient;
            _policy = Policy.WrapAsync(
                Policy<HttpResponseMessage>
                    .Handle<HttpRequestException>()
                    .OrResult(msg => !msg.IsSuccessStatusCode)
                    .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1)),
                Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(5))
            );
        }

        public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _policy.ExecuteAsync(
                    ct => _httpClient.GetAsync($"{ProviderBaseUri}/ping", ct),
                    cancellationToken
                );
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ProviderOneRoute[]> SearchAsync(ProviderOneSearchRequest request, CancellationToken cancellationToken)
        {
            var response = await _policy.ExecuteAsync(
                ct => _httpClient.PostAsJsonAsync($"{ProviderBaseUri}/search", request, ct),
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProviderOneSearchResponse>(cancellationToken: cancellationToken);
                return result.Routes;
            }
            else
            {
                throw new HttpRequestException($"Provider returned status code {response.StatusCode}");
            }
        }
    }


}
