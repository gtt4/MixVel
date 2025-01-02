// See https://aka.ms/new-console-template for more information

using MixVel.Interfaces;
using MixVel.Providers.ProviderTwo;
using System.Net.Http.Json;
using System.Threading;

internal class Client
{
    private readonly object ProviderBaseUri;
    private HttpClient _httpClient;

    public Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task SendGetRequest(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            Console.WriteLine($"Request to {endpoint} succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }
    }

    public async Task SendPostRequest(SearchRequest request)
    {
        try
        {
            var response  = await _httpClient.PostAsJsonAsync($"/api/v1/Search/search", request);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<SearchResponse>();
            //Console.WriteLine($"Request succeeded with {result.Routes.Length} routes.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request failed: {ex.Message}");
        }
    }
}