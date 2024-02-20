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
    public async Task<APIResponse<string>> Login([FromBody] LoginVM loginVM)
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
            var tokenValue = await GenerateJWTTokenAsync(_userExists, null);
            return new APIResponse<string>(StatusCodes.Status500InternalServerError, null, null, null);
        }
        else 
        {
            return new APIResponse<string>(StatusCodes.Status500InternalServerError, null, null, null);
        }
    }
    [HttpPost(Name = "VerifyAndGenerateTokenAsync")]
    private async Task<AuthResultVM> VerifyAndGenerateTokenAsync(TokenRequestVM tokenRequestVM)
    {
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        var storedToken = await _context.RefreshTokens.FirstOrDefaultAsync(x => x.token == tokenRequestVM.RefreshToken);
        var dbUser = await _userManager.FindByIdAsync(storedToken.UserId);
        try
        {
            var tokenCheckResult = jwtTokenHandler.ValidateToken(tokenRequestVM.Token, _tokenValidationParameters,
                out var validatedToken);
            return await GenerateJWTTokenAsync(dbUser, storedToken);
        }
        catch (SecurityTokenExpiredException)
        {
            if (storedToken.DateExpire >= DateTime.UtcNow)
            {
                return await GenerateJWTTokenAsync(dbUser, storedToken);
            }
            //if refresh token is not valid
            else
            {
                return await GenerateJWTTokenAsync(dbUser, null);
            }
        }
    }
    [HttpPost(Name = "RefreshToken")]
    public async Task<AuthResultVM> RefreshToken([FromBody] TokenRequestVM tokenRequestVM)
    {
        //check validation of data
        var result = await VerifyAndGenerateTokenAsync(tokenRequestVM);
        return result;
    }
    [HttpPost(Name = "TestToken")]
    public string TestToken()
    {
        return "JWT Token is working";
    }
    [HttpPost(Name = "GenerateJWTTokenAsync")]
    private async Task<AuthResultVM> GenerateJWTTokenAsync(IdentityUser user, RefreshToken rToken)
    {
        var authClaims = new List<Claim>()
         {
            new Claim(ClaimTypes.Name,user.UserName),
            new Claim(ClaimTypes.NameIdentifier,user.Id),
            new Claim(JwtRegisteredClaimNames.Email,user.Email),
            new Claim(JwtRegisteredClaimNames.Sub,user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString()),
         };
        //add user role to claim
        var userRoles = await _userManager.GetRolesAsync(user);
        foreach (var userRole in userRoles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, userRole));
        }
        var authSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes("This-is-token-key"));
        var token = new JwtSecurityToken(
        issuer: _configuration["Jwt2:Issuer"],
        audience: _configuration["Jwt2:Audience"],
        expires: DateTime.UtcNow.AddMinutes(1),
        claims: authClaims,
        signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );
        var jwtToken = new JwtSecurityTokenHandler().WriteToken(token);

        //if we refresh token is not nul then generate a new access token
        if (rToken != null)
        {
            var rTokenResponse = new AuthResultVM()
            {
                Token = jwtToken,
                RefreshToken = rToken.token,
                ExpiresAt = token.ValidTo
            };
        }

        var refreshToken = new RefreshToken()
        {
            JwtId = token.Id,
            IsRevoked = false,
            UserId = user.Id,
            DateAdded = DateTime.UtcNow,
            DateExpire = DateTime.UtcNow.AddMonths(6),
            token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString()
        };
        //add refresh token to the database
        await _context.RefreshTokens.AddAsync(refreshToken);
        await _context.SaveChangesAsync();

        var response = new AuthResultVM()
        {
            Token = jwtToken,
            RefreshToken = refreshToken.token,
            ExpiresAt = token.ValidTo
        };
        return response;
    }
    //#endregion
    //[Authorize(Roles = UserRoles.Student)]
    //[Authorize(Roles = UserRoles.Student+","+UserRoles.Manager)]


    [HttpPost(Name = "GetJWTToken")]
    public APIResponse<string> GetJWTToken()
    {
        var _token = GenerateJSONWebToken();
        return new APIResponse<string>(StatusCodes.Status200OK, null,null, null);
    }
    [HttpPost(Name = "GetEmployeeDetails")]
    public APIResponse<string> GetEmployeeDetails([FromServices] IEmployee service, Employee emp)
    {
        var _token = GenerateJSONWebToken();
        return new APIResponse<string>(StatusCodes.Status200OK, null,null, _token);
    }
    [HttpPost(Name = "GenerateJSONWebToken")]
    private string GenerateJSONWebToken()
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("ThisismySecretKey"));//_config["Jwt:Key"]
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken("Test.com", //_config["Jwt:Issuer"]
          "Test.com",
          null,
          expires: DateTime.Now.AddMinutes(120),
          signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
    [Authorize]
    [HttpPost(Name = "GetAuth")]
    public APIResponse<string> GetAuth()
    {
        return new APIResponse<string>(StatusCodes.Status200OK, null, null, null);
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
        else if (number == 5)
        {
            throw new Exception();
        }
    }
    #endregion
}
