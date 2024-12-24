using MixVel.Interfaces;
using MixVel.Providers;
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
        public void Test1()
        {
            var searchRequest = new SearchRequest();
            searchRequest.Origin = "Moscow";
            searchRequest.Destination = "Sochi";
            searchRequest.OriginDateTime = new DateTime(2025, 01, 01);
            var converterOne = new ProviderOneConverter();

            var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(_providerOneClientMock.Object, new ProviderOneConverter());
            var providerTwo = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(_providerOneClientMock.Object, new ProviderOneConverter());



            Assert.Pass();
        }
    }
}