using Microsoft.Extensions.Logging;
using MixVel.Interfaces;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class RouteCacheTest
    {
        [Test]
        public void Add_ShouldCacheRoutes_WhenRoutesAreValid()
        {
            // Arrange
            var loggerMock = Mock.Of<ILogger<RoutesCacheService>>();
            var cacheService = new RoutesCacheService(loggerMock);
            var route = new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "B",
                TimeLimit = DateTime.UtcNow.AddMinutes(30)
            };

            // Act
            cacheService.Add(new[] { route });

            // Assert
            var cachedRoutes = cacheService.Get(new SearchRequest { Origin = "A" }).ToList();
            Assert.Contains(route, cachedRoutes);
        }

        [Test]
        public void Invalidate_ShouldRemoveExpiredRoutes()
        {
            // Arrange
            var loggerMock = Mock.Of<ILogger<RoutesCacheService>>();
            var cacheService = new RoutesCacheService(loggerMock);
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
            var cacheService = new RoutesCacheService(loggerMock);
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
