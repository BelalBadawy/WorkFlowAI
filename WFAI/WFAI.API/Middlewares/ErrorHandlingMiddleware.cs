
using System.Net;
using System.Text.Json;
using WFAI.Application.Dtos.Wrappers;

namespace WFAI.API
{
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;
        private readonly IWebHostEnvironment _env;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger, IWebHostEnvironment env)
        {
            _next = next;
            _logger = logger;
            _env = env;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred.");

                if (context.Response.HasStarted)
                {
                    _logger.LogWarning(
                        "The response has already started, the error handling middleware will not modify the response.");
                    return;
                }

                await HandleExceptionAsync(context, ex, _env.IsDevelopment());
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception ex, bool isDevelopment)
        {
            context.Response.Clear();

            context.Response.StatusCode = ex switch
            {
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                KeyNotFoundException => (int)HttpStatusCode.NotFound,
                InvalidOperationException => (int)HttpStatusCode.BadRequest,
                TimeoutException => (int)HttpStatusCode.RequestTimeout,
                ArgumentException => (int)HttpStatusCode.BadRequest,
                _ => (int)HttpStatusCode.InternalServerError
            };

            context.Response.ContentType = "application/json";

            var message = isDevelopment ? ex.Message : "An unexpected error occurred. Please try again later.";
            var responseWrapper = ResponseWrapper.Fail(message, context.Response.StatusCode);
            var result = JsonSerializer.Serialize(responseWrapper);

            await context.Response.WriteAsync(result);
        }
    }
}