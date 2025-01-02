using MixVel.Providers.ProviderOne;
using MixVel.Providers.ProviderTwo;
using MixVel.Settings;
using RichardSzalay.MockHttp;
using System.Text;
using System.Text.Json;

public class MockClient
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ResponseGenerator _responseGenerator;

    public MockClient(ResponseGenerator? responseGenerator = null)
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        _responseGenerator = responseGenerator ?? new ResponseGenerator();
    }

    public HttpClient CreateMockClient(IProviderUriResolver uriResolver)
    {
        var mockHttp = new MockHttpMessageHandler();

        SetupMockEndpoint<ProviderOneSearchRequest, ProviderOneSearchResponse>(
            mockHttp,
            HttpMethod.Post,
            $"{uriResolver.GetProviderUri("ProviderOne")}/search",
            _responseGenerator.GenerateResponse);

        SetupMockEndpoint<ProviderTwoSearchRequest, ProviderTwoSearchResponse>(
            mockHttp,
            HttpMethod.Post,
            $"{uriResolver.GetProviderUri("ProviderTwo")}/search",
            _responseGenerator.GenerateResponse);

        return new HttpClient(mockHttp);
    }

    private void SetupMockEndpoint<TRequest, TResponse>(
        MockHttpMessageHandler mockHttp,
        HttpMethod method,
        string uri,
        Func<TRequest, TResponse> responseGenerator)
    {
        var rand = new Random();
        mockHttp.When(method, uri)
                .Respond(async request =>
                {
                    var content = await request.Content.ReadAsStringAsync();
                    var requestObject = JsonSerializer.Deserialize<TRequest>(content, _serializerOptions);
                    var responseObject = responseGenerator(requestObject);

                    var jsonResponse = JsonSerializer.Serialize(responseObject, _serializerOptions);

                    //await Task.Delay(rand.Next(1,3000));
                    return new HttpResponseMessage
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                    };
                });
    }
}

public class ResponseGenerator
{
    private readonly Random _rand;

    public ResponseGenerator()
    {
        _rand = Random.Shared;
    }

    public ProviderOneSearchResponse GenerateResponse(ProviderOneSearchRequest request)
    {
        return new ProviderOneSearchResponse
        {
            Routes = GenerateRoutes<ProviderOneRoute>(request.From, request.To, request.DateFrom, 5)
        };
    }

    public ProviderTwoSearchResponse GenerateResponse(ProviderTwoSearchRequest request)
    {
        return new ProviderTwoSearchResponse
        {
            Routes = GenerateRoutes(
                request.Departure,
                request.Arrival,
                request.DepartureDate,
                3,
                routeGenerator: (from, to, dateFrom, i) => new ProviderTwoRoute
                {
                    Departure = new ProviderTwoPoint { Point = from, Date = dateFrom.AddHours(i) },
                    Arrival = new ProviderTwoPoint { Point = to, Date = dateFrom.AddHours(i).AddMinutes(_rand.Next(60, 600)) },
                    Price = 100 + (i * 10),
                    TimeLimit = DateTime.UtcNow.AddSeconds(_rand.Next(1, 200))
                }
            )
        };
    }

    private TRoute[] GenerateRoutes<TRoute>(
        string from,
        string to,
        DateTime dateFrom,
        int count,
        Func<string, string, DateTime, int, TRoute>? routeGenerator = null)
    {
        routeGenerator ??= (fromPoint, toPoint, date, i) => (TRoute)(object)new ProviderOneRoute
        {
            From = fromPoint,
            To = toPoint,
            DateFrom = date.AddHours(i),
            DateTo = date.AddHours(i).AddMinutes(_rand.Next(60, 600)),
            Price = 100 + (i * 10),
            TimeLimit = DateTime.UtcNow.AddSeconds(_rand.Next(1, 200))
        };

        return Enumerable.Range(1, count)
                         .Select(i => routeGenerator(from, to, dateFrom, i))
                         .ToArray();
    }
}
