using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RequestRateLimiter.Core.Configuration
{
    public class LimitRule
    {
        /// <value>
        /// Represents a request count limit.
        /// </value>
        public int RequestLimit { get; set; }
        /// <value>
        /// Represents a request limit period.
        /// Example:
        /// <example>
        ///     <c>LimitPeriod = TimeSpan.FromMinutes(5);</c> would set the limit to <see cref="RequestLimit"/> per 5 minutes.
        /// </example>
        /// </value>
        public TimeSpan LimitPeriod { get; set; }
        /// <value>
        /// Represents a route for which the rule is set.
        /// </value>
        public string Route { get; set; }
        /// <value>
        /// Represents a function that evaluates if the request satsifies the given conditions. By default it checks if the <see cref="HttpRequest.Path"/> starts with <see cref="Route"/>.
        /// </value>
        public Func<HttpContext, Task<bool>> ConditionPredicate { get; set; }
        /// <value>
        /// Represents a function that gets invoke when a limit is broken. By default it writes a response with HTTP status code 429 and a message "Too many requests.".
        /// </value>
        public Func<HttpContext, Task> LimitBreakHandler { get; set; }

        public LimitRule()
        {
            ConditionPredicate = DefaultRequestCheck;
            LimitBreakHandler = DefaultHandleLimitBreak;
        }

        /// <summary>
        /// Creates a <see cref="LimitRule"/> object.
        /// </summary>
        /// <param name="limitPeriod">Represents a time period for which the rate limit is being measured.</param>
        /// <param name="requestLimit">Represents the maximum allowed number of requests for the specified time period before handling a limit break.</param>
        /// <param name="route">Represents a route for which the rule will be applied to.</param>
        /// <param name="contextPreidcate">Represents a function for evaluating if the rule should be applied to the request.</param>
        /// <param name="limitBreakHandler">Represents a function for handling limit breaks for this specific rule.</param>
        public LimitRule(TimeSpan limitPeriod, int requestLimit, string route = null, Func<HttpContext, Task<bool>> contextPreidcate = null, Func<HttpContext, Task> limitBreakHandler = null)
        {
            LimitPeriod = limitPeriod;
            RequestLimit = requestLimit;
            Route = route;
            ConditionPredicate = contextPreidcate ?? DefaultRequestCheck;
            LimitBreakHandler = limitBreakHandler ?? DefaultHandleLimitBreak;
        }

        private Task<bool> DefaultRequestCheck(HttpContext context)
        {
            return Task.FromResult(Route?.StartsWith(PathHelper.SanitizePath(context.Request.Path)) ?? false);
        }
        
        private static async Task DefaultHandleLimitBreak(HttpContext context)
        {
            context.Response.StatusCode = 429;
            await context.Response.WriteAsync("Too many requests.");
        }
    }
}
