using System.Net;
using System.Text.Json;
using SWSS_v1.Filters.Exceptions;

namespace SWSS_v1.Filters.MiddlewareActivations
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        public ExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }
        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await ExceptionAsync(context, ex);
            }
        }
        public static Task ExceptionAsync(HttpContext context, Exception ex)
        {
            HttpStatusCode statusCode;
            string message = "Unexpected error";
            var excpType = ex.GetType();
            if (excpType == typeof(BadRequestException))
            {
                statusCode = HttpStatusCode.BadRequest;
                message = ex.Message;
            }
            else if (excpType == typeof(NotFoundException))
            {
                statusCode = HttpStatusCode.NotFound;
                message = ex.Message;
            }
            else if (excpType == typeof(NotImplementedExceptions))
            {
                statusCode = HttpStatusCode.NotImplemented;
                message = ex.Message;
            }
            else if (excpType == typeof(UnauthorizedException))
            {
                statusCode = HttpStatusCode.Unauthorized;
                message = ex.Message;
            }
            else
            {
                statusCode = HttpStatusCode.InternalServerError;
                message = ex.Message;
            }
            var result = JsonSerializer.Serialize(new { message });
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)statusCode;
            return context.Response.WriteAsync(result);
        }
    }
}
