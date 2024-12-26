
using MixVel.Providers.ProviderOne;
using MixVel.Providers.ProviderTwo;
using MixVel.Settings;
using RichardSzalay.MockHttp;
using System.Text;
using System.Text.Json;

namespace MixVel
{
    public class MockClient
    {
        public HttpClient CreateMockClient(IProviderUriResolver uriResolver)
        {
            var mockHttp = new MockHttpMessageHandler();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var responseGenerator = new ResponseGenerator();

            mockHttp.When(HttpMethod.Post, $"{uriResolver.GetProviderUri("ProviderOne")}/search")
                    .Respond(request =>
                    {
                        var content = request.Content.ReadAsStringAsync().Result;
                        var searchRequest = JsonSerializer.Deserialize<ProviderOneSearchRequest>(content, options);

                        var response = responseGenerator.GenerateResponse(searchRequest);
                        var jsonResponse = JsonSerializer.Serialize(response);

                        return new HttpResponseMessage
                        {
                            StatusCode = System.Net.HttpStatusCode.OK,
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                        };
                    });


            mockHttp.When(HttpMethod.Post, $"{uriResolver.GetProviderUri("ProviderTwo")}/search")
                    .Respond(request =>
                    {
                        var content = request.Content.ReadAsStringAsync().Result;
                        var searchRequest = JsonSerializer.Deserialize<ProviderTwoSearchRequest>(content, options);

                        var response = responseGenerator.GenerateResponse(searchRequest);
                        var jsonResponse = JsonSerializer.Serialize(response);

                        return new HttpResponseMessage
                        {
                            StatusCode = System.Net.HttpStatusCode.OK,
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                        };
                    });


            var client = new HttpClient(mockHttp);

            return client;
        }
    }


    public class ResponseGenerator
    {
        private readonly Random _rand;

        public ResponseGenerator()
        {
            _rand = new Random();    
        }

        public ProviderOneSearchResponse GenerateResponse(ProviderOneSearchRequest request)
        {
            return new ProviderOneSearchResponse
            {
                Routes = Enumerable.Range(1, 3).Select(i => new ProviderOneRoute
                {
                    From = request.From,
                    To = request.To,
                    DateFrom = request.DateFrom.AddHours(i),
                    DateTo = request.DateFrom.AddHours(i).AddMinutes(_rand.Next(60, 600)),  
                    Price = 100 + (i * 10),
                    TimeLimit = DateTime.UtcNow.AddSeconds(_rand.Next(1,200))
                }).ToArray()
            };
        }

        public ProviderTwoSearchResponse GenerateResponse(ProviderTwoSearchRequest request)
        {
            return new ProviderTwoSearchResponse
            {
                Routes = Enumerable.Range(1, 3).Select(i => new ProviderTwoRoute
                {
                    Departure = new ProviderTwoPoint { Point = request.Departure, Date = request.DepartureDate.AddHours(i) },
                    Arrival = new ProviderTwoPoint { Point = request.Arrival, Date = request.DepartureDate.AddHours(i).AddMinutes(_rand.Next(60, 600)) },

                    Price = 100 + (i * 10),
                    TimeLimit = DateTime.UtcNow.AddSeconds(_rand.Next(1, 200))
                }).ToArray()
            };
        }
    }

}