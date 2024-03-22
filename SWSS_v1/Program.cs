using NLog.Extensions.Logging;
using SWSS_v1.Filters.MiddlewareActivations;
using SWSS_v1.Filters.MiddlewareExtensibles;
using SWSS_v1.Services;

//Returns WebApplicationBuilder class
var builder = WebApplication.CreateBuilder(args);

//var connectionString = "Data Source = DESKTOP-9H0UC46; Initial Catalog=SWSS; User Id=ss; password=12345678; TrustServerCertificate=True";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
//builder.Configuration.GetSection("");

#region services
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

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddNLog();


//builder.Services.JWTConfigureServices();
//builder.Services.AddAuthorization();
//builder.Services.AddAuthentication();
#endregion

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
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
   .AddJwtBearer(options =>
   {
       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidateAudience = true,
           ValidateLifetime = true,
           ValidateIssuerSigningKey = true,
           ValidIssuer = "Test.com",
           ValidAudience = "Test.com",
           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisismySecretKey"))
       };
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

//Return WebApplication class
var app = builder.Build();

// Configure the HTTP request pipeline.
if(app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
#region redirection
//When a web browser attempts to open a URL that has been redirected, a page with a different URL is opened.
//The UseHttpsRedirection() method invocation enables the HTTPS redirection middleware.
//This means that each request to your application will be inspected and possibly
//redirected by the middleware. You don't need to look for pages missing the RequireHttps attribute. All the pages of your application will require HTTPS.
app.UseHttpsRedirection();
#endregion

#region dbInitializer
AppDbInitializer.SeedRolesToDb(app).Wait();
#endregion

#region middleware 

app.UseStaticFiles();
app.MapEndpoints();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.UseMiddleware<FactoryMiddleware>();

#region exceptionHandling
app.ErrorHandler(); // directly used by extension method of IApplicationBuilder
app.UseMiddleware<ExceptionMiddleware>();
#endregion

app.Run();
#endregion Middleware

//Save roles to db 
//Before Tools > NPM > Package Manager Console > Update-Database 

