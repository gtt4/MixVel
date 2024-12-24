using MixVel.Interfaces;
using MixVel.Providers;
using MixVel.Service;
using Moq;
using TestTask;

namespace Tests
{
    public class Tests
    {

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



            var service = new SearchService(new List<IProvider>() {providerOne, providerTwo});
            var result = await service.SearchAsync(searchRequest, new CancellationToken());



            Assert.Pass();
        }
    }
}