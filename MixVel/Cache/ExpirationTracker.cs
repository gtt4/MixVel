using System.Collections.Concurrent;
using Route = MixVel.Interfaces.Route;


namespace MixVel.Cache
{
    public class ExpirationTracker : IDisposable
    {
        private readonly ConcurrentDictionary<long, ConcurrentDictionary<Guid, byte>> _timeBuckets = new();
        private readonly ILogger _logger;

        private readonly long _bucketRangeTicks;
        private long _earliestTimeLimitTicks = DateTime.MaxValue.Ticks;

        public ExpirationTracker(ILoggerFactory loggerFactory, long bucketRangeTicks)
        {
            _logger = loggerFactory.CreateLogger<ExpirationTracker>();
            _bucketRangeTicks = bucketRangeTicks;
        }

        public void AddRoute(Route route)
        {
            var bucketKey = GetBucketKey(route.TimeLimit);
            var bucket = _timeBuckets.GetOrAdd(bucketKey, _ => new ConcurrentDictionary<Guid, byte>());
            bucket[route.Id] = 0;
        }

        public IEnumerable<Guid> GetExpiredRoutes(DateTime now)
        {
            var expiredRoutes = new List<Guid>();

            foreach (var bucketKey in _timeBuckets.Keys.OrderBy(x => x))
            {
                if (IsBucketExpired(bucketKey, now))
                {
                    if (_timeBuckets.TryRemove(bucketKey, out var bucket))
                    {
                        expiredRoutes.AddRange(bucket.Keys);
                    }
                }
                else
                {
                    break; 
                }
            }

            return expiredRoutes;
        }

        public void AddRoutes(IEnumerable<Route> routes)
        {
            foreach (var item in routes)
            {
                AddRoute(item);
            }
        }

        public int GetBucketCount() =>  _timeBuckets.Count;


        private long GetBucketKey(DateTime timeLimit)
        {
            return (timeLimit.Ticks / _bucketRangeTicks); 
        }

        private bool IsBucketExpired(long bucketKey, DateTime now)
        {
            var bucketTime = new DateTime(bucketKey * _bucketRangeTicks);
            return bucketTime <= now;
        }

        public long GetEarliestTimeLimitTicks()
        {
            long earliestTicks = DateTime.MaxValue.Ticks;

            if (_timeBuckets.IsEmpty)
            {
                return earliestTicks;
            }

            var keysSnapshot = _timeBuckets.Keys.ToArray(); 
            var earliestBucket = keysSnapshot.Min();

            if (earliestBucket > 0)
            {
                earliestTicks = earliestBucket * _bucketRangeTicks;
            }

            return earliestTicks;
        }


        public void Dispose()
        {
            _timeBuckets.Clear();
        }
        
    }

}
