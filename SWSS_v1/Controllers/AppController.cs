using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using SWSS_v1.Filters.Exceptions;
using SWSS_v1.API;
using Newtonsoft.Json.Linq;
using Azure;

namespace SWSS_v1.Controllers;

[ApiController]
//Attribute routing becomes a requirement for apicontroller. For example:
[Route("api/[controller]/[action]")]
public class AppController : ControllerBase
{
    private ILogger<AppController> _logger;
    //they're using MyController:ControllerBase
    private readonly UserManager<IdentityUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppDBContext _context;
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _tokenValidationParameters;
    public AppController(UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        AppDBContext context,
        IConfiguration configuration,
        TokenValidationParameters tokenValidationParameters,
        ILogger<AppController> logger
        )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
        _tokenValidationParameters = tokenValidationParameters;
        _logger = logger;
    }
    #region linkdin jwt user registration
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterVM registerVM)
    {
        List<Error> lstError = new List<Error>();
        try 
        {  
            _logger.LogInformation("Fetching all the Students from the storage");
            //check user exists
            var userExist = await _userManager.FindByEmailAsync(registerVM.Email);
            if (userExist != null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new APIResponse<string>(StatusCodes.Status409Conflict, null, null,null) { }); ;
            }

            //Add the user to db
            ApplicationUser user = new() {
                UserName = registerVM.UserName,
                Email = registerVM.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                Phone = registerVM.Phone,
                Pincode = registerVM.Pincode
                
            };
            if (await _roleManager.RoleExistsAsync(registerVM.UserRole))
            {
                var result = await _userManager.CreateAsync(user, registerVM.Password);
                if (result.Errors.Count() > 0)
                {
                    
                    foreach (var error in result.Errors)
                    {
                        lstError.Add(new Error { _error = error.Code, _description = error.Description });
                        return StatusCode(StatusCodes.Status403Forbidden,
                                new APIResponse<string>(StatusCodes.Status409Conflict, null, null, null) { }); ;
                    }
                }
                else if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, registerVM.UserRole);
                    StatusCode(StatusCodes.Status201Created,
                        new APIResponse<string>(StatusCodes.Status201Created,null,null, null) { });  
                }
            }
            else 
            {
                lstError.Add(new Error { _error = "", _description = "Role doesn't exist" });
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new APIResponse<string>(StatusCodes.Status500InternalServerError,null,null,null));
            }
            //assign role to user

        }
        catch(Exception ex)
        {
            return StatusCode(StatusCodes.Status500InternalServerError,
                    new APIResponse<string>(StatusCodes.Status500InternalServerError, null, null, null));
        }
        return StatusCode(StatusCodes.Status500InternalServerError,
                    new APIResponse<string>(StatusCodes.Status500InternalServerError, null, null, null));
    }
    [HttpPost]
    public async Task<APIResponse<string>>Login([FromBody] LoginVM loginVM)
    {
        //check validation of data
        if (loginVM.Password == null)
        {
            List<String> _lsts = new List<string>();
            _lsts = null;
            return new APIResponse<string>(StatusCodes.Status500InternalServerError, null, null, null);

        }
        var _userExists = await _userManager.FindByEmailAsync(loginVM.Email);
        if (_userExists != null && await _userManager.CheckPasswordAsync(_userExists, loginVM.Password))
        {
            var tokenString = GenerateJSONWebToken(loginVM);

            var response = Ok(tokenString);
            return new APIResponse<string>(StatusCodes.Status200OK, null, null, tokenString);
        }
        else 
        {
            return new APIResponse<string>(StatusCodes.Status500InternalServerError, null, null, null);
        }
    }
    private string GenerateJSONWebToken(LoginVM userInfo)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
          _configuration["Jwt:Issuer"],
          null,
          expires: DateTime.Now.AddMinutes(120),
          signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateJSONWebTokenClaimsAddedInfo(LoginVM userInfo)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[] {
        new Claim(JwtRegisteredClaimNames.Sub, userInfo.UserName),
        new Claim(JwtRegisteredClaimNames.Email, userInfo.Email),
        new Claim("DateOfJoing", userInfo.DateOfJoing.ToString("yyyy-MM-dd")),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

        var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
            _configuration["Jwt:Issuer"],
            claims,
            expires: DateTime.Now.AddMinutes(120),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    [HttpGet]
    [Authorize]
    public ActionResult<IEnumerable<string>> GetClaimsRelatedInfo()
    {
        var currentUser = HttpContext.User;
        int spendingTimeWithCompany = 0;

        if (currentUser.HasClaim(c => c.Type == "DateOfJoing"))
        {
            DateTime date = DateTime.Parse(currentUser.Claims.FirstOrDefault(c => c.Type == "DateOfJoing").Value);
            spendingTimeWithCompany = DateTime.Today.Year - date.Year;
        }

        if (spendingTimeWithCompany > 5)
        {
            return new string[] { "High Time1", "High Time2", "High Time3", "High Time4", "High Time5" };
        }
        else
        {
            return new string[] { "value1", "value2", "value3", "value4", "value5" };
        }
    }
    private LoginVM AuthenticateUser(LoginVM login)
    {
        LoginVM user = null;

        //Validate the User Credentials
        //Demo Purpose, I have Passed HardCoded User Information
        if (login.UserName == "Jignesh")
        {
            user = new LoginVM { UserName = "Jignesh Trivedi", Email = "test.btest@gmail.com" };
        }
        return user;
    }

    [HttpPost(Name = "GetEmployeeDetails")]
    public APIResponse<string> GetEmployeeDetails([FromServices] IEmployee service, Employee emp)
    {
        return new APIResponse<string>(StatusCodes.Status200OK, null,null, "Hello World");
    }
    #endregion
    #region ExceptionHandling
    [HttpGet]
    public IActionResult Get()
    {
        try
        {
            
            _logger.LogTrace("This is trace log");
            _logger.LogInformation("Fetching all the Students from the storage");
            _logger.LogInformation($"Returning {6} students.");
            throw new Exception("sdfdsf_ppvefeffe");
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Something went wrong: {ex}");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet]
    public IActionResult TestExceptions(int number)
    {
        CheckTheNumber(number);
        return Ok();
    }

    private void CheckTheNumber(int number)
    {
        if (number == 1)
        {
            throw new BadRequestException("Number = 1 is the bad request exception");
        }
        else if (number == 2)
        {
            throw new NotFoundException("Number = 2 is the Not found exception");
        }
        else if (number == 3)
        {
            throw new NotImplementedExceptions("Number = 3 is the Not implemented exception");
        }
        else if (number == 4)
        {
            throw new UnauthorizedException("Number = 4 is the unauthorized exception");
        }
        throw new Exception();

    }


    #endregion
    [HttpGet]
    [Authorize]
    public ActionResult<string> GetValue()
    {
        return "Authorization is working.";
    }
}
