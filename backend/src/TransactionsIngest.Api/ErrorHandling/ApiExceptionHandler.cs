using Microsoft.AspNetCore.Diagnostics;
using TransactionsIngest.Application.Import;

namespace TransactionsIngest.Api.ErrorHandling;

/// <summary>Maps exceptions to RFC 7807 problem details. Unknown errors never leak the message.</summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title, string? Detail) error = exception switch
        {
            ImportAlreadyRunningException e => (StatusCodes.Status409Conflict, "Import already running", e.Message),
            OperationCanceledException => (499, "Request cancelled", null),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", "Something went wrong while processing the request."),
        };

        if (error.Status >= 500)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogWarning("{Title} on {Method} {Path}", error.Title, httpContext.Request.Method, httpContext.Request.Path);

        httpContext.Response.StatusCode = error.Status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Status = error.Status,
                Title = error.Title,
                Detail = error.Detail,
            },
        });
    }
}
