using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RequestRateLimiter.Core.Configuration
{
    public class RequestRateLimiterConfiguration
    {
        /// <value>
        /// Represents whether the middleware is enabled.
        /// </value>
        public bool IsEnabled { get; set; } = true;
        /// <value>
        /// Represents a request count limit for routes that do not match any rule in <see cref="LimitRules"/>.
        /// </value>
        public int GlobalRequestLimit { get; set; }
        /// <value>
        /// Represents a time span for when the request rate is being calculated each route that does not match any rule in <see cref="LimitRules"/>.
        /// <example>For example: 
        /// <code>GlobalLimiterTimeout = TimeSpan.FromMinutes(5)</code>
        /// would indicate that request rate limit is the value of <see cref="GlobalRequestLimit"/> per 5 minutes.
        /// </example>
        /// </value>
        public TimeSpan GlobalLimitPeriod { get; set; }
        /// <value>
        /// Represents a collection of request rate limit rules.
        /// </value>
        public IList<LimitRule> LimitRules { get; set; } = new List<LimitRule>();
        /// <value>
        /// Represents a function to be invoked when the global rate limit has been exceeded.
        /// </value>
        public Func<HttpContext, Task> GlobalLimitBreakHandler { get; set; }
        
        /// <summary>
        ///Represents a function for retrieving a key that would identify a unique client. By default, the client key is <c>HttpContext.Connection.RemoteIpAddress</c>.
        /// </summary>
        public Func<HttpContext, Task<string>> GetClientKeyFunc = httpContext => Task.FromResult(httpContext.Connection.RemoteIpAddress.ToString());

        internal LimitRule GlobalLimitRule => new LimitRule(GlobalLimitPeriod, GlobalRequestLimit, limitBreakHandler: GlobalLimitBreakHandler);
    }
}
