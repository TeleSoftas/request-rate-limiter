using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using RequestRateLimiter.Core;
using RequestRateLimiter.Core.Configuration;
using RequestRateLimiter.Core.Helpers;
using Xunit;

namespace RequestRateLimiter.Tests
{
    public class RequestRateLimiterMiddlewareTests
    {

        [Fact]
        public async Task GlobalSettings_LimitReached_Returns429()
        {
            const int maxRequestCount = 4;
            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = maxRequestCount;
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < maxRequestCount + 1; i++)
                {
                    message = await client.GetAsync("/"); 
                }
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }

        [Fact]
        public async Task GlobalSettings_LimitNotReached_DoesNotReturn429()
        {
            const int maxRequestCount = 4;
            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = maxRequestCount;
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < maxRequestCount - 1; i++)
                {
                    message = await client.GetAsync("/");
                }
            }

            Assert.NotEqual(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }

        [Fact]
        public async Task GlobalSettings_LimitReachedLimitTimedOut_DoesNotReturn429()
        {
            const int maxRequestCount = 4;
            HttpResponseMessage message = null;

            var dateTimeMock = new Mock<IDateTime>();
            dateTimeMock.SetupSequence(x => x.UtcNow)
                .Returns(new DateTime(2021, 8, 5, 0, 0, 0))
                .Returns(new DateTime(2021, 8, 5, 0, 1, 0))
                .Returns(new DateTime(2021, 8, 5, 0, 2, 0))
                .Returns(new DateTime(2021, 8, 5, 0, 3, 0))
                .Returns(new DateTime(2021, 8, 5, 0, 4, 0))
                .Returns(new DateTime(2021, 8, 5, 0, 11, 30));

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromMinutes(10);
                config.GlobalRequestLimit = maxRequestCount;
            });

            using (var host = await GetHost(configAction, dateTimeMock.Object))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < maxRequestCount + 2; i++)
                {
                    message = await client.GetAsync("/");
                }
            }

            Assert.NotEqual(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }
        
        [Fact]
        public async Task GlobalSettings_CustomLimitAction_Returns400()
        {
            const int maxRequestCount = 4;
            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = maxRequestCount;
                config.GlobalLimitBreakHandler = context =>
                {
                    context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return Task.CompletedTask;
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < maxRequestCount + 1; i++)
                {
                    message = await client.GetAsync("/"); 
                }
            }

            Assert.Equal(HttpStatusCode.BadRequest, message?.StatusCode);
        }
        
        [Fact]
        public async Task GlobalSettings_CustomClientKey_DoesNotReturn429()
        {
            const int maxRequestCount = 4;
            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = maxRequestCount;
                config.GetClientKeyFunc = context =>
                {
                    var headerValue = context.Request.Headers["test"].ToString();
                    return Task.FromResult($"{context.Connection.RemoteIpAddress}-{headerValue}");
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < maxRequestCount; i++)
                {
                    client.DefaultRequestHeaders.Add("test", "header-1");
                    message = await client.GetAsync("/"); 
                }
                
                client = host.GetTestClient();
                for (var i = 0; i < maxRequestCount; i++)
                {
                    client.DefaultRequestHeaders.Add("test", "header-2");
                    message = await client.GetAsync("/"); 
                }
            }

            Assert.NotEqual(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }

        [Fact]
        public async Task RouteSettings_LimitNotReached_DoesNotReturn429()
        {
            const int globalMaxRequestCount = 4;
            const int routeMaxRequestCount = 8;
            const string route = "/dummy";

            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = globalMaxRequestCount;
                config.LimitRules = new List<LimitRule>
                {
                    new (TimeSpan.FromDays(1), routeMaxRequestCount, route)
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < globalMaxRequestCount + 1; i++)
                {
                    message = await client.GetAsync(route);
                }
            }

            Assert.NotEqual(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }

        [Fact]
        public async Task RouteSettings_LimitReached_Returns429()
        {
            const int globalMaxRequestCount = 8;
            const int routeMaxRequestCount = 4;
            const string route = "/dummy";

            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = globalMaxRequestCount;
                config.LimitRules = new List<LimitRule>
                {
                    new (TimeSpan.FromDays(1), routeMaxRequestCount, route)
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < routeMaxRequestCount + 1; i++)
                {
                    message = await client.GetAsync(route);
                }
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }
        
        [Fact]
        public async Task RouteSettings_CustomLimitAction_Returns429()
        {
            const int globalMaxRequestCount = 8;
            const int routeMaxRequestCount = 4;
            const string route = "/dummy";

            HttpResponseMessage message = null;

            var limitBreakAction = new Func<HttpContext, Task>(context =>
            {
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return Task.CompletedTask;
            });
            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = globalMaxRequestCount;
                config.LimitRules = new List<LimitRule>
                {
                    new (TimeSpan.FromDays(1), routeMaxRequestCount, route, limitBreakHandler: limitBreakAction)
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < routeMaxRequestCount + 1; i++)
                {
                    message = await client.GetAsync(route);
                }
            }

            Assert.Equal(HttpStatusCode.BadRequest, message?.StatusCode);
        }

        [Fact]
        public async Task RouteSettings_LimitDisabled_DoesNotReturn429()
        {
            const int globalMaxRequestCount = 3;
            const string route = "/dummy";

            HttpResponseMessage message = null;

            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = globalMaxRequestCount;
                config.LimitRules = new List<LimitRule>
                {
                    new (TimeSpan.Zero, 0, route)
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                for (var i = 0; i < globalMaxRequestCount + 1; i++)
                {
                    message = await client.GetAsync(route);
                }
            }

            Assert.NotEqual(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }
        
        [Fact]
        public async Task RouteSettings_CustomRequestCheck_RequestDoesNotMatch_DoesNotReturn429()
        {
            const int globalMaxRequestCount = 4;
            const int maxRequestCount = 3;

            HttpResponseMessage message = null;

            var contextMatchAction = new Func<HttpContext, Task<bool>> (context =>
            {
                var headerValue = context.Request.Headers["test"].ToString();
                return Task.FromResult(headerValue != "testHeader");
            });
            
            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = globalMaxRequestCount;
                config.LimitRules = new List<LimitRule>
                {
                    new (TimeSpan.FromDays(1), maxRequestCount, null, contextMatchAction)
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                client.DefaultRequestHeaders.Add("test", "testHeader");
                for (var i = 0; i < maxRequestCount + 1; i++)
                {
                    message = await client.GetAsync("/"); 
                }
            }

            Assert.NotEqual(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }
        
        [Fact]
        public async Task RouteSettings_CustomRequestCheck_RequestMatches_Returns429()
        {
            const int globalMaxRequestCount = 3;
            const int maxRequestCount = 4;
            HttpResponseMessage message = null;

            var contextMatchAction = new Func<HttpContext, Task<bool>> (context =>
            {
                var headerValue = context.Request.Headers["test"].ToString();
                return Task.FromResult(headerValue == "testHeader");
            });
            
            var configAction = new Action<RequestRateLimiterConfiguration>(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = globalMaxRequestCount;
                config.LimitRules = new List<LimitRule>
                {
                    new (TimeSpan.FromDays(1), maxRequestCount, null, contextMatchAction)
                };
            });

            using (var host = await GetHost(configAction))
            {
                var client = host.GetTestClient();
                client.DefaultRequestHeaders.Add("test", "testHeader");

                for (var i = 0; i < maxRequestCount + 1; i++)
                {
                    message = await client.GetAsync("/"); 
                }
            }

            Assert.Equal(HttpStatusCode.TooManyRequests, message?.StatusCode);
        }

        private static Task<IHost> GetHost(Action<RequestRateLimiterConfiguration> configure, IDateTime dateTime = null)
        {
            dateTime ??= new DateTimeHelper();

            return new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder
                        .UseTestServer()
                        .ConfigureServices(services =>
                        {
                            services.ConfigureRequestRateLimiter(configure);
                            services.AddSingleton(dateTime);
                        })
                        .Configure(app =>
                        {
                            app.UseMiddleware<FakeIpMiddleware>();
                            app.UseRequestRateLimiter();
                        });
                })
                .StartAsync();
        }
    }
}
