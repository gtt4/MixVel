//using MixVel.Interfaces;
//using MixVel.Providers;
//using MixVel.Service;
//using Moq;
//using RichardSzalay.MockHttp;
//using System.Text;
//using System.Text.Json;
//using MixVel;
//using MockProviderClients;
//using Microsoft.Extensions.Logging;
//using MixVel.Providers.ProviderOne;
//using MixVel.Providers.ProviderTwo;


//namespace Tests
//{
//    public class Tests
//    {



//        private readonly Mock<IProviderClient<ProviderOneSearchRequest, ProviderOneSearchResponse>> _providerOneClientMock = new Mock<IProviderClient<ProviderOneSearchRequest, ProviderOneSearchResponse>>();
//        private readonly Mock<IProviderClient<ProviderTwoSearchRequest, ProviderTwoSearchResponse>> _providerTwoClientMock = new Mock<IProviderClient<ProviderTwoSearchRequest, ProviderTwoSearchResponse>>();

//        [SetUp]
//        public void Setup()
//        {
//            //IProviderOne _providerOne = new 
//        }

//        [Test]
//        public async Task Test1()
//        {
//            var searchRequest = new SearchRequest();
//            searchRequest.Origin = "Moscow";
//            searchRequest.Destination = "Sochi";
//            searchRequest.OriginDateTime = new DateTime(2025, 01, 01);
//            var converterOne = new ProviderOneConverter();
//            var converterTwo = new ProviderTwoConverter();

//            _providerOneClientMock.Setup(x => x.SearchAsync(converterOne.ConvertRequest(searchRequest), It.IsAny<CancellationToken>())).
//                ReturnsAsync(new ProviderOneSearchResponse()
//                {
//                    Routes = new[] {
//                            new ProviderOneRoute
//                                {
//                                    From = "Moscow",
//                                    To = "Sochi",
//                                    DateFrom = new DateTime(2025, 1, 1, 8, 0, 0),
//                                    DateTo = new DateTime(2025, 1, 1, 12, 0, 0),
//                                    Price = 1500.0m,
//                                    TimeLimit = new DateTime(2025, 1, 1, 18, 0, 0)
//                                },
//                                new ProviderOneRoute
//                                {
//                                    From = "Moscow",
//                                    To = "Sochi",
//                                    DateFrom = new DateTime(2025, 1, 1, 15, 0, 0),
//                                    DateTo = new DateTime(2025, 1, 1, 19, 0, 0),
//                                    Price = 1700.0m,
//                                    TimeLimit = new DateTime(2025, 1, 1, 23, 59, 59)
//                                }
//                    }
//                });

//            _providerTwoClientMock.Setup(x => x.SearchAsync(converterTwo.ConvertRequest(searchRequest), It.IsAny<CancellationToken>())).
//    ReturnsAsync(new ProviderTwoSearchResponse()
//    {
//        Routes = new[] {
//                            new ProviderTwoRoute
//                                {
//                                    Departure =  new ProviderTwoPoint () {Point = "Moscow", Date =  new DateTime(2025, 1, 1, 8, 0, 0)},
//                                    Arrival =  new ProviderTwoPoint () {Point = "Sochi", Date =  new DateTime(2025, 1, 1, 14, 0, 0)},
//                                    Price = 1400.0m,
//                                    TimeLimit = new DateTime(2025, 1, 1, 18, 0, 0)
//                                },
//                                new ProviderTwoRoute
//                                {
//                                    Departure =  new ProviderTwoPoint () {Point = "Moscow", Date =  new DateTime(2025, 1, 1, 8, 0, 0)},
//                                    Arrival =  new ProviderTwoPoint () {Point = "Sochi", Date =  new DateTime(2025, 1, 1, 12, 0, 0)},
//                                    Price = 1700.0m,
//                                    TimeLimit = new DateTime(2025, 1, 1, 18, 0, 0)
//                                }
//        }
//    });

//            var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(_providerOneClientMock.Object, null);
//            var providerTwo = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(_providerOneClientMock.Object, null);


//            //IRoutesCacheService cache = new RoutesCacheService();
//            //var service = new SearchService(new List<IProvider>() {providerOne, providerTwo}, cache);
//            //var result = await service.SearchAsync(searchRequest, new CancellationToken());



//            Assert.Pass();
//        }
//        /*
//                private ISearchService CreateSearchService(HttpClient client)
//                {

//                    IRoutesCacheService cache = new RoutesCacheService();
//                    var invalidationScheduler = new InvalidationScheloduler(cache);

//                    var converterOne = new ProviderOneConverter();
//                    var converterTwo = new ProviderTwoConverter();

//                    IProviderUriResolver uriResolver = new TestProviderUriResolver(); 

//                    var clientOne = new ProviderOneClient(client, uriResolver);
//                    var clientTwo = new ProviderTwoClient(client, uriResolver);

//                    var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(clientOne, converterOne);
//                    var providerTwo = new ProviderAdapter<ProviderTwoSearchRequest, ProviderTwoSearchResponse, ProviderTwoRoute>(clientTwo, converterTwo);

//                    var service = new SearchService([providerOne, providerTwo], cache); // called from controler
//                    return service;
//                }



//                [Test]
//                public async Task IntegrationTest()
//                {

//                    //// Arrange
//                    var mockHttp = new MockClient().CreateMockClient(new TestProviderUriResolver());
//                    var service = CreateSearchService(mockHttp);

//                    //// Mock provider one as ready (200)
//                    //mockHttp.When(ProviderOnePingUrl)
//                    //        .Respond("application/json", "OK"); // 200 by default

//                    //// Mock provider two as down (500)
//                    //mockHttp.When(ProviderTwoPingUrl)
//                    //        .Respond(System.Net.HttpStatusCode.InternalServerError);

//                    // Arrange

//                    // Act

//                    // Assert
//                    //Assert.That(providerOneResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.OK));
//                    //Assert.That(providerTwoResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.InternalServerError));



//                    var searchRequest = new SearchRequest();
//                    searchRequest.Origin = "Moscow";
//                    searchRequest.Destination = "Sochi";
//                    searchRequest.OriginDateTime = new DateTime(2025, 01, 01);

//                    var result = await service.SearchAsync(searchRequest, new CancellationToken());
//                    searchRequest.Filters = new SearchFilters() { OnlyCached = true };
//                    var resultFromCache = await service.SearchAsync(searchRequest, new CancellationToken());

//                }
//        */
//    }
//}