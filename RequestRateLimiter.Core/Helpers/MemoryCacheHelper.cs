using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace RequestRateLimiter.Core.Helpers
{
    internal sealed class MemoryCacheHelper : MemoryCache, IMemoryCacheHelper
    {
        public MemoryCacheHelper(IOptions<MemoryCacheOptions> optionsAccessor) : base(optionsAccessor)
        { }
    }
}
