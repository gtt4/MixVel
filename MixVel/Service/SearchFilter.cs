using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

internal class SearchFilter
{
    public IEnumerable<Route> ApplyFilters(SearchFilters filters, IEnumerable<Route> routes)
    {
        if (filters == null) return routes;

        return routes.Where(route =>
            (!filters.DestinationDateTime.HasValue ||
             route.DestinationDateTime.Date <= filters.DestinationDateTime.Value.Date) &&
            (!filters.MaxPrice.HasValue ||
             route.Price <= filters.MaxPrice.Value) &&
            (!filters.MinTimeLimit.HasValue ||
             route.TimeLimit >= filters.MinTimeLimit.Value));
    }

}