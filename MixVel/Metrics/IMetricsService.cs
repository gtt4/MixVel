public interface IMetricsService
{
    void IncrementCounter(string metricName, string[]? labels = null);
    void ObserveHistogram(string metricName, double value, string[]? labels = null);
}
