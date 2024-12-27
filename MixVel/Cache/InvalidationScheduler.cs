using MixVel.Interfaces;

namespace MixVel.Cache
{
    public class InvalidationScheduler : IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _backgroundTask;
        private readonly ILogger<InvalidationScheduler> _logger;
        private readonly IRoutesCacheService _cache;
        private readonly int _defaultDelayMax = 60;
        private readonly int _defaultDelayMin = 5;

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
                // 
            }
        }

        private TimeSpan ComputeDelay()
        {
            var now = DateTime.UtcNow.Ticks;
            var earliestTimeLimit = _cache.EarliestTimeLimitTicks;

            var delay = earliestTimeLimit <= now
                ? TimeSpan.FromSeconds(_defaultDelayMax * 2)
                : TimeSpan.FromTicks(earliestTimeLimit - now);

            return TimeSpan.FromSeconds(Math.Clamp(delay.TotalSeconds, _defaultDelayMin, _defaultDelayMax));
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
