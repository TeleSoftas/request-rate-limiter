using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RequestRateLimiter.Tests
{
    public class FakeIpMiddleware
    {
        private readonly RequestDelegate _next;
        
        public FakeIpMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            context.Connection.RemoteIpAddress = new IPAddress(192001);
            await _next.Invoke(context);
        }
    }
}
