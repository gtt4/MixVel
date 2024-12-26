namespace MixVel.Settings
{
    public interface IProviderUriResolver
    {
        public string GetProviderUri(string providerName);
    }
}