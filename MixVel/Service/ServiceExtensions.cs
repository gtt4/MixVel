using AutoMapper;
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

            
            bool useMockClients = configuration.GetValue<bool>("UseMockClients");

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

                var configuration = new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<ProviderOneMappingProfile>();
                    cfg.AddProfile<ProviderTwoMappingProfile>();
                });
                var mapper = configuration.CreateMapper();

                var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneRoute>(
                    clientOne, mapper);

                var providerTwo = new ProviderAdapter<ProviderTwoSearchRequest, ProviderTwoRoute>(
                    clientTwo, mapper);



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
