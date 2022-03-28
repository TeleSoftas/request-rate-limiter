using Microsoft.Extensions.Caching.Memory;

namespace RequestRateLimiter.Core.Helpers
{
    public interface IMemoryCacheHelper : IMemoryCache
    {  }
}
