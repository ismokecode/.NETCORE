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
using SWSS_v1.UnitOfBox;
using System.Web.Http.Results;
using System.Net;
using System.Collections.Generic;
using Microsoft.Identity.Client;
using SWSS_v1.UnitOfWork;
using SWSS_v1.Models;
using NLog.Fluent;
using System.Web.Http.ModelBinding;
using SWSS_v1.Services;
using System.Security.Cryptography;
using Microsoft.AspNetCore.SignalR;

namespace SWSS_v1.Controllers;

[ApiController]
//Attribute routing becomes a requirement for apicontroller. For example:
[Route("api/[controller]/[action]")]
public class AppController : ControllerBase
{
    private ILogger<AppController> _logger;
    //they're using MyController:ControllerBase
    //private readonly UserManager<IdentityUser> _userManager;
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly RoleManager<IdentityRole> _roleManager;
    //private readonly CustomDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentRepository _istudentRepos;
    private readonly IQuestionRepository _iquestionRepos;
    private readonly IOptionRepository _iOptionRepos;
    private readonly IMailCommunication _imailCommunication;
    public AppController(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        //CustomDbContext context,
        IConfiguration configuration,
        TokenValidationParameters tokenValidationParameters,
        ILogger<AppController> logger,
        IUnitOfWork UnitOfWork,
        IStudentRepository istudentRepos,
        IQuestionRepository iQuestionRepos,
        IOptionRepository iOptionRepos,
        IMailCommunication imailCommunication
        )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        //_context = context;
        _configuration = configuration;
        _tokenValidationParameters = tokenValidationParameters;
        _logger = logger;
        _unitOfWork = UnitOfWork;
        _istudentRepos = istudentRepos;
        _iquestionRepos = iQuestionRepos;
        _iOptionRepos = iOptionRepos;
        _imailCommunication = imailCommunication;
    }
    #region IdentityUser 

    #region Reset Password Using Email token
    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return BadRequest();

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user == null) return RedirectToAction("ResetPasswordConfirmation");

        // Attempt to reset the password using the token
        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

        if (result.Succeeded)
        {
            return RedirectToAction("ResetPasswordConfirmation");
        }

        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
        return Ok();
    }


    #region ForgotPassword
    [HttpPost]
    public async Task<string> ForgotPassword(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
        {
            // Don't reveal if the user exists for security
            return "Please enter registered email.";
        }

        // Generate the reset token
        var token = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Create the callback URL
        var callbackUrl = Url.Action("ResetPassword", "Account",
            new { userId = user.Id, token = token }, protocol: Request.Scheme);

        // Send email using your IEmailSender implementation

        #region email functionality
        var smtpSection = _configuration.GetSection("SmtpSettings");
        var from = smtpSection["SenderEmail"]; // Accessing child key
        var to = "sudarshanbgs01@gmail.com"; // Accessing child key
        var password = smtpSection["Password"];
        _imailCommunication.Send(from, to, "Reset Password", $"Please reset your password by clicking here: <a href='{callbackUrl}'>link</a>", password);
        #endregion
        return "test";
    }
    #endregion


    #region Reset Password working
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<string>>> ResetPassword([FromBody] ResetPassword obj)
    {
        APIResponse_V<string> response = new APIResponse_V<string>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                var userName = User.Identity.Name;
                var user = await _userManager.FindByNameAsync(userName);
                if (user != null)
                {
                    var result = await _userManager.ChangePasswordAsync(user, obj.OldPassword, obj.NewPassword);
                    if(result.Succeeded)
                    {
                    response._success.Add("Password changed successfully.");
                    }
                    else 
                    {
                        response._success.Add("Incorrect old password.");
                    }
                    return Ok(response);
                }
                else
                {
                    response._errors.Add("Please enter registered username");
                    return Ok(response);
                }             
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex) 
        {
            response._errors.Add("Something went wrong.");
            return Ok(response);
        }
    }
    #endregion

    [HttpPost]
    //[Route("login")]
    public async Task<IActionResult> Login([FromBody] LoginVM model)
    {
        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
        {
            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var token = CreateToken(authClaims);
            var refreshToken = GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);

            await _userManager.UpdateAsync(user);

            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                RefreshToken = refreshToken,
                Expiration = token.ValidTo
            });
        }
        return Unauthorized();
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Super Admin")]
    public async Task<ActionResult<APIResponse_V<string>>> Register([FromBody] RegisterVM registerVM)
    {
        APIResponse_V<Customer> response = new APIResponse_V<Customer>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                string loggedInUser = User.Identity?.Name;
                // 1. Retrieve the user by their username
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                // 2. Get the list of role names associated with that user               
                var roles = await _userManager.GetRolesAsync(loggedInUserDeatails);
                foreach (var role in roles)
                {
                    //if super admin have multiple roles that condition is pending
                    if(role.ToUpper()=="SUPER ADMIN")
                    {
                        registerVM.UserRole = "ADMIN";
                    }
                    else 
                    {
                        registerVM.UserRole = "USER";
                    }
                }
                
                _logger.LogInformation("AppController register method called.");
                //check user exists
                var userExist = await _userManager.FindByNameAsync(registerVM.UserName);
                if (userExist != null)
                {
                    //if user exist
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add("User name already taken.");
                    return Ok(response); ;
                }
                //Add the user to db
                ApplicationUser user = new()
                {
                    FirstName = registerVM.FirstName,
                    LastName = registerVM.LastName,
                    Email = registerVM.Email,
                    CreatedBy = User.Identity?.Name,
                    CreatedDateTime = DateTime.Now,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    UserName = registerVM.UserName,
                    Phone = registerVM.Phone,
                    Pincode = registerVM.Pincode,
                    RefreshTokenExpiryTime = DateTime.Now.AddDays(_configuration.GetValue<double>("RefreshTokenValidityInDays")),
                    InstituteId = registerVM.InstituteId
                };
                if (await _roleManager.RoleExistsAsync(registerVM.UserRole))
                {
                   

                    var result = await _userManager.CreateAsync(user, registerVM.Password);
                    if (result.Errors.Count() > 0)
                    {

                        foreach (var error in result.Errors)
                        {
                            response._errors.Add(error.Description);
                            return Ok(response);
                        }
                    }
                    else if (result.Succeeded)
                    {
                        await _userManager.AddToRoleAsync(user, registerVM.UserRole);
                        response.isSucceed = true;
                        response._success.Add("Your registration has been successful.");
                        return Ok(response);
                    }
                }
                else
                {
                    response._statusCode = StatusCodes.Status404NotFound;
                    response._errors.Add("User role doesn't exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        //assign role to user
        catch (Exception ex)
        {
            response._statusCode = StatusCodes.Status400BadRequest;
            response._errors.Add("Something went wrong. Please try later.");
            return Ok(response);
        }
        return Ok(response);
    }

    [HttpPost]
    [Route("register-admin")]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterVM model)
    {
        var userExists = await _userManager.FindByNameAsync(model.UserName);
        if (userExists != null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User already exists!" });

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.UserName,
            FirstName = model.FirstName,
            LastName = model.LastName,
            InstituteId = _configuration.GetValue<int>("instititueId"),     
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed! Please check user details and try again." });
        if (!await _roleManager.RoleExistsAsync(UserRoles.SuperAdmin))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.SuperAdmin));
        if (!await _roleManager.RoleExistsAsync(UserRoles.Admin))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
        if (!await _roleManager.RoleExistsAsync(UserRoles.User))
            await _roleManager.CreateAsync(new IdentityRole(UserRoles.User));

        if (await _roleManager.RoleExistsAsync(UserRoles.SuperAdmin))
        {
            await _userManager.AddToRoleAsync(user, UserRoles.SuperAdmin);
        }
        //if (await _roleManager.RoleExistsAsync(UserRoles.Admin))
        //{
        //    await _userManager.AddToRoleAsync(user, UserRoles.Admin);
        //}
        //if (await _roleManager.RoleExistsAsync(UserRoles.User))
        //{
        //    await _userManager.AddToRoleAsync(user, UserRoles.User);
        //}
        return Ok(new { Status = "Success", Message = "User created successfully!" });
    }
    [HttpPost]
    [Route("register")]
    public async Task<IActionResult> Register_v([FromBody] RegisterVM model)
    {
        var userExists = await _userManager.FindByNameAsync(model.UserName);
        if (userExists != null)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User already exists!" });

        ApplicationUser user = new()
        {
            Email = model.Email,
            SecurityStamp = Guid.NewGuid().ToString(),
            UserName = model.UserName
        };
        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
            return StatusCode(StatusCodes.Status500InternalServerError, new { Status = "Error", Message = "User creation failed! Please check user details and try again." });

        return Ok(new { Status = "Success", Message = "User created successfully!" });
    }

    [HttpPost]
    [Route("refresh-token")]
    public async Task<IActionResult> RefreshToken(TokenModel tokenModel)
    {
        if (tokenModel is null)
        {
            return BadRequest("Invalid client request");
        }

        string? accessToken = tokenModel.AccessToken;
        string? refreshToken = tokenModel.RefreshToken;

        var principal = GetPrincipalFromExpiredToken(accessToken);
        if (principal == null)
        {
            return BadRequest("Invalid access token or refresh token");
        }

#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        string username = principal.Identity.Name;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.

        var user = await _userManager.FindByNameAsync(username);

        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
        {
            return BadRequest("Invalid access token or refresh token");
        }

        var newAccessToken = CreateToken(principal.Claims.ToList());
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await _userManager.UpdateAsync(user);

        return new ObjectResult(new
        {
            accessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
            refreshToken = newRefreshToken
        });
    }

    [Authorize]
    [HttpPost]
    [Route("revoke/{username}")]
    public async Task<IActionResult> Revoke(string username)
    {
        var user = await _userManager.FindByNameAsync(username);
        if (user == null) return BadRequest("Invalid user name");

        user.RefreshToken = null;
        await _userManager.UpdateAsync(user);

        return NoContent();
    }

    [HttpGet]
    [Authorize]
    public IEnumerable<IdentityUser> GetUsers()
    {
        var users = _userManager.Users.ToList();
        return users;
    }
    [HttpGet]
    [Authorize]
    public IQueryable<IdentityUser> GetUserByCode(string code)
    {
        var user = _userManager.Users.Where(x => x.Email == code);
        return user;
    }
    #endregion End IdentityUser 

    #region WaterSupply

    #region Customers
    [HttpPost]
    [Authorize]
    //[FromBody][Bind("employeeId", "name", "email", "position,departmentId")] Employee objAuth
    public async Task<ActionResult<APIResponse_V<Customer>>> CreateCustomer([FromBody] Customer customer)
    {
        APIResponse_V<Customer> response = new APIResponse_V<Customer>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                if (!_unitOfWork.Customers.IsExist(customer) && customer.CustomerId == 0)
                {
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Repository<Customer>().InsertAsync(customer);
                    await _unitOfWork.Customers.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    response._statusCode = StatusCodes.Status200OK;

                    return Ok(response);
                }
                else if(customer.CustomerId > 0)
                //else if (!_unitOfWork.Customers.IsExistUpdate(customer))
                {
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Customers.UpdateAsync(customer);
                    //await _unitOfWork.Repository<Customer>().UpdateAsync(customer);
                    await _unitOfWork.Customers.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add("Phone number "+ customer.Phone + " already exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }

        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Customer>>>GetAllCustomer()
    {
        APIResponse_V<Customer> response = new APIResponse_V<Customer>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._results = await _unitOfWork.Customers.GetAllAsync();
            response._success.Add("Successfully data fetched.");
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Customer>>> SearchCustomerByPhone(string phone)
    {
        APIResponse_V<Customer> response = new APIResponse_V<Customer>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._results = await _unitOfWork.Customers.SearchCustomerByPhoneAsync(phone);
            response._success.Add("Successfully data fetched.");
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Customer>>> DeleteCustomer(int id)
    {
        APIResponse_V<Location> response = new APIResponse_V<Location>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Customers.DeleteAsync(id);
            await _unitOfWork.Locations.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Customer>>> GetCustomerById(int id)
    {
        APIResponse_V<Customer> response = new APIResponse_V<Customer>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._result = await _unitOfWork.Customers.GetByIdAsync(id);
            if (response._result == null)
            {
                response._errors.Add("No data found.");
            }
            else
            {
                response._success.Add("Successfully data fetched.");
            }
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    #endregion

    #region Locations
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Location>>> CreateLocation([FromBody][Bind("LocationId", "LocationName")] Location loc)
    {
        APIResponse_V<Location> response = new APIResponse_V<Location>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                loc.LocationName.Trim();
                if (!_unitOfWork.Locations.IsExist(loc) && loc.LocationId == 0)
                {
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Locations.InsertAsync(loc);
                    await _unitOfWork.Locations.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else if(loc.LocationId>0)
                //else if (!_unitOfWork.Locations.IsExistUpdate(loc))
                {
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Locations.UpdateAsync(loc);
                    await _unitOfWork.Locations.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else 
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(loc.LocationName + " already exist");
                    return Ok(response); 
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Location>>> GetAllLocations()
    {
        APIResponse_V<Location> response = new APIResponse_V<Location>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            response._results = await _unitOfWork.Locations.GetAllAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Location>>> SearchLocationByName(string locationName)
    {
        APIResponse_V<Location> response = new APIResponse_V<Location>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._results = await _unitOfWork.Locations.SearchLocationByName(locationName);
            response._success.Add("Successfully data fetched.");
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Location>>> GetLocationById(int id)
    {
        APIResponse_V<Location> response = new APIResponse_V<Location>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._result = await _unitOfWork.Locations.GetByIdAsync(id);
            if (response._result == null) {
                response._errors.Add("No data found.");
            }
            else {
                response._success.Add("Successfully data fetched.");
            }          
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }    
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<object>>> DeleteLocation(int id)
    {
        APIResponse_V<object> response = new APIResponse_V<object>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Locations.DeleteAsync(id);
            await _unitOfWork.Locations.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    #endregion

    #endregion

    #region olt

    #region Classes
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Classes>>> CreateClass([FromBody] Classes cls)
    {
        APIResponse_V<Classes> response = new APIResponse_V<Classes>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;      
        try
        {
            if (ModelState.IsValid)
            {
                // 1. Retrieve the user by their username
                string loggedInUser = User.Identity?.Name;
                // 2. Get the list of role names associated with that user
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                cls.InstituteId = loggedInUserDeatails.InstituteId;
                cls.ClassName.Trim();         
                if (!_unitOfWork.Classes.IsExist(cls) && cls.ClassesId == 0)
                {                   
                    cls.CreatedBy = User.Identity?.Name;
                    cls.CreatedDate = DateTime.Now;
                    cls.isActive = true;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Classes.InsertAsync(cls);
                    await _unitOfWork.Classes.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else if (cls.ClassesId > 0)
                //else if (!_unitOfWork.Locations.IsExistUpdate(loc))
                {
                    _unitOfWork.BeginTransaction();
                    var result = await _unitOfWork.Classes.GetByIdAsync(cls.ClassesId);
                    cls.ModifiedDate = DateTime.Now;
                    cls.ModifiedBy = User.Identity?.Name;
                    cls.isActive = true;
                    await _unitOfWork.Classes.UpdateAsync(cls);
                    await _unitOfWork.Classes.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(cls.ClassName + " already exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Classes>>> DeleteClass(int id)
    {
        APIResponse_V<Classes> response = new APIResponse_V<Classes>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Classes.DeleteAsync(id);
            await _unitOfWork.Classes.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Classes>>> GetAllClasses()
    {
        APIResponse_V<Classes> response = new APIResponse_V<Classes>();       
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            response._results = await _unitOfWork.Classes.GetClassesByInstitute(InstituteId);
            return Ok(response);    
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;    
            return Ok(response);
        }
    }
    
    [HttpGet]
    public async Task<ActionResult<APIResponse_V<Classes>>> GetAllClassesForVisitors()
    {
        APIResponse_V<Classes> response = new APIResponse_V<Classes>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            // 1. Retrieve the user by their username
            //string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            //var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = _configuration.GetValue<int>("instititueId");
            response._results = await _unitOfWork.Classes.GetClassesByInstitute(InstituteId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Classes>>> GetClassById(int id)
    {
        APIResponse_V<Classes> response = new APIResponse_V<Classes>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._result = await _unitOfWork.Classes.GetByIdAsync(id);
            if (response._result == null)
            {
                response._errors.Add("No data found.");
            }
            else
            {
                response._success.Add("Successfully data fetched.");
            }
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    #endregion
    
    #region Subjects
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Subject>>> CreateSubject([FromBody] Subject sub)
    {
        APIResponse_V<Classes> response = new APIResponse_V<Classes>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                // 1. Retrieve the user by their username
                string loggedInUser = User.Identity?.Name;
                // 2. Get the list of role names associated with that user
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                sub.InstituteId = loggedInUserDeatails.InstituteId;

                sub.SubjectName.Trim();
                if (!_unitOfWork.Subjects.IsExist(sub) && sub.SubjectID == 0)
                {
                    sub.CreatedBy = User.Identity?.Name;
                    sub.CreatedDate = DateTime.Now;
                    sub.isActive = true;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Subjects.InsertAsync(sub);
                    await _unitOfWork.Subjects.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else if (sub.SubjectID > 0)
                //else if (!_unitOfWork.Locations.IsExistUpdate(loc))
                {
                    sub.ModifiedBy = User.Identity?.Name;
                    sub.ModifiedDate = DateTime.Now;
                    sub.isActive = true;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Subjects.UpdateAsync(sub);
                    await _unitOfWork.Subjects.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(sub.SubjectName + " already exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Subject>>> GetSubjectById(int id)
    {
        APIResponse_V<Subject> response = new APIResponse_V<Subject>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._result = await _unitOfWork.Subjects.GetByIdAsync(id);
            if (response._result == null)
            {
                response._errors.Add("No data found.");
            }
            else
            {
                response._success.Add("Successfully data fetched.");
            }
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Subject>>> GetAllSubjects()
    {
        APIResponse_V<Subject> response = new APIResponse_V<Subject>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            response._results = await _unitOfWork.Subjects.GetSubjectsByInstitute(InstituteId);
            response._success.Add("Data Saved successfully");
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }

    [HttpGet]
    public async Task<ActionResult<APIResponse_V<Subject>>> GetAllSubjectForVisitors(int classId)
    {
        APIResponse_V<Subject> response = new APIResponse_V<Subject>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            //keep hard coded visitors by default instituteId 5
            int instituteId = _configuration.GetValue<int>("instititueId");
            int[] subjectsId = await _unitOfWork.ClassSubjectMappers.GetSubjectsIdByClassIdForVisitors(instituteId, classId);
            
            response._results = await _unitOfWork.Subjects.GetSubjectsForVisitors(instituteId, subjectsId);
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Subject>>> DeleteSubject(int id)
    {
        APIResponse_V<Subject> response = new APIResponse_V<Subject>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Subjects.DeleteAsync(id);
            await _unitOfWork.Subjects.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    #endregion Subjects

    #region Students
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Student>>> CreateStudent(Student stu)
    {
        APIResponse_V<Student> response = new APIResponse_V<Student>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        stu.isActive = true;
        //stu.Classes = null;
        try
        {
            if (ModelState.IsValid)
            {
                // 1. Retrieve the user by their username
                string loggedInUser = User.Identity?.Name;
                // 2. Get the list of role names associated with that user
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                stu.InstituteId = loggedInUserDeatails.InstituteId;

                stu.StudentName.Trim();
                if (stu.StudentId == 0)
                {
                    stu.CreatedBy = User.Identity?.Name;
                    stu.CreatedDate = DateTime.UtcNow;
                    stu.isActive = true;
                    stu.isSelected = false;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Students.InsertAsync(stu);
                    await _unitOfWork.Students.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else if (stu.StudentId > 0)
                //else if (!_unitOfWork.Locations.IsExistUpdate(loc))
                {
                    stu.ModifiedBy = User.Identity?.Name;
                    stu.ModifiedDate = DateTime.UtcNow;
                    stu.isActive = true;
                    stu.isSelected = false;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Students.UpdateAsync(stu);
                    await _unitOfWork.Students.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(stu.StudentName + " already exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Student>>> GetStudentById(int id)
    {
        APIResponse_V<Student> response = new APIResponse_V<Student>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._result = await _istudentRepos.GetStudentClassDetailsById(id);
            if (response._result == null)
            {
                response._errors.Add("No data found.");
            }
            else
            {
                response._success.Add("Successfully data fetched.");
            }
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Student>>> GetAllStudents()
    {
        APIResponse_V<Student> response = new APIResponse_V<Student>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            //var s = await _istudentReposs.GetAllAsync();
            //response._results = await _istudentRepos.GetStudentClassDetailsAsync();
            response._results = await _istudentRepos.GetStudentsByInstitute(InstituteId);
            //var ss = _unitOfWork.Students.GetAllAsync();
            //var result = await _unitOfWork.Students.GetStudentClassDetailsAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Student>>> GetStudentsByClassId(int classId)
    {
        APIResponse_V<Student> response = new APIResponse_V<Student>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            //var s = await _istudentReposs.GetAllAsync();
            //response._results = await _istudentRepos.GetStudentClassDetailsAsync();
            response._results = await _istudentRepos.GetStudentsByInstituteAndClassId(InstituteId,classId);
            //var ss = _unitOfWork.Students.GetAllAsync();
            //var result = await _unitOfWork.Students.GetStudentClassDetailsAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Student>>> DeleteStudent(int id)
    {
        APIResponse_V<Student> response = new APIResponse_V<Student>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
            _unitOfWork.BeginTransaction();
            //await _unitOfWork.Students.DeleteAsync(id);
            await _istudentRepos.InActiveStudentAsync(id);
            await _unitOfWork.Students.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    #endregion Subjects

    #region Questions
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Question>>> CreateQuestion(Question question)
    {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                // 1. Retrieve the user by their username
                string loggedInUser = User.Identity?.Name;
                // 2. Get the list of role names associated with that user
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                question.InstituteId = loggedInUserDeatails.InstituteId;

                question.QuestionText.Trim();
                
                if (question.QuestionId == 0)
                {
                    question.CreatedBy = User.Identity?.Name;
                    question.CreatedDate = DateTime.Now;
                    question.isActive = true;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Questions.InsertAsync(question);
                    await _unitOfWork.Options.SaveAsync();
                    int id = question.QuestionId;
                    //_unitOfWork.Commit();
                    //_unitOfWork.BeginTransaction();
                    question.Options.Add(new Option { QuestionId = id, OptionText = question.Option1, isAnswer = false });
                    question.Options.Add(new Option { QuestionId = id, OptionText = question.Option2, isAnswer = false });
                    question.Options.Add(new Option { QuestionId = id, OptionText = question.Option3, isAnswer = false });
                    question.Options.Add(new Option { QuestionId = id, OptionText = question.Option4, isAnswer = false });
                    for(int i=0; i<4;i++)
                    {
                        if (i==question.AnswerOptionId)
                        {
                            question.Options[i].isAnswer = true;
                        }
                        else
                        {
                            question.Options[i].isAnswer = false;
                        }
                        _unitOfWork.Options.InsertAsync(question.Options[i]);
                        
                    }
                    await _unitOfWork.Options.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else if (question.QuestionId > 0)
                //else if (!_unitOfWork.Locations.IsExistUpdate(loc))
                {
                    question.ModifiedBy = User.Identity?.Name;
                    question.ModifiedDate = DateTime.Now;
                    question.isActive = true;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Questions.UpdateAsync(question);
                    //await _unitOfWork.Questions.SaveAsync();
                    //_unitOfWork.Commit();
                    question.Options.Add(new Option { OptionId = question.OptionId1, QuestionId = question.QuestionId, OptionText = question.Option1, isAnswer = false });
                    question.Options.Add(new Option { OptionId = question.OptionId2, QuestionId = question.QuestionId, OptionText = question.Option2, isAnswer = false });
                    question.Options.Add(new Option { OptionId = question.OptionId3, QuestionId = question.QuestionId, OptionText = question.Option3, isAnswer = false });
                    question.Options.Add(new Option { OptionId=  question.OptionId4, QuestionId = question.QuestionId, OptionText = question.Option4, isAnswer = false });
                    for (int i = 0; i < 4; i++)
                    {
                        if (i == question.AnswerOptionId)
                        {
                            question.Options[i].isAnswer = true;
                        }
                        else
                        {
                            question.Options[i].isAnswer = false;
                        }
                    }
                    //for(int i=0; i<4;i++)
                    //{
                    //    _unitOfWork.Options.UpdateAsync(question.Options[i]);
                    //}
                    await _unitOfWork.Options.UpdateAsyncList(question.Options);
                    await _unitOfWork.Options.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(question.QuestionText + " already exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {   
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }

    [HttpPost]
    [Authorize]
    //public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion(int[] questionsId, int classId, int subjectId)    public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion(int[] questionsId, int classId, int subjectId)
    public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion([FromBody] QuestionRequest request)
     {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>(); 
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.BeginTransaction();
                await _unitOfWork.Questions.SetAsQuestions(request.questionsId, request.classId, request.subjectId);

                _unitOfWork.Commit();
                response._statusCode = StatusCodes.Status200OK;
                #region email functionality
                //var smtpSection = _configuration.GetSection("SmtpSettings");
                //var from = smtpSection["SenderEmail"]; // Accessing child key
                //var to = "sudarshanbgs01@gmail.com"; // Accessing child key
                //var password = smtpSection["Password"];
                //_imailCommunication.Send(from, to, "Test mail", "Test link is workig.", password);
                #endregion
                response._success.Add("Data updated successfully");

                return Ok(response);
            }
        }
        catch(Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    //public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion(int[] questionsId, int classId, int subjectId)    public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion(int[] questionsId, int classId, int subjectId)
    public async Task<ActionResult<APIResponse_V<String>>> ClassAndSubjectMapper([FromBody] ClassSubjectMapper obj)
    {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                // 1. Retrieve the user by their username
                string loggedInUser = User.Identity?.Name;
                // 2. Get the list of role names associated with that user
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                obj.InstituteId = loggedInUserDeatails.InstituteId;
                if (!_unitOfWork.ClassSubjectMappers.IsExist(obj))
                {
                   
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.ClassSubjectMappers.AddAsync(obj);
                    _unitOfWork.ClassSubjectMappers.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else
                {
                    response._success.Add("Data already exist");
                }
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
        finally
        {
        }
        return Ok(response);
    }

    [HttpPost]
    [Authorize]
    //public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion(int[] questionsId, int classId, int subjectId)    public async Task<ActionResult<APIResponse_V<Question>>> SetAsQuestion(int[] questionsId, int classId, int subjectId)
    public async Task<ActionResult<APIResponse_V<TestLink>>> CreateAndSendTestLinks([FromBody] TestLink request)
    {
        APIResponse_V<TestLink> response = new APIResponse_V<TestLink>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                request.ExpiryDateTime = DateTime.UtcNow.AddHours(12);
                // 1. Retrieve the user by their username
                string loggedInUser = User.Identity?.Name;
                // 2. Get the list of role names associated with that user
                var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
                request.InstituteId = loggedInUserDeatails.InstituteId;

                _unitOfWork.BeginTransaction();
                IEnumerable<TestLink> returnVal;

                List<TestLink> linkDetails= request.StudentsId.Select(id => new TestLink
                {
                    StudentId = id,
                    ClassId = request.ClassId,
                    SubjectId = request.SubjectId,
                    ExpiryDateTime = request.ExpiryDateTime,
                    Durations = request.Durations,
                    TotalQuestions = request.TotalQuestions,
                    OnlineLineTestLink = Convert.ToString(Guid.NewGuid()) + "instituteId=" + request.InstituteId + "classId=" + request.ClassId + "subjectId=" + request.SubjectId.ToString(),
                    InstituteId = request.InstituteId
                }).ToList();

                _unitOfWork.TestLinks.SaveTestLinkAsync(linkDetails);

                response._statusCode = StatusCodes.Status200OK;
                //get all students details by StudentsId[]
                #region email functionality
                string[] emails = await _unitOfWork.Students.GetStudentsEmailById(request.StudentsId);
                _unitOfWork.Commit();
                //configuration section not to store into var
                var smtpSection = _configuration.GetSection("SmtpSettings");
                string from = smtpSection["SenderEmail"]; // Accessing child key
                string password = smtpSection["Password"];
                string subject = smtpSection["Subject"];
                string testLink = smtpSection["TestLink"];
                string to = string.Empty;
                for (int i=0; i<emails.Length; i++)
                {                    
                    //getting students emails using StudentsId[]              
                    to = emails[i]; // Accessing child key                  
                    _imailCommunication.Send(from, to, subject, testLink + linkDetails[i].OnlineLineTestLink,password);                  
                }
                #endregion
                response._success.Add("Data updated successfully");
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
        return Ok(response);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Question>>> GetQuestionById(int id)
    {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._result = await _iquestionRepos.GetQuestionOptionsByIdAsync(id);

            if (response._result == null)
            {
                response._errors.Add("No data found.");
            }
            else
            {
                response._success.Add("Successfully data fetched.");
            }
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Question>>> GetAllQuestions()
    {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            response._results = await _iquestionRepos.GetQuestionOptionsByInstituteAsync(InstituteId);
            //response._results = await _unitOfWork.Questions.GetAllAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Question>>> GetQuestionsByClassAndSubject(int classId,int subjectId)
    {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            response._results = await _iquestionRepos.GetQuestionsByClassAndSubject(classId, subjectId, InstituteId);
            if(response._results.Count()==0)
            {
                response._success.Add("Sorry no records found.");

            }
            //response._results = await _unitOfWork.Questions.GetAllAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<Quiz>>> GetQuizByClassAndSubject(int classId, int subjectId)
    {
        List<Quiz> response = new List<Quiz>();
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            int InstituteId = loggedInUserDeatails.InstituteId;
            var results = await _iquestionRepos.GetQuizQuestionByClassAndSubject(classId, subjectId, InstituteId);

            if (results.Count() > 0)
            {
                foreach(var quiz in results)
                {
                    response.Add(new Quiz
                    {
                        QuestionText = quiz.QuestionText,
                        Options = new string[]
                        {
                            quiz.Options[0].OptionText.ToString(),
                            quiz.Options[1].OptionText.ToString(),
                            quiz.Options[2].OptionText.ToString(),
                            quiz.Options[3].OptionText.ToString()
                        },
                        Answer = GetAnswer(quiz.Options)
                    });
                }
             }
            else
            {
                
            }
            //response._results = await _unitOfWork.Questions.GetAllAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(response);
        }
    }

    [HttpGet]
    public async Task<ActionResult<List<Quiz>>> GetQuizByClassAndSubjectForVisitors(int classId, int subjectId)
    {
        List<Quiz> response = new List<Quiz>();
        try
        {
            // 1. Retrieve the user by their username
            string loggedInUser = User.Identity?.Name;
            // 2. Get the list of role names associated with that user
            //var loggedInUserDeatails = await _userManager.FindByNameAsync(loggedInUser);
            //int InstituteId = loggedInUserDeatails.InstituteId;
            int InstituteId = _configuration.GetValue<int>("instititueId");       
            var results = await _iquestionRepos.GetQuizQuestionByClassAndSubject(classId, subjectId, InstituteId);

            if (results.Count() > 0)
            {
                foreach (var quiz in results)
                {
                    response.Add(new Quiz
                    {
                        QuestionText = quiz.QuestionText,
                        Options = new string[]
                        {
                            quiz.Options[0].OptionText.ToString(),
                            quiz.Options[1].OptionText.ToString(),
                            quiz.Options[2].OptionText.ToString(),
                            quiz.Options[3].OptionText.ToString()
                        },
                        Answer = GetAnswer(quiz.Options)
                    });
                }
            }
            else
            {

            }
            //response._results = await _unitOfWork.Questions.GetAllAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(response);
        }
    }
    public static string GetAnswer(List<Option> options)
    {
        foreach(var option in options)
        {
            if(option.isAnswer)
            {
                return option.OptionText;
            }
        }
        return "";
    }

    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Question>>> DeleteQuestion(int id)
    {
        APIResponse_V<Question> response = new APIResponse_V<Question>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
            _unitOfWork.BeginTransaction();
            await _iOptionRepos.DeleteOptionByQuestionIdAsync(id);
            await _unitOfWork.Questions.DeleteAsync(id);
            await _unitOfWork.Questions.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    #endregion

    #region Institue
    [HttpPost]
    [Authorize(Roles ="Super Admin")]
    public async Task<ActionResult<APIResponse_V<Institute>>> CreateInstitute([FromBody] Institute obj)
    {
        APIResponse_V<Institute> response = new APIResponse_V<Institute>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                obj.Name.Trim();
                if (/*!_unitOfWork.Institutes.IsExist(obj) &&*/ obj.InstituteId == 0)
                {
                    obj.CreatedBy = User.Identity?.Name;
                    var user = await _userManager.FindByNameAsync(obj.CreatedBy);
                    obj.CreatedBy = user.Id;
                    obj.CreatedDate = DateTime.Now;
                    obj.isActive = true;
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Institutes.InsertAsync(obj);
                    await _unitOfWork.Institutes.SaveAsync();
                    _unitOfWork.Commit();
                    response._success.Add("Data saved successfully");
                    return Ok(response);
                }
                else if (obj.InstituteId > 0)
                //else if (!_unitOfWork.Locations.IsExistUpdate(loc))
                {
                    _unitOfWork.BeginTransaction();
                    var result = await _unitOfWork.Institutes.GetByIdAsync(obj.InstituteId);
                    obj.ModifiedBy = User.Identity?.Name;
                    obj.ModifiedDate = DateTime.Now;
                    obj.isActive = true;                  
                    await _unitOfWork.Institutes.UpdateAsync(obj);
                    await _unitOfWork.Institutes.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(obj.Name + " already exist");
                    return Ok(response);
                }
            }
            else
            {
                foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry modelState in ModelState.Values)
                {
                    foreach (Microsoft.AspNetCore.Mvc.ModelBinding.ModelError error in modelState.Errors)
                    {
                        response._errors.Add(error.ErrorMessage);
                    }
                }
                response._statusCode = StatusCodes.Status400BadRequest;
                return Ok(response);
            }
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status400BadRequest;
            //return new BadRequestException(ex.ToString());
            return Ok(response);
        }
    }
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Institute>>> DeleteInstitute(int id)
    {
        APIResponse_V<Institute> response = new APIResponse_V<Institute>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            _unitOfWork.BeginTransaction();
            await _unitOfWork.Institutes.DeleteAsync(id);
            await _unitOfWork.Institutes.SaveAsync();
            _unitOfWork.Commit();
            response._success.Add("Data deleted successfully");
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Institute>>> GetAllInstitute()
    {
        APIResponse_V<Institute> response = new APIResponse_V<Institute>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            response._results = await _unitOfWork.Institutes.GetAllAsync();
            return Ok(response);
        }
        catch (Exception ex)
        {
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return Ok(response);
        }
    }
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Institute>>> GetInstituteById(int id)
    {
        APIResponse_V<Institute> response = new APIResponse_V<Institute>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        //response._result = null;
        response.exception = null;
        try
        {
            _unitOfWork.BeginTransaction();
        
            response._result = await _unitOfWork.Institutes.GetByIdAsync(id);
            _unitOfWork.Commit();
            if (response._result == null)
            {
                response._errors.Add("No data found.");
            }
            else
            {
                response._success.Add("Successfully data fetched.");
            }
            response._statusCode = StatusCodes.Status200OK;
            return Ok(response);
        }
        catch (Exception ex)
        {
            _unitOfWork.Rollback();
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            response._statusCode = StatusCodes.Status500InternalServerError;
            return BadRequest(response);
        }
    }
    #endregion

    #region online test link get questions for students
    [HttpGet]
    public async Task<ActionResult<List<Quiz>>> OnlineTestLinkForStudents(string id)
    {
        List<Quiz> response = new List<Quiz>();
        try
       {
            //link start
            if (id != null)
            {
                TestLink obj = await _unitOfWork.TestLinks.GetByIdAsync(id);
                if (obj != null)
                {
                    //check link expiry start
                    if (_unitOfWork.TestLinks.isEarlier(obj.ExpiryDateTime ?? DateTime.Now))
                    {
                    var results = await _iquestionRepos.GetQuizQuestionByClassAndSubject(obj.ClassId, obj.SubjectId, obj.InstituteId ?? 0);

                    if (results.Count() > 0)
                    {
                        foreach (var quiz in results)
                        {
                            response.Add(new Quiz
                            {
                                QuestionText = quiz.QuestionText,
                                Options = new string[]
                                {
                                    quiz.Options[0].OptionText.ToString(),
                                    quiz.Options[1].OptionText.ToString(),
                                    quiz.Options[2].OptionText.ToString(),
                                    quiz.Options[3].OptionText.ToString()
                                },
                                Answer = GetAnswer(quiz.Options)
                            });
                        }
                    }
                    else
                    {

                    }
                    //response._results = await _unitOfWork.Questions.GetAllAsync();
                    return Ok(response);
                }
               //check link expiry end
                }
                return Ok(response);
            }
            return Ok(response);
        }      
        catch (Exception ex)
        {
            return Ok(response);
        }
    }
    #endregion

    #endregion olt

    #region JWT token starts
    //[HttpGet]
    //public string CreateToken(LoginVM user)
    //{
    //    var issuer = _configuration["Jwt:Issuer"];
    //    var audience = _configuration["Jwt:Audience"];
    //    var key = Encoding.ASCII.GetBytes
    //    (_configuration["Jwt:Key"]);
    //    var tokenDescriptor = new SecurityTokenDescriptor
    //    {
    //        Subject = new ClaimsIdentity(new[]
    //        {
    //            new Claim("Id", Guid.NewGuid().ToString()),
    //            //new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
    //            new Claim(JwtRegisteredClaimNames.Email, user.Email),
    //            new Claim(JwtRegisteredClaimNames.Jti,
    //            Guid.NewGuid().ToString())
    //         }),
    //        Expires = DateTime.UtcNow.AddMinutes(5),
    //        Issuer = issuer,
    //        Audience = audience,
    //        SigningCredentials = new SigningCredentials
    //        (new SymmetricSecurityKey(key),
    //        SecurityAlgorithms.HmacSha512Signature)
    //    };
    //    var tokenHandler = new JwtSecurityTokenHandler();;
    //    var token = tokenHandler.CreateToken(tokenDescriptor);

    //    #region added claims & refresh tokens
    //    //https://www.c-sharpcorner.com/article/jwt-authentication-with-refresh-tokens-in-net-6-0/
    //    var refreshToken = GenerateRefreshToken();
    //    int.TryParse(_configuration.GetSection("Jwt")["RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);
    //    user.RefreshToken = refreshToken;
    //    user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);
    //    //check below code later
    //    //new
    //    //{
    //    //    Token = new JwtSecurityTokenHandler().WriteToken(token),
    //    //    RefreshToken = refreshToken,
    //    //    Expiration = token.ValidTo
    //    //});
    //    #endregion

    //    var jwtToken = tokenHandler.WriteToken(token);
    //    return jwtToken;
    //}
    #region GenerateRefreshToken
    private JwtSecurityToken CreateToken(List<Claim> authClaims)
    {
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
        int.TryParse(_configuration["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

        return token;
    }
    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }
    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
            ValidateLifetime = false
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
        if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            throw new SecurityTokenException("Invalid token");

        return principal;

    }

    [Authorize]
    [HttpPost]
    [Route("revoke-all")]
    public async Task<IActionResult> RevokeAll()
    {
        var users = _userManager.Users.ToList();
        foreach (var user in users)
        {
            //check it later
            //user.RefreshToken = null;
            await _userManager.UpdateAsync(user);
        }

        return NoContent();
    }

    #endregion

    #endregion End Jwt token

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


    #endregion

    #region Test API
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
    [HttpGet]
    public IActionResult TestExceptions(int number)
    {
        CheckTheNumber(number);
        return Ok();
    }
    [HttpGet] 
    public IActionResult TestApi_IActionResult()
    {
        return Ok(new List<LoginVM> {
            new LoginVM() {Password="test",UserName="Rajeev" },
            new LoginVM(){Password = "123",UserName="Sonu" }
        });
    }
    #endregion
}