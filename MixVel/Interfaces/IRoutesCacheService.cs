namespace MixVel.Interfaces
{
    public interface IRoutesCacheService
    {
        void Add(IEnumerable<Route> routes);
        IEnumerable<Route> Get(SearchRequest request);
        void Invalidate(CancellationToken cancellationToken, bool force);
    }

    public interface IScheduledJob
    {
        void Execute(CancellationToken cancellationToken);
        long GetEarliestTimeLimitTicks();
    }
}
