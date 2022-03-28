using System;

namespace RequestRateLimiter.Core.Helpers
{
    public class DateTimeHelper : IDateTime
    {
        public DateTime Now => DateTime.Now;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
