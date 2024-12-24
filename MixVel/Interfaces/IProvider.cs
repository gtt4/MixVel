namespace MixVel.Interfaces
{
    public interface IProvider
    {
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

        public Task<IEnumerable<Route>> SearchAsync(SearchRequest request, CancellationToken cancellationToken);
    }
}
