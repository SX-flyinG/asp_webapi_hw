using System.Diagnostics;
using System.Text.Json;
using asp_webapi_hw.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace asp_webapi_hw.Middleware;

public static class ExceptionHandler
{
    public static RequestDelegate Handle() =>
        async context =>
        {
            var feature   = context.Features.Get<IExceptionHandlerFeature>();
            var exception = feature?.Error;

            var (status, title) = exception switch
            {
                ArgumentException        => (400, "Bad Request"),
                ConflictException        => (409, "Conflict"),
                UnauthorizedAppException => (401, "Unauthorized"),
                KeyNotFoundException     => (404, "Not Found"),
                _                        => (500, "Internal Server Error")
            };

            var problem = new ProblemDetails
            {
                Type     = $"https://httpstatuses.com/{status}",
                Title    = title,
                Status   = status,
                Detail   = exception?.Message,
                Instance = context.Request.Path
            };

            problem.Extensions["traceId"] =
                Activity.Current?.Id ?? context.TraceIdentifier;

            context.Response.StatusCode  = status;
            context.Response.ContentType = "application/problem+json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(problem,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    }));
        };
}
