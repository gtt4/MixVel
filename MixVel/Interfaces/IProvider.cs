using TestTask;

namespace MixVel.Interfaces
{
    public interface IProvider
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);
    }

    public interface IProviderOne: IProvider
    {
        Task<ProviderOneSearchResponse> SearchAsync(ProviderOneSearchRequest request, CancellationToken cancellationToken);
    }

    public interface IProviderTwo: IProvider
    {
        Task<ProviderTwoSearchResponse> SearchAsync(ProviderTwoSearchRequest request, CancellationToken cancellationToken);
    }
}
