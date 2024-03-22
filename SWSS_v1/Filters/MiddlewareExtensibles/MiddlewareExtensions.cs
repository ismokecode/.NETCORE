using SWSS_v1.Filters.MiddlewareActivations;
using System.Runtime.CompilerServices;

namespace SWSS_v1.Filters.MiddlewareExtensibles
{
    public static class MiddlewareExtensions
    {
        //IApplicationBuilder class provides a mechanism to configure middleware
        //in request pipeline

        //UseMiddleware > Extenden method
        public static IApplicationBuilder ErrorHandler(this IApplicationBuilder applicationBuilder)
            => applicationBuilder.UseMiddleware<ExceptionMiddleware>();
    }
   
    public interface IABC
    {
        public IABC abc();
    }
    public static class ABCE
    {
        public static IABC abcd(this IABC app)
        {
            return app.abc();
        }
    }
}