using MixVel.Interfaces;
using MixVel.Providers.ProviderTwo;
using MixVel.Settings;
using Polly;

public class ProviderTwoClient : IProviderClient<ProviderTwoSearchRequest, ProviderTwoRoute>
{
    private readonly HttpClient _httpClient;
    private readonly AsyncPolicy<HttpResponseMessage> _policy;


    private readonly string ProviderBaseUri;


    public ProviderTwoClient(HttpClient httpClient, IProviderUriResolver providerUriResolver)
    {
        ProviderBaseUri = providerUriResolver.GetProviderUri("ProviderTwo");

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

    public async Task<ProviderTwoRoute[]> SearchAsync(ProviderTwoSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await _policy.ExecuteAsync(
            ct => _httpClient.PostAsJsonAsync($"{ProviderBaseUri}/search", request, ct),
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ProviderTwoSearchResponse>(cancellationToken: cancellationToken);
            return result?.Routes;
        }
        else
        {
            throw new HttpRequestException($"Provider returned status code {response.StatusCode}");
        }
    }
}