If token is expired so it should be generate refresh token.

GenerateToken - JwtToken (change)+ Refresh Token(won't change)
JwtTokenExpired - ExpireToken + RefreshToken = Generate New Refresh Token

Microsoft.EntityFrameworkCore.SqlServer;
Microsoft.EntityFrameworkCore.Tools;
Microsoft.AspNetCore.Identity.EntityFrameworkCore;
Microsoft.AspNetCore.Authentication.JwtBearer;

//Run command#1 Add-Migration IdentityTablesAdded generate identity table using code first approch 
//Error then Install Microsoft.EntityFrameworkCore.Tools (use latest versions)

//If needed Run command#1 add-migration InitialMigration
//Run command#2 update-database (Code first approch db has been created)

----Request pipeling middleware
Request > ExceptionHandler > HSTS > HTTPRedirection > StaticFiles > Routing > CORS > 
Authantication > Authorization > Custom Middleware 1 > Custom Middleware 2 > End Points
> Generate Response


//Sample data

{
  "firstName": "sonu",
  "lastName": "jha",
  "email": "sonu@gmail.com",
  "userName": "sonu",
  "password": "sonu@123",
  "userRole": "Admin",
  "phone": "8676968080",
  "pincode": "800008"
}

//
Sample Query
use swss

select * from dbo.AspNetUsers
select * from dbo.AspNetRoles

<<Middleware Registration>>
1. With Request Delegates
   app.Use(async(context,_next)=>{
    // add code before request
        await _next(context);
    //add code after middleware
   });

2. ConventionMiddleware
public class ConventionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger _logger;

    public ConventionMiddleware(
        RequestDelegate next,
        ILogger logger)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Before request");

        await next(context);

        _logger.LogInformation("After request");
    }
}
app.UseMiddleware<ConventionMiddleware>();

3.
public class FactoryMiddleware : IMiddleware
{
    private readonly ILogger _logger;

    public FactoryMiddleware(ILogger logger)
    {
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        RequestDelegate next)
    {
        _logger.LogInformation("Before request");

        await next(context);

        _logger.LogInformation("After request");
    }
}
builder.Services.AddTransient<FactoryMiddleware>();
app.UseMiddleware<FactoryMiddleware>();


>ASP.NET CORE LIFECYCLE: LIFECYCLE is sequence of events,stages or components that interact to each
others to process HTTP request that goes to the clients.
Stages:
    1. HTTP Request first it will go to middleware, its a sequence of middleware.
    2. Routing middleware decides how the incoming HTTP request mapped to controller and action 
        methods with the help of conventional routes and attributr routes.
    3. Controller initialization responsible to handle the incoming HTTP requests
        controller selects the appropriate action methods based on raouting.
    4. After action method execution - after the controller are initialized 
        action method executed which returns view or data result.
        that finally client receive response.

> Sequence of Middleware in ASP.NET CORE:
    1. app.UseExceptionHandler();
        2. app.UseHsts();
            3.app.UseHttpsRedirection();
                4. app.UseStaticFiles();
                    5. app.UseCookiePolicy();
                        6. app.UseRouting();
                            7. app.UseCors();
                                8.app.UseAuthentication()
                                    9.app.UseAuthorization()
                                        10.app.UseSession();
                                            11. app.UseMiddleware<CustomMiddleware>
                                                12.app.UseEndPoints()

>ASP.NET CORE DEPENDENCY INJECTION
NOTE: We try to return same method value through controller and partial page into index page
       and try to see the differecnce.
    1. Singelton : Service remains exactly the same through lifetime of application.
                    Uses: Caching Services, Global configuration which won't change which
                    might be business rules.
    2. Scoped: First partial and controller value are same then next time partial change and leads to 
                change controller value.
    3. Transient: New object created  everytimes Partial and controller value will changes everytimes.  

Ex. 1
public interface IService
{
    string GetGuid();
}
interface ITransientService:IService
{
}
public class TransientService:ITransientService
{
    private string _guid;
    public TransientService()
    {
        _guid=Guid.NewGuid().ToString();
    }
    public string GetGuid()
    {
        return _guid;
    }
}
2.builder.Services.Transient(ITransientService,TransientService)  
3. Inject service to HomeController: return View("Scoped",_scoped.GetGuid); so here returning value
    to controller to view and from partial view to compare both value
4.How to inject service into the partial view: @inject ITransientService Transient use > @Transient.GetGuid();

5. Index page looks like:
    <h1></h1>
    @Partial4
6. Partial view looks like
    @inject ITransientService Transient
    @Transient.GetGuid();
    
>>JWT Authentication https://www.infoworld.com/article/3669188/how-to-implement-jwt-authentication-in-aspnet-core.html

>>login test data
{
  "firstName": "string",
  "lastName": "string",
  "email": "chotu@gmail.com",
  "userName": "ch",
  "password": "Abcd@1234",
  "dateOfJoing": "2024-03-21T15:31:17.878Z"
}

>>https://www.infoworld.com/article/3692811/how-to-use-the-unit-of-work-pattern-in-aspnet-core.html
**Notes:
    1.Base class always returns and assign left side.
    2. Method overriding vs hidding
        base b = new Derived()
        b.Dis(); // it overriding call the derived class method
        base b = new Derived() // method hiding call the base class method