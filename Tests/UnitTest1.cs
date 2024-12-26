using MixVel.Interfaces;
using MixVel.Providers;
using MixVel.Service;
using Moq;
using TestTask;
using RichardSzalay.MockHttp;
using System.Text;
using System.Text.Json;


namespace Tests
{
    public class Tests
    {

        private const string ProviderOnePingUrl = "http://provider-one/api/v1/ping";
        private const string ProviderTwoPingUrl = "http://provider-two/api/v1/ping";

        private const string ProviderOneSearchUrl = "http://provider-one/api/v1/search";
        private const string ProviderTwoSearchUrl = "http://provider-two/api/v1/search";

        private readonly Mock<IProviderClient<ProviderOneSearchRequest, ProviderOneSearchResponse>> _providerOneClientMock = new Mock<IProviderClient<ProviderOneSearchRequest, ProviderOneSearchResponse>>();
        private readonly Mock<IProviderClient<ProviderTwoSearchRequest, ProviderTwoSearchResponse>> _providerTwoClientMock = new Mock<IProviderClient<ProviderTwoSearchRequest, ProviderTwoSearchResponse>> ();

        [SetUp]
        public void Setup() 
        {
            //IProviderOne _providerOne = new 
        }

        [Test]
        public async Task Test1()
        {
            var searchRequest = new SearchRequest();
            searchRequest.Origin = "Moscow";
            searchRequest.Destination = "Sochi";
            searchRequest.OriginDateTime = new DateTime(2025, 01, 01);
            var converterOne = new ProviderOneConverter();
            var converterTwo = new ProviderTwoConverter();

            _providerOneClientMock.Setup(x => x.SearchAsync(converterOne.ConvertRequest(searchRequest), It.IsAny<CancellationToken>())).
                ReturnsAsync(new ProviderOneSearchResponse() 
                {
                    Routes = new[] {
                            new ProviderOneRoute
                                {
                                    From = "Moscow",
                                    To = "Sochi",
                                    DateFrom = new DateTime(2025, 1, 1, 8, 0, 0),
                                    DateTo = new DateTime(2025, 1, 1, 12, 0, 0),
                                    Price = 1500.0m,
                                    TimeLimit = new DateTime(2025, 1, 1, 18, 0, 0)
                                },
                                new ProviderOneRoute
                                {
                                    From = "Moscow",
                                    To = "Sochi",
                                    DateFrom = new DateTime(2025, 1, 1, 15, 0, 0),
                                    DateTo = new DateTime(2025, 1, 1, 19, 0, 0),
                                    Price = 1700.0m,
                                    TimeLimit = new DateTime(2025, 1, 1, 23, 59, 59)
                                }
                    }
                });

            _providerTwoClientMock.Setup(x => x.SearchAsync(converterTwo.ConvertRequest(searchRequest), It.IsAny<CancellationToken>())).
    ReturnsAsync(new ProviderTwoSearchResponse()
    {
        Routes = new[] {
                            new ProviderTwoRoute
                                {
                                    Departure =  new ProviderTwoPoint () {Point = "Moscow", Date =  new DateTime(2025, 1, 1, 8, 0, 0)},
                                    Arrival =  new ProviderTwoPoint () {Point = "Sochi", Date =  new DateTime(2025, 1, 1, 14, 0, 0)},
                                    Price = 1400.0m,
                                    TimeLimit = new DateTime(2025, 1, 1, 18, 0, 0)
                                },
                                new ProviderTwoRoute
                                {
                                    Departure =  new ProviderTwoPoint () {Point = "Moscow", Date =  new DateTime(2025, 1, 1, 8, 0, 0)},
                                    Arrival =  new ProviderTwoPoint () {Point = "Sochi", Date =  new DateTime(2025, 1, 1, 12, 0, 0)},
                                    Price = 1700.0m,
                                    TimeLimit = new DateTime(2025, 1, 1, 18, 0, 0)
                                }
        }
    });

            var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(_providerOneClientMock.Object, new ProviderOneConverter());
            var providerTwo = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(_providerOneClientMock.Object, new ProviderOneConverter());


            IRoutesCacheService cache = new RoutesCacheService();
            var service = new SearchService(new List<IProvider>() {providerOne, providerTwo}, cache);
            var result = await service.SearchAsync(searchRequest, new CancellationToken());



            Assert.Pass();
        }

        [Test]
        public async Task IntegrationTest()
        {
            IRoutesCacheService cache = new RoutesCacheService();
            //var invalidationScheduler = new InvalidationScheduler(cache);

            var converterOne = new ProviderOneConverter();
            var converterTwo = new ProviderTwoConverter();


            //// Arrange
            //var mockHttp = new MockHttpMessageHandler();

            //// Mock provider one as ready (200)
            //mockHttp.When(ProviderOnePingUrl)
            //        .Respond("application/json", "OK"); // 200 by default

            //// Mock provider two as down (500)
            //mockHttp.When(ProviderTwoPingUrl)
            //        .Respond(System.Net.HttpStatusCode.InternalServerError);

            // Arrange
            var mockHttp = new MockHttpMessageHandler();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Enables case-insensitive matching
            };
            // Mock Provider One
            mockHttp.When(HttpMethod.Post, ProviderOneSearchUrl)
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

            // Mock Provider Two
            mockHttp.When(HttpMethod.Post, ProviderTwoSearchUrl)
                    .Respond(request =>
                    {
                        var content = request.Content.ReadAsStringAsync().Result;
                        var searchRequest = JsonSerializer.Deserialize<ProviderTwoSearchRequest>(content, options); // Reuse model for simplicity

                        var response = ResponseGenerator.GenerateResponse(searchRequest); // Reuse generator for simplicity
                        var jsonResponse = JsonSerializer.Serialize(response);

                        return new HttpResponseMessage
                        {
                            StatusCode = System.Net.HttpStatusCode.OK,
                            Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                        };
                    });


            var client = new HttpClient(mockHttp);

            // Act

            // Assert
            //Assert.That(providerOneResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
            //Assert.That(providerTwoResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.InternalServerError));


            var clientOne = new ProviderOneClient(mockHttp.ToHttpClient());
            var clientTwo = new ProviderTwoClient(mockHttp.ToHttpClient());

            var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(clientOne, converterOne);
            var providerTwo = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(clientOne, converterOne);

            var service = new SearchService([providerOne, providerTwo], cache);

            var searchRequest = new SearchRequest();
            searchRequest.Origin = "Moscow";
            searchRequest.Destination = "Sochi";
            searchRequest.OriginDateTime = new DateTime(2025, 01, 01);

            var result = await service.SearchAsync(searchRequest, new CancellationToken());
            searchRequest.Filters = new SearchFilters() { OnlyCached = true };
            var resultFromCache = await service.SearchAsync(searchRequest, new CancellationToken());





        }
    }
}