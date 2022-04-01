# RequestRateLimiter ![Nuget](https://img.shields.io/nuget/v/RequestRateLimiter.svg) ![Build](https://img.shields.io/github/workflow/status/TeleSoftas/request-rate-limiter/build-and-test)
This is an ASP.NET Core middleware that allows configuring request rate limitations on your web application.

Request limits can be configured per route and/or per IP. 

Requests statistics is stored in-memory, keep this in mind when scaling the app.

This rate limiter is using a circular buffer to keep track of the request rate per configured period.


# Setup
The easiest way to set up this middleware is to use `ConfigureRequestRateLimiter()` in `ConfigureServices()` in `Startup.cs`.  
In the example below we configure the rate limiter to allow `20` requests per `1`day for every endpoint.

``` csharp
services.ConfigureRequestRateLimiter(config =>
            {
                config.GlobalLimitPeriod = TimeSpan.FromDays(1);
                config.GlobalRequestLimit = 20;
            });
  ```

Add `UseRequestRateLimiter()` in `Startup.cs Configure()` method to register the middleware. Pay attention to the order of middlewares in `Configure()`, RequestRateLimiter should be registered as early as possible to prevent execution of other middlewares when limits are reached.

``` csharp
app.UseRequestRateLimiter();
```

# Configuration
This middleware can be configured via `appsettings.json` and/or with the `ConfigureRequestRateLimiter()` method.

## Configuring with `appsettings.json`
To configure the rate limiter via `appsettings.json` use the following:
``` csharp
services.ConfigureRequestRateLimiter(IConfiguration);
```
Or if you want to also use the configuration from the code at the same time consider using the following:
``` csharp
services.ConfigureRequestRateLimiter(IConfiguration, Action<RequestRateLimiterConfiguration>);
```

`IsEnabled` - enables/disables the middleware.

`GlobalRequestLimit` - a maximum request count for every endpoint. When the configured number is exceeded, the middelware returns a configured response and prevents execution of further middlewares.

`GlobalLimitPeriod` - sets a timespan for checking request rates for every endpoint.

`LimitRules` - an array of rules for specific endpoints. A rule in this array has these parameters:
* `Route` - specifies the route for which the rule is being applied to.
* `RequestLimit` - a maximum request count per configured period for the specified endpoint. When the configured number is exceeded, the middelware returns a configured response and prevents execution of further middlewares.
* `LimitPeriod` - sets a timespan for checking request rates for the specified endpoint.

### Examples:
In the example below we have configured the middleware to:
* Limit `/some-route` to `3` requests per `10` seconds
* Limit `/some-other-route` to `15` request per `30` seconds
* Limit every other route to `10` requests per `1` minute
``` json
  "RequestRateLimiter": {
    "IsEnabled": true,
    "GlobalRequestLimit": 10,
    "GlobalLimitPeriod": "00:01:00",
    "LimitRules": [
      {
        "RequestLimit": 3,
        "LimitPeriod": "00:00:10",
        "Route": "/some-route"
      },
            {
        "RequestLimit": 15,
        "LimitPeriod": "00:00:30",
        "Route": "/some-other-route"
      }
    ]
  }
```

***

## Configuring with `ConfigureRequestRateLimiter()`
`ConfigureRequestRateLimiter()` method has several overloads for a more flexible configuration.

### Configure the middleware using `IConfiguration`:
``` csharp
services.ConfigureRequestRateLimiter(IConfiguration);
```

### Configure the middleware using `Action<RequestRateLimiterConfiguration>`:
``` csharp
services.ConfigureRequestRateLimiter(Action<RequestRateLimiterConfiguration>);
```

### Configure the middleware using both `IConfiguration` and `Action<RequestRateLimiterConfiguration>`:
``` csharp
services.ConfigureRequestRateLimiter(IConfiguration, Action<RequestRateLimiterConfiguration>);
```
### Using `Action<RequestRateLimiterConfiguration>`:
Using `Action<RequestRateLimiterConfiguration>` enables configuration in the code rather than using `appsettings.json`. It contains the same configurable properties with some additions:
* `bool IsEnabled` - enables/disables the middleware.
* `int GlobalRequestLimit` - a maximum request count for every endpoint. When the configured number is exceeded, the middelware returns a configured response and prevents execution of further middlewares.
* `TimeSpan GlobalLimitPeriod` - sets a timespan for checking request rates for every endpoint.
* `Func<HttpContext, Task> GlobalLimitBreakHandler` - a `Func` that gets executed whenever a client breaks any other rate limit. By default it sets the `HttpContext.Response.StatusCode` to `429` with the message `"Too many requests."`
* `public Func<HttpContext, Task<string>> GetClientKeyFunc` - a `Func` that returns a string indicating a unique client. By default the middleware takes `HttpContext.Connection.RemoteIpAddress` as the client key.
* `IList<LimitRule> LimitRules` - a list of rules for specific endpoints. A rule in this list contains these parameters:
    * `TimeSpan limiterTimeout` - sets a timespan for checking request rates for the specified endpoint.
    * `int maxRequests` - a maximum request count per configured period for the specified endpoint. When the configured number is exceeded, the middelware returns a configured response and prevents execution of further middlewares.
    * `string route` - specifies the route for which the rule is being applied to.
    * `Func<HttpContext, Task<bool>> contextPreidcate` - a `Func` that determines if the `HttpContext` matches the requirements. By default it checks if `HttpContext.Request.Path` matches the specified route.
    * `Func<HttpContext, Task> limitBreakHandler` - a `Func` that gets executed whenever a client breaks the specified routes rate limit. By default it sets the `HttpContext.Response.StatusCode` to `429` with the message `"Too many requests."`

