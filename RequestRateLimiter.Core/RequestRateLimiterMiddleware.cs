using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using RequestRateLimiter.Core.Configuration;
using RequestRateLimiter.Core.Helpers;

namespace RequestRateLimiter.Core
{
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RequestRateLimiterMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly RequestRateLimiterConfiguration _configuration;

        public RequestRateLimiterMiddleware(
            RequestDelegate next,
            IOptionsMonitor<RequestRateLimiterConfiguration> options)
        {
            _next = next;
            _configuration = options.CurrentValue;
        }

        public async Task Invoke(HttpContext httpContext, IMemoryCacheHelper memoryCache, IDateTime dateTime)
        {
            if (!_configuration.IsEnabled)
            {
                await _next(httpContext);
                return;
            }

            //subject for optimization
            var limitSettings = await GetLimitSettings(httpContext);
            //If timeout or max request count is set to 0, then we treat the path as excluded from limiting
            if (limitSettings.LimitPeriod.TotalSeconds == 0 || limitSettings.RequestLimit == 0)
            {
                await _next(httpContext);
                return;
            }

            var keyContext = ("RateLimiter-", await _configuration.GetClientKeyFunc(httpContext),
                !string.IsNullOrEmpty(limitSettings.Route) ? $"-{PathHelper.SanitizePath(httpContext.Request.Path)}" : string.Empty);

            var keyLength = keyContext.Item1.Length + keyContext.Item2.Length + keyContext.Item3.Length;
            var key = string.Create(keyLength, keyContext, (chars, state) =>
            {
                var (keyPrefix, clientKey, keyPostfix) = state;
                var pos = 0;
                keyPrefix.AsSpan().CopyTo(chars);
                pos += keyPrefix.Length;
                clientKey.AsSpan().CopyTo(chars[pos..]);
                pos += clientKey.Length;
                keyPostfix.AsSpan().CopyTo(chars[pos..]);
            });

            var date = dateTime.UtcNow;
            //investigate race conditions
            if (memoryCache.TryGetValue(key, out CircularBufferCounter data))
            {
                if (data.Count(date.Subtract(limitSettings.LimitPeriod), limitSettings.LimitPeriod) >= limitSettings.RequestLimit - 1)
                {
                    await limitSettings.LimitBreakHandler(httpContext);
                    return;
                }

                data.Increment(date);
            }
            else
            {
                var options = new MemoryCacheEntryOptions().SetSlidingExpiration(limitSettings.LimitPeriod);
                var cbc = new CircularBufferCounter(date, limitSettings.LimitPeriod / 10, limitSettings.LimitPeriod);
                memoryCache.Set(key, cbc, options);
            }

            await _next(httpContext);
        }

        private async Task<LimitRule> GetLimitSettings(HttpContext httpContext)
        {
            if (_configuration.LimitRules != null)
            {
                foreach (var settings in _configuration.LimitRules)
                {
                    if (await settings.ConditionPredicate(httpContext))
                    {
                        return settings;
                    }
                }
            }

            return _configuration.GlobalLimitRule;
        }
    }
}
