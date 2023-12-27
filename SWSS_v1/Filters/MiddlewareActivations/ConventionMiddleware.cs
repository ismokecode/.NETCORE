using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using System;

namespace SWSS_v1.Filters.MiddlewareActivations
{
    /*
    There are 3 ways to create middleware in ASP.NET Core:

        Using request delegates
        By convention --old approch new approch using IExceptionHandler
        IMiddleware
    */

    //eg. convention-based approach approach requires you to define an InvokeAsync method.
    public class ConventionalMiddleware
    {
        private readonly RequestDelegate _next;

        public ConventionalMiddleware(RequestDelegate next)
            => _next = next;

        public async Task InvokeAsync(HttpContext context)
        {
            var keyValue = context.Request.Query["key"];

            if (!string.IsNullOrWhiteSpace(keyValue))
            {

            }
            await _next(context);
        }
    }
}