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

            services.AddSingleton<ISearchService>(provider =>
            {
                var cache = provider.GetRequiredService<IRoutesCacheService>();
                var scheduler = provider.GetRequiredService<InvalidationScheduler>();

                var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
                var logger = provider.GetRequiredService<ILogger<SearchService>>();


                HttpClient httpClient;
                var useMockClients = false; // TODO 

                if (useMockClients)  
                {

                    httpClient = new MockClient().CreateMockClient(provider.GetRequiredService<IProviderUriResolver>());
                }
                else
                {
                    httpClient = new HttpClient(); 
                }

                var clientOne = new ProviderOneClient(httpClient, uriResolver);
                var clientTwo = new ProviderTwoClient(httpClient, uriResolver);

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


            //if (!useMockClients)
            //{
            //    services.AddHttpClient("ProviderOneClient", (provider, client) =>
            //    {
            //        var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
            //        client.BaseAddress = new Uri(uriResolver.GetProviderOneUri());
            //    });

            //    services.AddHttpClient("ProviderTwoClient", (provider, client) =>
            //    {
            //        var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
            //        client.BaseAddress = new Uri(uriResolver.GetProviderTwoUri());
            //    });
            //}

            return services;
        }
    }
}
