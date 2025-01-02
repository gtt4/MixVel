using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixVel.Cache;
using MixVel.Interfaces;
using Moq;

namespace Tests;

public class RouteCacheTest
{
    private RoutesCacheService _cacheService;

    [SetUp]
    public void Setup()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();

        loggerFactoryMock
            .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);
        var mockMetricsService = new Mock<IMetricsService>();

        var cacheSettings = new CacheSettings
        {
            TimeBucketRangeInMin = 1,
            MinRoutesToInvalidate = 1
        };

        var optionsMock = new Mock<IOptions<CacheSettings>>();
        optionsMock.Setup(o => o.Value).Returns(cacheSettings);
        _cacheService = new RoutesCacheService(loggerFactoryMock.Object, mockMetricsService.Object, optionsMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _cacheService.Dispose();
    }


    [Test]
    public void Add_ShouldCacheRoutes_WhenRoutesAreValid()
    {
        // Arrange

        var route = new Route
        {
            Origin = "A",
            Destination = "B",
            TimeLimit = DateTime.UtcNow.AddMinutes(30)
        };

        // Act
        _cacheService.Add(new[] { route });

        // Assert
        var cachedRoutes = _cacheService.Get(new SearchRequest { Origin = "A" }).ToList();
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

        var expiredRoute = new Route
        {
            Id = Guid.NewGuid(),
            Origin = "A",
            Destination = "B",
            TimeLimit = DateTime.UtcNow.AddMinutes(-1)
        };
        _cacheService.Add(new[] { expiredRoute });

        // Act
        _cacheService.Invalidate(new CancellationToken());

        // Assert
        var cachedRoutes = _cacheService.Get(new SearchRequest { Origin = "A" });

        Assert.IsEmpty(cachedRoutes);
    }

    [Test]
    public void Invalidate_ShouldFilterOutExpiredRoutes()
    {
        // Arrange

        var expiredRoute = new Route
        {
            Id = Guid.NewGuid(),
            Origin = "A",
            Destination = "B",
            TimeLimit = DateTime.UtcNow.AddMinutes(-1)
        };
        _cacheService.Add(new[] { expiredRoute });

        // Act
        // Invalidation not yet happend

        // Assert
        var cachedRoutes = _cacheService.Get(new SearchRequest { Origin = "A" });

        Assert.IsEmpty(cachedRoutes);
    }
    [Test]
    public void Add_ShouldNotAddDuplicateRoutesToCache()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var routes = new List<Route>
        {
            new Route { Id = Guid.NewGuid(), Origin = "A", Destination = "B", TimeLimit = now.AddHours(1) },
            new Route { Id = Guid.NewGuid(), Origin = "A", Destination = "B", TimeLimit = now.AddHours(1) }, // Duplicate
            new Route { Id = Guid.NewGuid(), Origin = "C", Destination = "D", TimeLimit = now.AddHours(2) }
        };

        // Act
        _cacheService.Add(routes);

        // Assert
        var result = _cacheService.Get(new SearchRequest { Origin = "A" }).ToList();

        Assert.That(result, Has.Count.EqualTo(1)); // Only one route from "A" to "B" should exist
        Assert.That(result, Has.Exactly(1).Matches<Route>(r =>
            r.Origin == "A" && r.Destination == "B" && r.TimeLimit == now.AddHours(1)));

        var allRoutes = _cacheService.Get(new SearchRequest { Origin = "C" }).ToList();
        Assert.That(allRoutes, Has.Count.EqualTo(1)); // Verify other routes are unaffected
        Assert.That(allRoutes, Has.Exactly(1).Matches<Route>(r =>
            r.Origin == "C" && r.Destination == "D" && r.TimeLimit == now.AddHours(2)));
    }
}

