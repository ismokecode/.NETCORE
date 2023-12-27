using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Reflection.Metadata;
using System;
using System.Web.Http.ExceptionHandling;

namespace SWSS_v1.Filters.MiddlewareActivations
{
    /* NEW WAY TO HANDLE EXCEPTION
     * https://www.milanjovanovic.tech/blog/global-error-handling-in-aspnetcore-8#new-way-iexceptionhandler
    ASP.NET Core 8 introduces a new IExceptionHandler abstraction for managing 
    exceptions. The built -in exception handler middleware uses 
    IExceptionHandler implementations to handle exceptions.

    This interface has only one TryHandleAsync method.

    TryHandleAsync attempts to handle the specified exception within
    the ASP.NET Core pipeline. If the exception can be handled, 
    it should return true. If the exception can't be handled, 
    it should return false. This allows you to implement custom 
    exception-handling logic for different scenarios.

    Service config>

    builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    builder.Services.AddProblemDetails();
    app.UseExceptionHandler();

    --You call the AddExceptionHandler method to register the GlobalExceptionHandler as a service. It's registered with a singleton lifetime. So be careful about injecting services with a different lifetime.

    --I'm also calling AddProblemDetails to generate a Problem Details response for common exceptions.
   

     */
    internal sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public Task HandleAsync(ExceptionHandlerContext context, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(
                exception, "Exception occurred: {Message}", exception.Message);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Server error"
            };

            httpContext.Response.StatusCode = problemDetails.Status.Value;

            await httpContext.Response
                .WriteAsJsonAsync(problemDetails, cancellationToken);
            return true;
        }
    }
}

/*Syntax
 internal sealed class GlobalExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        // Handle the exception, log errors.

        return true;
    }
}
 */