## Examples:
## Setting up global settings:
___
### Overriding `RequestRateLimiterConfiguration.GetClientKeyFunc`:
``` csharp
services.ConfigureRequestRateLimiter(config =>
{
    config.GlobalRequestLimit = 5;
    config.GlobalLimitPeriod = TimeSpan.FromMinutes(10);
    config.GetClientKeyFunc = context =>
    {
        return Task.FromResult($"{context.Connection.RemoteIpAddress}-{context.Request.Headers["User-Agent"]}");
    };
});
```
The example above configures the middleware so that:
* A unique client key is considered to be a combination of client IP address and their user agent
* Each client is limited to `5` requests per `10` minutes
___
### Overriding `RequestRateLimiterConfiguration.GlobalLimitBreakHandler`:
``` csharp
services.ConfigureRequestRateLimiter(config =>
{
    config.GlobalRequestLimit = 10;
    config.GlobalLimitPeriod = TimeSpan.FromSeconds(50);
    config.GlobalLimitBreakHandler = context =>
    {
        context.Response.StatusCode = 400;
        return Task.CompletedTask;
    };
});
```
The example above configures the middleware so that:
* When the global rate limit is exceeded the client receives an HTTP status code `400`
* Each client is limited to `10` requests per `50` seconds
___
## Setting up `LimitRules`:
### Setting a rule for a route:
``` csharp
services.ConfigureRequestRateLimiter(config =>
{
    config.GlobalRequestLimit = 5;
    config.GlobalLimitPeriod = TimeSpan.FromSeconds(10);
    config.LimitRules = new List<LimitRule> 
    {
        new LimitRule(TimeSpan.FromSeconds(30), 2, "/some-route") 
    };
});
```
The example above configures the middleware so that:
* A client requesting `/some-route` will be limited to `2` requests per `30` seconds
* Requests to other endpoints will be limited to `5` requests per `10` seconds
___
### Overriding `LimitRule.LimitBreakHandler` for custom limit break handling:
``` csharp
services.ConfigureRequestRateLimiter(config =>
{
    Func<HttpContext, Task> limitBreakHandler = context =>
    {
        context.Response.StatusCode = 400;
        return Task.CompletedTask;
    };

    config.GlobalRequestLimit = 20;
    config.GlobalLimitPeriod = TimeSpan.FromHours(1);               
    config.LimitRules = new List<LimitRule> 
    {
        new LimitRule(TimeSpan.FromSeconds(30), 2, "/some-route", limitBreakHandler: limitBreakHandler) 
    };
});
```
The example above configures the middleware so that:
* A client requesting `/some-route` will be limited to `2` requests per `30` seconds
* If the client exceeds the rate limit for `/some-route` they will receive an HTTP status code `400`
* Requests to other endpoints will be limited to `5` requests per `10` seconds
___
### Overriding `LimitRule.ConditionPredicate` for custom request evaluation:
``` csharp
var conditionPredicate = new Func<HttpContext, Task<bool>> (context =>
{
    var headerValue = context.Request.Headers["test"].ToString();
    return Task.FromResult(headerValue != "testHeader");
});

services.ConfigureRequestRateLimiter(config =>
{
    config.GlobalLimitPeriod = TimeSpan.FromDays(1);
    config.GlobalRequestLimit = 20;
    config.LimitRules = new List<LimitRule>
    {
        new (TimeSpan.FromHours(1), 5, null, conditionPredicate)
    };
});
```
The example above configures the middleware so that:
* Any request with a HTTP header named `test` will be limited to `5` requests per `1` hour
* Requests to other routes are limited to `20` requests per `1` day per client key (IP by default)
