using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System.Web.Http.ExceptionHandling;

namespace SWSS_v1.Filters.MiddlewareActivations
{

    public class FactoryMiddleware : IMiddleware
    {
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var keyValue = context.Request.Query["key"];

            if (!string.IsNullOrWhiteSpace(keyValue))
            {

            }
            await next(context);
        }

    }
}
