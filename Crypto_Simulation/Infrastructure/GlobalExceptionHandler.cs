using Crypto_Simulation.DataContext.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Crypto_Simulation.Infrastructure
{
    /// <summary>
    /// Turns domain exceptions into RFC 7807 problem responses. Controllers used to wrap every
    /// call in try/catch and map anything at all to 404 or 400, which leaked internal messages
    /// and returned misleading status codes.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly IProblemDetailsService _problemDetailsService;
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(
            IProblemDetailsService problemDetailsService,
            ILogger<GlobalExceptionHandler> logger)
        {
            _problemDetailsService = problemDetailsService;
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var (status, title, detail) = Map(exception);

            if (status >= StatusCodes.Status500InternalServerError)
            {
                _logger.LogError(exception, "Unhandled exception while processing {Path}.", httpContext.Request.Path);
            }
            else
            {
                _logger.LogInformation("Request to {Path} rejected: {Message}", httpContext.Request.Path, exception.Message);
            }

            httpContext.Response.StatusCode = status;

            return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Instance = httpContext.Request.Path
                }
            });
        }

        private static (int Status, string Title, string Detail) Map(Exception exception) => exception switch
        {
            NotFoundException ex =>
                (StatusCodes.Status404NotFound, "Resource not found", ex.Message),
            ValidationException ex =>
                (StatusCodes.Status400BadRequest, "Invalid request", ex.Message),
            ConflictException ex =>
                (StatusCodes.Status409Conflict, "Conflict", ex.Message),
            ForbiddenException ex =>
                (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            _ =>
                // Never echo the message of an unexpected exception back to the caller.
                (StatusCodes.Status500InternalServerError, "An unexpected error occurred",
                 "The request could not be completed. Please try again later.")
        };
    }
}
