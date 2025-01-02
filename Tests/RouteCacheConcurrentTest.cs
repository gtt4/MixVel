using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MixVel.Cache;
using MixVel.Interfaces;
using Moq;

namespace Tests;


[TestFixture]
public class RoutesCacheServiceConcurrentTests
{
    private RoutesCacheService _cacheService;
    private List<Route> _testRoutes;

    [SetUp]
    public void Setup()
    {
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger>();

        // Setup the factory to return the mock logger for any type
        loggerFactoryMock
            .Setup(factory => factory.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);
        var metrics = new Mock<IMetricsService>().Object;


        var cacheSettings = new CacheSettings
        {
            TimeBucketRangeInMin = 1,
            MinRoutesToInvalidate = 1,
            InvalidationDelayMinInSeconds = 1,
            InvalidationDelayMaxInSeconds = 2
        };

        var optionsMock = new Mock<IOptions<CacheSettings>>();
        optionsMock.Setup(o => o.Value).Returns(cacheSettings);
        _cacheService = new RoutesCacheService(loggerFactoryMock.Object, metrics, optionsMock.Object);

        _testRoutes = new List<Route>
        {
            new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "B",
                TimeLimit = DateTime.UtcNow.AddSeconds(2)
            },
            new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "C",
                TimeLimit = DateTime.UtcNow.AddSeconds(2)
            },
            new Route
            {
                Id = Guid.NewGuid(),
                Origin = "D",
                Destination = "E",
                TimeLimit = DateTime.UtcNow.AddSeconds(2)
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _cacheService.Dispose();
    }

    [Test]
    public void RoutesCacheService_HandlesConcurrentAccessCorrectly()
    {
        var cancellationTokenSource = new CancellationTokenSource();
        var tasks = new List<Task>();

        tasks.Add(Task.Run(() =>
        {
            foreach (var route in _testRoutes)
            {
                _cacheService.Add(new[] { route });
            }
        }));

        Task.WaitAll(tasks.ToArray());

        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 50; j++)
                {
                    var request = new SearchRequest
                    {
                        Origin = "A",
                        Filters = null
                    };
                    var routes = _cacheService.Get(request);
                    Assert.That(routes, Is.Not.Null);
                    Assert.That(routes.Any(route => route.Origin == "A"), Is.True);
                }
            }));
        }

        // Simulate invalidate operations concurrently
        tasks.Add(Task.Run(() =>
        {
            Thread.Sleep(3000);
            _cacheService.Invalidate(new CancellationToken(), force: true);
        }));

        Task.WaitAll(tasks.ToArray());

        var allRoutes = _cacheService.Get(new SearchRequest { Origin = "A" });
        Assert.That(allRoutes, Is.Empty); // Expired routes should be removed
    }

    [Test]
    public void RoutesCacheService_ReadsWhileWriting()
    {
        var writeTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var newRoute = new Route
                {
                    Id = Guid.NewGuid(),
                    Origin = "X",
                    Destination = "Y",
                    TimeLimit = DateTime.UtcNow.AddMinutes(10)
                };
                _cacheService.Add(new[] { newRoute });
                Thread.Sleep(10); // Simulate staggered writes
            }
        });

        var readTask = Task.Run(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var request = new SearchRequest
                {
                    Origin = "X",
                    Filters = null
                };
                var routes = _cacheService.Get(request);
                Assert.That(routes, Is.Not.Null);
                Thread.Sleep(5); // Simulate staggered reads
            }
        });

        Task.WaitAll(writeTask, readTask);

        // Ensure no exceptions occurred and data integrity is maintained
        var finalRoutes = _cacheService.Get(new SearchRequest { Origin = "X" });
        Assert.That(finalRoutes.Count(), Is.GreaterThanOrEqualTo(1));
    }
}


