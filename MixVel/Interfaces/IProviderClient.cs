using MixVel.Providers.ProviderOne;
using MixVel.Providers.ProviderTwo;

namespace MixVel.Interfaces
{
    public interface IProviderClient<Request, Response>
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
        Task<Response> SearchAsync(Request request, CancellationToken cancellationToken);
    }

    public interface IProviderOne: IProviderClient<ProviderOneSearchRequest, ProviderOneSearchResponse>
    {
    }

    public interface IProviderTwo: IProviderClient<ProviderTwoSearchRequest, ProviderTwoSearchResponse>
    {
    }
}
