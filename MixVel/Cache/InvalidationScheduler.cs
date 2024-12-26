using MixVel.Interfaces;

namespace MixVel.Cache
{
    public class InvalidationScheduler : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _backgroundTask;
        private readonly ILogger<InvalidationScheduler> _logger;
        private readonly IRoutesCacheService _cache;

        public InvalidationScheduler(IRoutesCacheService cache, ILogger<InvalidationScheduler> logger)
        {
            _cache = cache;
            _backgroundTask = Task.Run(InvalidatePeriodicallyAsync);
            _logger = logger;
        }

        private async Task InvalidatePeriodicallyAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    InvalidateIfNecessary();
                    var delay = ComputeDelay();
                    _logger.LogInformation($"invalidation delay = {delay.TotalMinutes}");
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

            var delay = earliestTimeLimit <= now
                ? TimeSpan.FromMinutes(1) // Default delay
                : earliestTimeLimit - now;

            return TimeSpan.FromSeconds(Math.Clamp(delay.TotalSeconds, 5, 50));

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
