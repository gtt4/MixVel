using MixVel.Interfaces;
using Route = MixVel.Interfaces.Route;

internal class SearchFilter
{
    public IEnumerable<Route?> ApplyFilters(SearchFilters filters, IEnumerable<Route?> routes)
    {

        if (filters.DestinationDateTime.HasValue)
        {
            routes = routes.Where(route => route.DestinationDateTime.Date <= filters.DestinationDateTime.Value.Date);
        }

        if (filters.MaxPrice.HasValue)
        {
            routes = routes.Where(route => route.Price <= filters.MaxPrice.Value);
        }

        if (filters.MinTimeLimit.HasValue)
        {
            routes = routes.Where(route => route.TimeLimit >= filters.MinTimeLimit.Value);
        }


        return routes;
    }
}