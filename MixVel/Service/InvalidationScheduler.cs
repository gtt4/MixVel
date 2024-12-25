using MixVel.Interfaces;

namespace MixVel.Service
{
    public class InvalidationScheduler : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _backgroundTask;
        private readonly IRoutesCacheService _cache;

        public InvalidationScheduler(IRoutesCacheService cache)
        {
            _cache = cache;
            _backgroundTask = Task.Run(InvalidatePeriodicallyAsync);
        }

        private async Task InvalidatePeriodicallyAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    InvalidateIfNecessary();
                    var delay = ComputeDelay();
                    await Task.Delay(delay, _cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, exit the loop
            }
        }

        private TimeSpan ComputeDelay()
        {
            var now = DateTime.UtcNow;
            var earliestTimeLimit = _cache.EarliestTimeLimit;

            if (earliestTimeLimit == DateTime.MaxValue || earliestTimeLimit <= now)
            {
                return TimeSpan.FromMinutes(1); // Default delay
            }
            else
            {
                var delay = earliestTimeLimit - now;
                return delay < TimeSpan.FromSeconds(5) ? TimeSpan.FromSeconds(5) : delay;
            }
        }

        private void InvalidateIfNecessary()
        {
            try
            {
                _cache.Invalidate();
            }
            finally
            {
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            try
            {
                _backgroundTask.Wait();
            }
            catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is TaskCanceledException))
            {
                // 
            }
            _cts.Dispose();
        }
    }

}
