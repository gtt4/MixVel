using MixVel.Interfaces;
using Polly;
using TestTask;

namespace MixVel.Providers
{
    public class ProviderOneClient : IProviderClient<ProviderOneSearchRequest, ProviderOneSearchResponse>
    {
        private readonly HttpClient _httpClient;
        private readonly AsyncPolicy<HttpResponseMessage> _policy;

        private const string ProviderOnePingUrl = "http://provider-one/api/v1/ping";
        private const string ProviderOneSearchUrl = "http://provider-one/api/v1/search";
 
        public ProviderOneClient(HttpClient httpClient)
        {
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
                    ct => _httpClient.GetAsync(ProviderOnePingUrl, ct),
                    cancellationToken
                );
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<ProviderOneSearchResponse> SearchAsync(ProviderOneSearchRequest request, CancellationToken cancellationToken)
        {
            var response = await _policy.ExecuteAsync(
                ct => _httpClient.PostAsJsonAsync(ProviderOneSearchUrl, request, ct),
                cancellationToken
            );

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProviderOneSearchResponse>(cancellationToken: cancellationToken);
                return result;
            }
            else
            {
                throw new HttpRequestException($"Provider returned status code {response.StatusCode}");
            }
        }
    }


}
