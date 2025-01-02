using Microsoft.Extensions.Options;
using MixVel.Interfaces;

namespace MixVel.Cache;
public class InvalidationScheduler : IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _backgroundTask;
    private readonly ILogger<InvalidationScheduler> _logger;
    private readonly IScheduledJob _periodicTask;
    private readonly CacheSettings _settings;
    
    public InvalidationScheduler(IScheduledJob periodicTask, ILogger<InvalidationScheduler> logger, IOptions<CacheSettings> settings)
    {
        if (settings.Value.InvalidationDelayMinInSeconds <= 0 || settings.Value.InvalidationDelayMinInSeconds >= settings.Value.InvalidationDelayMaxInSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(settings), "Minimum delay must be positive and less than the maximum delay.");
        }
        _periodicTask = periodicTask ?? throw new ArgumentNullException(nameof(periodicTask));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backgroundTask = Task.Run(RunPeriodicallyAsync);
        _settings = settings.Value;
    }

    private async Task RunPeriodicallyAsync()
    {
        try
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting cache invalidation.");
                    ExecuteIfNecessary(_cts.Token);
                    var delay = ComputeDelay();
                    _logger.LogInformation($"Next cache invalidation scheduled after {delay.TotalSeconds} seconds.");
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
        var earliestTimeLimit = new DateTime(_periodicTask.GetEarliestTimeLimitTicks());
        _logger.LogInformation($"The earliestTimeLimit is {earliestTimeLimit.TimeOfDay}.");
        var delay = earliestTimeLimit > now
            ? earliestTimeLimit - now
            : TimeSpan.FromSeconds(_settings.InvalidationDelayMaxInSeconds);

        var clampedSeconds = Math.Clamp(delay.TotalSeconds, _settings.InvalidationDelayMinInSeconds, _settings.InvalidationDelayMaxInSeconds);
        return TimeSpan.FromSeconds(clampedSeconds);
    }

    private void ExecuteIfNecessary(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _periodicTask.Execute(cancellationToken);
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
