using Microsoft.Extensions.Logging;
using MixVel.Interfaces;
using MixVel.Providers;
using MixVel.Service;
using Moq;

namespace Tests
{
    public class SearchServiceTest
    {
        [Test]
        public async Task SearchAsync_ShouldReturnCachedRoutes_WhenOnlyCachedFilterIsApplied()
        {
            // Arrange
            var cacheMock = new Mock<IRoutesCacheService>();
            var loggerMock = Mock.Of<ILogger<SearchService>>();
            var providers = new List<IProvider>();
            var searchService = new SearchService(providers, cacheMock.Object, loggerMock);

            var request = new SearchRequest
            {
                Filters = new SearchFilters { OnlyCached = true },
                Origin = "A"
            };

            var cachedRoutes = new List<Route> { new Route { Origin = "A", Destination = "B" } };
            cacheMock.Setup(c => c.Get(request)).Returns(cachedRoutes);

            // Act
            var response = await searchService.SearchAsync(request, CancellationToken.None);

            // Assert
            
            CollectionAssert.AreEqual(cachedRoutes, response.Routes);
            cacheMock.Verify(c => c.Get(request), Times.Once); 
        }

    }
}
