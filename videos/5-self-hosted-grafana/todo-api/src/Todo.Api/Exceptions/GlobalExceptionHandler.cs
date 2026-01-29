using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace Todo.Api.Exceptions;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger
) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        logger.LogError(exception, "An unhandled exception occurred while processing the request.");

        var activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Detail = $"An unexpected error occurred. Trace ID: {activity?.Id}",
                    Extensions = new Dictionary<string, object?>
                    {
                        { "requestId", httpContext.TraceIdentifier },
                        { "traceId", activity?.Id },
                    },
                },
            }
        );
    }
}
