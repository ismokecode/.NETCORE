using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols;
using NLog.Extensions.Logging;
using SWSS_v1.Filters.MiddlewareActivations;
using SWSS_v1.Filters.MiddlewareExtensibles;
//using SWSS_v1.Filters.ExceptionHandlings;
using SWSS_v1.Services;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddNLog();

//var connectionString = "Data Source = DESKTOP-9H0UC46; Initial Catalog=SWSS; User Id=ss; password=12345678; TrustServerCertificate=True";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
/*
When we removed below AddEndPointsApiExplorer and run our application,
 we only see the /WeatherForecast endpoint in the Swagger UI documentation.

 /WeatherForecast - On the other hand, the controller endpoint is displayed 
 because the AddControllers() method calls AddApiExplorer() which only registers 
 services to discover controller endpoints and not Minimal API endpoints.

 However, if we remove both AddControllers() and AddEndpointsApiExplorer() 
 calls from our service registration, we get an error running the application. 
 This is because after removing the method calls,
  registration of services required by Swagger does not happen.
*/
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.MyDependencyInjection();
builder.Services.AddScoped<IEmployee, EmployeeService>();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

#region EFConnection and Identity and Roles
var tokenValidationParameters = new TokenValidationParameters()
{
    ValidateIssuerSigningKey = true,
    //sign key to check validate token
    //IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(Configuration["Jwt2:Secret"]))
    IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("This-is-token-key")),
    ValidateIssuer = true,
    //come from configuration
    //ValidIssuer = Configuration["Jwt2:Issuer"];
    ValidIssuer = "Issuer",
    ValidateAudience = true,
    ValidAudience = "test.com",
    ValidateLifetime = true,
    //set ClockSkew value to Zero because we have set the token expiration time 1 and by default token expiration time 10 mins
    ClockSkew = TimeSpan.Zero
};
builder.Services.AddSingleton(tokenValidationParameters);
builder.Services.AddDbContext<AppDBContext>(options => options.UseSqlServer(connectionString));

//Add Identity which are going to use upcoming part, Second parameter base class responsible for user role
//Define class work with identity related table
builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDBContext>()
    .AddDefaultTokenProviders();


//Run command#1 Add-Migration IdentityTablesAdded generate identity table using code first approch 
//Error then Install Microsoft.EntityFrameworkCore.Tools (use latest versions)
//Run command#2 update-database
#endregion

#region Authentication Filter
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
    //Add JwtBearer inside here token will validation code
    .AddJwtBearer(options => {
        options.SaveToken = true;
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = tokenValidationParameters;
    });
builder.Services.AddSingleton<FactoryMiddleware>();

#endregion

APIAssembly.GetAssemblies();
#region use either one method APIAssembly.GetAssemblies(); or DiscoverModules & RegisterApis
//this method will get all instance of a class which implemented IMapEndPoints in current assembly and add into list

//APIAssembly.GetAssemblies();


///this method will return all the instance of a class which implemented IMapEndPoints in current assembly

//var modules = APIAssembly.DiscoverModules(AppDomain.CurrentDomain.GetAssemblies());
//builder.RegisterApis(modules);
#endregion

#region JWT Auth
//builder.Services.JWTConfigureServices();
//builder.Services.AddAuthorization();
//builder.Services.AddAuthentication();
#endregion


var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

#region <<<exceptionHandling>>>
    //1st old way conventional middleware
    //app.UseMiddleware<ConventionMiddleware>();

    //second way
    //builder.Services.AddTransient<FactoryMiddleware>();
    //app.UseMiddleware<FactoryMiddleware>();

    //3rd new way
    //builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    //builder.Services.AddProblemDetails();

    //to add the exception handler in request pipeline
    //app.UseExceptionHandler();
#endregion  <<<exceptionHandling>>>

#region <<<dbInitializer>>>
AppDbInitializer.SeedRolesToDb(app).Wait();
#endregion

#region <<<Middleware>>>
   
app.UseStaticFiles();
app.MapEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
//app.RajeevMiddleWare();
app.UseConventionalMiddleware();
app.UseMiddleware<FactoryMiddleware>();
app.ErrorHandler();
//app.MapControllerRoute(name:"default",
//    pattern:"{controller}/{action}/{id?}");

app.Run();
#endregion Middleware

//Save roles to db 
//Before Tools > NPM > Package Manager Console > Update-Database 

