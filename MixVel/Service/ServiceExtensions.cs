using Microsoft.Extensions.DependencyInjection;
using MixVel.Cache;
using MixVel.Interfaces;
using MixVel.Providers;
using MixVel.Providers.ProviderOne;
using MixVel.Providers.ProviderTwo;
using MixVel.Settings;

namespace MixVel.Service
{
    public static class ServicesExtensions
    {
        public static IServiceCollection AddCustomServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IRoutesCacheService, RoutesCacheService>();
            services.AddSingleton<InvalidationScheduler>();

            // Determine whether to use mock clients
            //bool useMockClients = configuration.GetValue<bool>("UseMockClients");

            services.AddSingleton<ISearchService>(provider =>
            {
                var cache = provider.GetRequiredService<IRoutesCacheService>();
                var scheduler = provider.GetRequiredService<InvalidationScheduler>();

                var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
                var logger = provider.GetRequiredService<ILogger<SearchService>>();


                HttpClient httpClientOne;
                HttpClient httpClientTwo;

                if (true)
                {

                    httpClientOne = new MockClient().CreateMockClient(provider.GetRequiredService<IProviderUriResolver>());
                    httpClientTwo = httpClientOne;
                }
                else
                {
                    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();

                    httpClientOne = httpClientFactory.CreateClient("ProviderOneClient");
                    httpClientTwo = httpClientFactory.CreateClient("ProviderTwoClient");
                }

                var clientOne = new ProviderOneClient(httpClientOne, uriResolver);
                var clientTwo = new ProviderTwoClient(httpClientTwo, uriResolver);

                var converterOne = new ProviderOneConverter();
                var converterTwo = new ProviderTwoConverter();

                var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneSearchResponse, ProviderOneRoute>(
                    clientOne, converterOne);

                var providerTwo = new ProviderAdapter<ProviderTwoSearchRequest, ProviderTwoSearchResponse, ProviderTwoRoute>(
                    clientTwo, converterTwo);



                return new SearchService([providerOne, providerTwo], cache, logger);
            });

            //// Register HTTP clients if using real clients
            //if (!useMockClients)
            //{
            //    services.AddHttpClient("ProviderOneClient", (provider, client) =>
            //    {
            //        var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
            //        client.BaseAddress = new Uri(uriResolver.GetProviderOneUri());
            //        // Configure client settings if needed
            //    });

            //    services.AddHttpClient("ProviderTwoClient", (provider, client) =>
            //    {
            //        var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
            //        client.BaseAddress = new Uri(uriResolver.GetProviderTwoUri());
            //        // Configure client settings if needed
            //    });
            //}

            return services;
        }
    }
}
