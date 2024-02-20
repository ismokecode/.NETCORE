using SWSS_v1.Filters.MiddlewareActivations;
using System.Runtime.CompilerServices;

namespace SWSS_v1.Filters.MiddlewareExtensibles
{
    public static class MiddlewareExtensions
    {
        //IApplicationBuilder class provides a mechanism to configure middleware in request pipeline
        public static IApplicationBuilder ErrorHandler(this IApplicationBuilder applicationBuilder)
            => applicationBuilder.UseMiddleware<ExceptionMiddleware>();
        public static IApplicationBuilder UseConventionalMiddleware(
      this IApplicationBuilder app)
      => app.UseMiddleware<ConventionalMiddleware>();        

        /*Today, when you use the IMiddleware interface, you also have to add it
        to the dependency injection container as a service:
        services.AddTransient<FactoryMiddleware>();*/

        //public static IApplicationBuilder UseFactoryActivatedMiddleware(
        //    this IApplicationBuilder app)
        //    => app.UseMiddleware<FactoryMiddleware>();
    }
}
