using MixVel.Providers.ProviderOne;
using MixVel.Providers.ProviderTwo;
using MixVel.Settings;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace MockProviderClients
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

            mockHttp.When(HttpMethod.Post, $"{uriResolver.GetProviderUri("ProviderOne")}/search")
                    .Respond(request =>
                    {
                        var content = request.Content.ReadAsStringAsync().Result;
                        var searchRequest = JsonSerializer.Deserialize<ProviderOneSearchRequest>(content, options);

                        var response = ResponseGenerator.GenerateResponse(searchRequest);
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

                        var response = ResponseGenerator.GenerateResponse(searchRequest);
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
        public static ProviderOneSearchResponse GenerateResponse(ProviderOneSearchRequest request)
        {
            return new ProviderOneSearchResponse
            {
                Routes = Enumerable.Range(1, 3).Select(i => new ProviderOneRoute
                {
                    From = request.From,
                    To = request.To,
                    DateFrom = request.DateFrom.AddHours(i),
                    Price = 100 + (i * 10),
                    TimeLimit = DateTime.UtcNow.AddMinutes(1)
                }).ToArray()
            };
        }

        public static ProviderTwoSearchResponse GenerateResponse(ProviderTwoSearchRequest request)
        {
            return new ProviderTwoSearchResponse
            {
                Routes = Enumerable.Range(1, 3).Select(i => new ProviderTwoRoute
                {
                    Departure = new ProviderTwoPoint { Point = request.Departure, Date = request.DepartureDate.AddHours(i) },
                    Arrival = new ProviderTwoPoint { Point = request.Arrival },

                    Price = 100 + (i * 10),
                    TimeLimit = DateTime.UtcNow.AddMinutes(1)
                }).ToArray()
            };
        }
    }
}
