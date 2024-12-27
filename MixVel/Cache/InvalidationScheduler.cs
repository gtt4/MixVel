using MixVel.Interfaces;

namespace MixVel.Cache
{
    public class InvalidationScheduler : IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _backgroundTask;
        private readonly ILogger<InvalidationScheduler> _logger;
        private readonly IRoutesCacheService _cache;
        private readonly int _defaultDelayMax = 60;
        private readonly int _defaultDelayMin = 5;

        public InvalidationScheduler(IRoutesCacheService cache, ILogger<InvalidationScheduler> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backgroundTask = Task.Run(InvalidatePeriodicallyAsync);
        }

        private async Task InvalidatePeriodicallyAsync()
        {
            try
            {
                while (!_cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        InvalidateIfNecessary(_cts.Token);
                        var delay = ComputeDelay();
                        await Task.Delay(delay, _cts.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred during cache invalidation.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "The invalidation task encountered a fatal error and will terminate.");
            }
        }


        private TimeSpan ComputeDelay()
        {
            var now = DateTime.UtcNow;
            var earliestTimeLimit = new DateTime(_cache.EarliestTimeLimitTicks);

            var delay = earliestTimeLimit > now
                ? earliestTimeLimit - now
                : TimeSpan.FromSeconds(_defaultDelayMax);

            var clampedSeconds = Math.Clamp(delay.TotalSeconds, _defaultDelayMin, _defaultDelayMax);
            return TimeSpan.FromSeconds(clampedSeconds);
        }

        private void InvalidateIfNecessary(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _cache.Invalidate();
        }

        public async ValueTask DisposeAsync()
        {
            _cts.Cancel();
            try
            {
                await _backgroundTask;
            }
            catch (OperationCanceledException)
            {
                // 
            }
            _cts.Dispose();
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
