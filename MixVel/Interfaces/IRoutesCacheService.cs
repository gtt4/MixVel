namespace MixVel.Interfaces
{
    public interface IRoutesCacheService
    {
        void Add(IEnumerable<Route> routes);
        IEnumerable<Route> Get(SearchRequest request);
        void Invalidate();
        DateTime EarliestTimeLimit { get; set; }
    }
}
