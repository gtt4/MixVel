using Microsoft.Extensions.Options;

namespace MixVel.Settings
{
    public class ProviderUriResolver : IProviderUriResolver
    {
        private readonly IOptions<ProviderSettings> _settings;

        public ProviderUriResolver(IOptions<ProviderSettings> settings)
        {
            _settings = settings;
        }

        public string GetProviderUri(string providerName)
        {
            return providerName switch
            {
                "ProviderOne" => _settings.Value.ProviderOne,
                "ProviderTwo" => _settings.Value.ProviderTwo,
                _ => throw new ArgumentException($"Unknown provider: {providerName}", nameof(providerName))
            };
        }
    }

}
