﻿using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

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

        public async Task<SearchResponse> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
        {

            var tasks = _providers.Select(async client =>
            {
                var routes = await GetRoutesFromProviderAsync(client, request, cancellationToken);
                var aggregate = Aggregate(routes);
                return aggregate;
            });

            var partialAggregates = await Task.WhenAll(tasks);
            return MergeRouteAggregates(partialAggregates);
        }

        private SearchResponse MergeRouteAggregates(RoutesAggregate[] partialAggregates)
        {
            var searchResponse = new SearchResponse();
            var notEmptyPartialAggregates = partialAggregates.Where(x => x.HaveResult);
            searchResponse.Routes = notEmptyPartialAggregates.Select(x => x.Routes).SelectMany(x => x).ToArray();
            searchResponse.MinMinutesRoute = notEmptyPartialAggregates.Min(x => x.MinTime);
            searchResponse.MinPrice = notEmptyPartialAggregates.Min(x => x.MinPrice);
            return searchResponse;
        }

        private RoutesAggregate Aggregate(IEnumerable<Route> routes)
        {
            var first = routes.FirstOrDefault();
            var minPrice = first.Price;
            var minTime = first.DestinationDateTime - first.OriginDateTime;

            foreach (var item in routes)
            {
                if (minPrice > item.Price)
                    minPrice = item.Price;

                if (minTime > item.DestinationDateTime - item.OriginDateTime)
                    minTime = item.DestinationDateTime - item.OriginDateTime;
            }

            return new RoutesAggregate()
            {
                Routes = routes,
                MinPrice = minPrice,
                MinTime = (int)minTime.TotalMinutes
            };
        }


        private async Task<IEnumerable<Route>> GetRoutesFromProviderAsync(IProvider client, SearchRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return await client.SearchAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                return Enumerable.Empty<Route>();
            }
        }
    }
}
