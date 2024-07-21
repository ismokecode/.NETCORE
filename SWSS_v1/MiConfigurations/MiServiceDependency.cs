

using SWSS_v1.Services;
using SWSS_v1.UnitOfBox;

public static class MiServiceDependency
{
    public static void MyDependencyInjection(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
    public static void JWTConfigureServices(this IServiceCollection services)
    {
        var config = new ConfigurationBuilder();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = "Test.com", //Configuration["Jwt:Issuer"],
                ValidAudience = "Test.com", //Configuration["Jwt:Issuer"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisismySecretKey"))//new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]))
            };
        });
    }
}


