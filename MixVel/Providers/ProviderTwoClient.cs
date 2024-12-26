using MixVel.Interfaces;
using Polly;
using TestTask;

public class ProviderTwoClient : IProviderClient<ProviderTwoSearchRequest, ProviderTwoSearchResponse>
{
    private readonly HttpClient _httpClient;
    private readonly AsyncPolicy<HttpResponseMessage> _policy;


    private const string ProviderTwoSearchUrl = "http://provider-two/api/v1/search";
    private const string ProviderTwoPingUrl = "http://provider-two/api/v1/ping";

    public ProviderTwoClient(HttpClient httpClient)
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
                ct => _httpClient.GetAsync(ProviderTwoPingUrl, ct),
                cancellationToken
            );
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<ProviderTwoSearchResponse> SearchAsync(ProviderTwoSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await _policy.ExecuteAsync(
            ct => _httpClient.PostAsJsonAsync(ProviderTwoSearchUrl, request, ct),
            cancellationToken
        );

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<ProviderTwoSearchResponse>(cancellationToken: cancellationToken);
            return result;
        }
        else
        {
            throw new HttpRequestException($"Provider returned status code {response.StatusCode}");
        }
    }
}