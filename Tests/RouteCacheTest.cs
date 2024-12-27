using Microsoft.Extensions.Logging;
using MixVel.Interfaces;
using Moq;

namespace Tests
{
    public class RouteCacheTest
    {
        [Test]
        public void Add_ShouldCacheRoutes_WhenRoutesAreValid()
        {
            // Arrange
            var loggerMock = Mock.Of<ILogger<RoutesCacheService>>();
            var metricsMock = Mock.Of<IMetricsService>();

            var cacheService = new RoutesCacheService(loggerMock, metricsMock);
            var route = new Route
            {
                Origin = "A",
                Destination = "B",
                TimeLimit = DateTime.UtcNow.AddMinutes(30)
            };

            // Act
            cacheService.Add(new[] { route });

            // Assert
            var cachedRoutes = cacheService.Get(new SearchRequest { Origin = "A" }).ToList();
            foreach (var cachedRoute in cachedRoutes)
            {
                Assert.IsTrue(Guid.Empty != cachedRoute.Id);
            }
            Assert.Contains(route, cachedRoutes);
        }

        [Test]
        public void Invalidate_ShouldRemoveExpiredRoutes()
        {
            // Arrange
            var loggerMock = Mock.Of<ILogger<RoutesCacheService>>();
            var metricsMock = Mock.Of<IMetricsService>();

            var cacheService = new RoutesCacheService(loggerMock, metricsMock);
            var expiredRoute = new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "B",
                TimeLimit = DateTime.UtcNow.AddMinutes(-1)
            };
            cacheService.Add(new[] { expiredRoute });

            // Act
            cacheService.Invalidate();

            // Assert
            var cachedRoutes = cacheService.Get(new SearchRequest { Origin = "A" });

            Assert.IsEmpty(cachedRoutes);
        }

        [Test]
        public void Invalidate_ShouldFilterOutExpiredRoutes()
        {
            // Arrange
            var loggerMock = Mock.Of<ILogger<RoutesCacheService>>();
            var metricsMock = Mock.Of<IMetricsService>();

            var cacheService = new RoutesCacheService(loggerMock, metricsMock);
            var expiredRoute = new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "B",
                TimeLimit = DateTime.UtcNow.AddMinutes(-1)
            };
            cacheService.Add(new[] { expiredRoute });

            // Act
            // Invalidation not yet happend

            // Assert
            var cachedRoutes = cacheService.Get(new SearchRequest { Origin = "A" });

            Assert.IsEmpty(cachedRoutes);
        }

    }
}
