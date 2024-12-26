using MixVel.Settings;
using System.Runtime;

namespace Tests
{
    internal class TestProviderUriResolver : IProviderUriResolver
    {
        public string GetProviderUri(string providerName)
        {
            return providerName switch
            {
                "ProviderOne" => "http://provider-one/api/v1/",
                "ProviderTwo" => "http://provider-two/api/v1/",
                _ => throw new ArgumentException($"Unknown provider: {providerName}", nameof(providerName))
            };
        }
    }
}