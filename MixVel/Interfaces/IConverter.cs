using TestTask;

namespace MixVel.Interfaces
{
    public interface IConverter<ProviderRequest, ProviderResponse, ProviderRoute>
    {
        ProviderRequest ConvertRequest(SearchRequest request);
        Route ConvertRoute(ProviderRoute route); // TODO hide
        IEnumerable<Route> ConvertRoutes(ProviderResponse? response);
        
    }
}
