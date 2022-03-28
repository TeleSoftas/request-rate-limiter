using System;

namespace RequestRateLimiter.Core.Helpers
{
    public interface IDateTime
    {
        DateTime Now { get; }
        DateTime UtcNow { get; }
    }
}