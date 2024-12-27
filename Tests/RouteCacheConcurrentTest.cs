using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    using Microsoft.Extensions.Logging;
    using MixVel.Interfaces;
    using Moq;
    using NUnit.Framework;
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    [TestFixture]
    public class RoutesCacheServiceConcurrentTests
    {
        private RoutesCacheService _cacheService;
        private List<Route> _testRoutes;

        [SetUp]
        public void Setup()
        {
            var logger = new Mock<ILogger<RoutesCacheService>>().Object;
            var metrics = new Mock<IMetricsService>().Object;
            _cacheService = new RoutesCacheService(logger, metrics);

            _testRoutes = new List<Route>
        {
            new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "B",
                TimeLimit = DateTime.UtcNow.AddMinutes(10)
            },
            new Route
            {
                Id = Guid.NewGuid(),
                Origin = "A",
                Destination = "C",
                TimeLimit = DateTime.UtcNow.AddMinutes(15)
            },
            new Route
            {
                Id = Guid.NewGuid(),
                Origin = "D",
                Destination = "E",
                TimeLimit = DateTime.UtcNow.AddMinutes(20)
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
                Thread.Sleep(10); 
                _cacheService.Invalidate();
            }));

            Task.WaitAll(tasks.ToArray());

            var allRoutes = _cacheService.Get(new SearchRequest { Origin = "A" });
            //Assert.That(allRoutes, Is.Empty); // Expired routes should be removed
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

}
