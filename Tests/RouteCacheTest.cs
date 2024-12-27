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
        [Test]
        public void Add_ShouldNotAddDuplicateRoutesToCache()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<RoutesCacheService>>();
            var mockMetricsService = new Mock<IMetricsService>();
            var service = new RoutesCacheService(mockLogger.Object, mockMetricsService.Object);

            var now = DateTime.UtcNow;

            var routes = new List<Route>
        {
            new Route { Id = Guid.NewGuid(), Origin = "A", Destination = "B", TimeLimit = now.AddHours(1) },
            new Route { Id = Guid.NewGuid(), Origin = "A", Destination = "B", TimeLimit = now.AddHours(1) }, // Duplicate
            new Route { Id = Guid.NewGuid(), Origin = "C", Destination = "D", TimeLimit = now.AddHours(2) }
        };

            // Act
            service.Add(routes);

            // Assert
            var result = service.Get(new SearchRequest { Origin = "A" }).ToList();

            Assert.That(result, Has.Count.EqualTo(1)); // Only one route from "A" to "B" should exist
            Assert.That(result, Has.Exactly(1).Matches<Route>(r =>
                r.Origin == "A" && r.Destination == "B" && r.TimeLimit == now.AddHours(1)));

            var allRoutes = service.Get(new SearchRequest { Origin = "C" }).ToList();
            Assert.That(allRoutes, Has.Count.EqualTo(1)); // Verify other routes are unaffected
            Assert.That(allRoutes, Has.Exactly(1).Matches<Route>(r =>
                r.Origin == "C" && r.Destination == "D" && r.TimeLimit == now.AddHours(2)));
        }


    }
}
