using MixVel.Interfaces;

namespace MixVel.Service
{
    public class SearchService: ISearchService
    {
        List<IProvider> _providers;
        
        public SearchService(List<IProvider> providers) 
        {
            _providers = providers;
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
