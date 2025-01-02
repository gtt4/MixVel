using AutoMapper;
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
            services.AddSingleton<IScheduledJob>(provider => (IScheduledJob)provider.GetRequiredService<IRoutesCacheService>());
            services.AddSingleton<InvalidationScheduler>();

            services.AddSingleton<ISearchService>(provider =>
            {
                var cache = provider.GetRequiredService<IRoutesCacheService>();
                var scheduler = provider.GetRequiredService<InvalidationScheduler>();
                var uriResolver = provider.GetRequiredService<IProviderUriResolver>();
                var logger = provider.GetRequiredService<ILogger<SearchService>>();

                var httpClient = CreateHttpClient(provider);
                var mapper = CreateMapper();
                var providers = CreateProviders(provider, httpClient, uriResolver, mapper);

                return new SearchService(providers, cache, logger);
            });

            return services;
        }

        private static HttpClient CreateHttpClient(IServiceProvider provider)
        {
            var useMockClients = true; // TODO: Replace with configuration or environment setting
            return useMockClients
                ? new MockClient().CreateMockClient(provider.GetRequiredService<IProviderUriResolver>())
                : new HttpClient();
        }

        private static IMapper CreateMapper()
        {
            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProviderOneMappingProfile>();
                cfg.AddProfile<ProviderTwoMappingProfile>();
            });
            return configuration.CreateMapper();
        }

        private static List<IProvider> CreateProviders(IServiceProvider provider, HttpClient httpClient, IProviderUriResolver uriResolver, IMapper mapper)
        {
            var loggerForProviderOne = provider.GetRequiredService<ILogger<ProviderAdapter<ProviderOneSearchRequest, ProviderOneRoute>>>();
            var providerOne = new ProviderAdapter<ProviderOneSearchRequest, ProviderOneRoute>(
                new ProviderOneClient(httpClient, uriResolver), mapper, loggerForProviderOne);

            var loggerForProviderTwo = provider.GetRequiredService<ILogger<ProviderAdapter<ProviderTwoSearchRequest, ProviderTwoRoute>>>();
            var providerTwo = new ProviderAdapter<ProviderTwoSearchRequest, ProviderTwoRoute>(
                new ProviderTwoClient(httpClient, uriResolver), mapper, loggerForProviderTwo);

            return new List<IProvider> { providerOne, providerTwo };
        }
    }
}
