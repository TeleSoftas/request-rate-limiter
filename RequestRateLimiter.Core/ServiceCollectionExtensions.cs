using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RequestRateLimiter.Core.Configuration;
using RequestRateLimiter.Core.Helpers;

namespace RequestRateLimiter.Core
{
    public static class ServiceCollectionExtensions
    {
        private const string ConfigName = "RequestRateLimiter";
        
        public static IApplicationBuilder UseRequestRateLimiter(this IApplicationBuilder applicationBuilder)
        {
            return applicationBuilder.UseMiddleware<RequestRateLimiterMiddleware>();
        }

        /// <summary>
        /// Configures the rate limiter by getting the configuration section from <see cref="IConfiguration"/>.
        /// </summary>
        public static IServiceCollection ConfigureRequestRateLimiter(this IServiceCollection services, IConfiguration configuration)
        {
            services.RegisterDependencies();
            return services.Configure<RequestRateLimiterConfiguration>(configuration.GetSection(ConfigName));
        }
        
        /// <summary>
        /// Configures the rate limiter by explicitly providing <see cref="RequestRateLimiterConfiguration"/>.
        /// </summary>
        public static IServiceCollection ConfigureRequestRateLimiter(this IServiceCollection services, Action<RequestRateLimiterConfiguration> configurationAction)
        {
            services.RegisterDependencies(); return services.Configure(configurationAction);
        }

        /// <summary>
        /// Configures the rate limiter by getting the configuration section from <see cref="IConfiguration"/> and by explicitly providing <see cref="RequestRateLimiterConfiguration"/>.
        /// </summary>
        public static IServiceCollection ConfigureRequestRateLimiter(this IServiceCollection services, IConfiguration configuration, Action<RequestRateLimiterConfiguration> configurationAction)
        {
            services.RegisterDependencies(); 
            services.Configure<RequestRateLimiterConfiguration>(configuration.GetSection(ConfigName));
            return services.Configure(configurationAction);
        }

        private static IServiceCollection RegisterDependencies(this IServiceCollection services)
        {
            services.AddSingleton<IDateTime, DateTimeHelper>();
            services.AddSingleton<IMemoryCacheHelper, MemoryCacheHelper>();
            return services;
        }
    }
}
