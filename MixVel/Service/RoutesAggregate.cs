

namespace MixVel.Service
{
    internal class RoutesAggregate
    {
        public IEnumerable<Interfaces.Route> Routes { get; internal set; }
        public decimal MinPrice { get; internal set; }
        public int MinTime { get; internal set; }
        public bool HaveResult => Routes != null && Routes.Any();
    }
}