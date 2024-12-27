using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

namespace MixVel.Providers
{
    public interface IProvider
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

        public Task<IEnumerable<Route>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
    }
}
