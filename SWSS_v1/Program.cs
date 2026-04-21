using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;
using SWSS_v1.Filters.MiddlewareActivations;
using SWSS_v1.Filters.MiddlewareExtensibles;
using SWSS_v1.Services;
using SWSS_v1.UnitOfBox;
using SWSS_v1.Utility;
using System.Text.Json;
using System.Text.Json.Serialization;

//Returns WebApplicationBuilder class
var builder = WebApplication.CreateBuilder(args);

//.Configure<TOption> so send JsonSerializerOptions
//Tips .Add(JsonConverter Item<T>) object so send new ObjectCycleConverter<Location>();
//Enable if needed any issue while DataSave and get of Customer & Location 

//builder.Services.Configure<JsonSerializerOptions>(options =>
//{
//    options.ReferenceHandler = ReferenceHandler.Preserve;
//    //options.Converters.Add(new ObjectCycleConverter<DSVWCSWAVE0430_Inbound>());
//    options.Converters.Add(new ObjectCycleConverter<Location>());
//    options.Converters.Add(new ObjectCycleConverter<Department>());
//});

//TO solve error

//Error: response status is 500 Issue to fetch Customer records having forign Key of LocatioID
/*
 * {
  "message": "A possible object cycle was detected. This can either be due to a cycle or if the object depth is 
larger than the maximum allowed depth of 32. Consider using ReferenceHandler.Preserve on JsonSerializerOptions to 
support cycles. 
Path: $._results.Location.Customers.Location.Customers.Location.Customers.Location.Customers.Location.Customers.Location.Customers.Location.Customers.Location.Customers.Location.Customers.Location.Customers."
}
 */

builder.Services.AddControllers()
              .AddJsonOptions(options =>
                  options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles);

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
//builder.Services.AddSwaggerGen();
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    MaxDepth = 6 // Fixed
};
//jsonString = JsonSerializer.Serialize(pkg, options);
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        }
    );
    option.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        }
    );
});
builder.Services.MyDependencyInjection();
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddNLog();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

//builder.Services.JWTConfigureServices()
//;
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
//builder.Services.AddDbContext<CustomDbContext>(options => options.UseSqlServer(connectionString));
//to resolve forign key issue between customer & location
builder.Services.AddDbContext<CustomDbContext>(options => {
    options.UseSqlServer(connectionString);
    //options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
});


//builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

//Add Identity which are going to use upcoming part, Second parameter base class responsible for user role
//Define class work with identity related table

//For  controller IdentiryUser 

/*Error 1: {
"message": "Unable to resolve service for type 'Microsoft.AspNetCore.Identity.UserManager`1" +
    "[SWSS_v1.Models.ApplicationUser]' while attempting to activate 'SWSS_v1.Controllers.AppController'."
}
//builder.Services.AddIdentity<IdentityUser, IdentityRole>()
.AddEntityFrameworkStores<CustomDbContext>()
.AddDefaultTokenProviders(); */

//Solution bcoz we are using ApplicationUser so registered service for ApplicationUser with IdentityRole
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<CustomDbContext>()
    .AddDefaultTokenProviders();

//When a method like GetAllClasses returns objects with bidirectional relationships 
//(e.g., a Class containing a list of Students, where each Student also references that same Class),
//standard JSON serializers like System.Text.Json or Newtonsoft.Json will fail with a JsonException or
//StackOverflowException because they enter an infinite loop trying to resolve these references. 
builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });


//Run command#1 Add-Migration IdentityTablesAdded generate identity table using code first approch 
//Error then Install Microsoft.EntityFrameworkCore.Tools (use latest versions)
//Run command#2 update-database
#endregion

#region Authentication Filter
//builder.Services.AddSingleton<FactoryMiddleware>();
#endregion

APIAssembly.GetAssemblies();
#region use either one method APIAssembly.GetAssemblies(); or DiscoverModules & RegisterApis
//this method will get all instance of a class which implemented IMapEndPoints in current assembly and add into list

//APIAssembly.GetAssemblies();


///this method will return all the instance of a class which implemented IMapEndPoints in current assembly

//var modules = APIAssembly.DiscoverModules(AppDomain.CurrentDomain.GetAssemblies());
//builder.RegisterApis(modules);
#endregion

//Tips builder.Services.AddCors(Action means obj=>{use the obj} then obj.Policy(Action means obj2=>{ use the obj2}
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
                      builder =>
                      {
                          builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();                     
                      });
});

#region Jwt token configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey
        (Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false,
        ValidateIssuerSigningKey = true
    };
});
builder.Services.AddAuthorization();
#endregion Jwt token configuration

//builder.Services.AddTransient<IUnitOfWork, UnitOfWork>();

//Return WebApplication class
var app = builder.Build();
#region CORS
//app.UseCors(builder => builder
//               .AllowAnyHeader()
//               .AllowAnyMethod()
//               .SetIsOriginAllowed((host) => true)
//               .AllowCredentials()
//           );
#endregion
#region dbInitializer
//AppDbInitializer.SeedRolesToDb(app).Wait();
#endregion

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
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



#region middleware 

app.UseStaticFiles();
app.MapEndpoints();
app.UseCors(MyAllowSpecificOrigins);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
//app.UseMiddleware<FactoryMiddleware>();

#region exceptionHandling
//app.ErrorHandler(); // directly used by extension method of IApplicationBuilder
app.UseMiddleware<ExceptionMiddleware>();
#endregion
//terminator middleware
app.Run();// terminate middleware
#endregion Middleware

//Before Tools > NPM > Package Manager Console > Update-Database 