using MixVel.Providers.ProviderOne;
using MixVel.Providers.ProviderTwo;

namespace MixVel.Interfaces
{
    public interface IProviderClient<Request, Route>
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
        Task<Route[]> SearchAsync(Request request, CancellationToken cancellationToken);
    }
}
