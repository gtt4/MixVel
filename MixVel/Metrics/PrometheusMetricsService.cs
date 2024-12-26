using Prometheus;

public class PrometheusMetricsService : IMetricsService
{
    private readonly Counter _cacheHits = Metrics.CreateCounter("cache_hits", "Number of cache hits", "origin");
    private readonly Counter _cacheMisses = Metrics.CreateCounter("cache_misses", "Number of cache misses", "origin");
    private readonly Histogram _searchLatency = Metrics.CreateHistogram("search_latency_seconds", "Search latency", "provider");

    public void IncrementCounter(string metricName, string[]? labels = null)
    {
        switch (metricName)
        {
            case "cache_hits":
                _cacheHits.WithLabels(labels ?? new string[] { }).Inc();
                break;
            case "cache_misses":
                _cacheMisses.WithLabels(labels ?? new string[] { }).Inc();
                break;
        }
    }

    public void ObserveHistogram(string metricName, double value, string[]? labels = null)
    {
        switch (metricName)
        {
            case "search_latency_seconds":
                _searchLatency.WithLabels(labels ?? new string[] { }).Observe(value);
                break;
        }
    }
}
