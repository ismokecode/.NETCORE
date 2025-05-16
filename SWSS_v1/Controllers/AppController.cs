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
    //private readonly CustomDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly IUnitOfWork _unitOfWork;
    public AppController(UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        //CustomDbContext context,
        IConfiguration configuration,
        TokenValidationParameters tokenValidationParameters,
        ILogger<AppController> logger,
        IUnitOfWork UnitOfWork
        )
    {
        _userManager = userManager;
        _roleManager = roleManager;
        //_context = context;
        _configuration = configuration;
        _tokenValidationParameters = tokenValidationParameters;
        _logger = logger;
        _unitOfWork = UnitOfWork;
    }
    #region IdentityUser 
    [HttpPost]
    [Authorize]
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
                _logger.LogInformation("Fetching all the Students");
                //check user exists
                var userExist = await _userManager.FindByEmailAsync(registerVM.Email);
                if (userExist != null)
                {
                    //if user exist
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add("Email already exist.");
                    return Ok(response); ;
                }

                //Add the user to db
                ApplicationUser user = new()
                {
                    UserName = registerVM.UserName,
                    Email = registerVM.Email,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    Phone = registerVM.Phone,
                    Pincode = registerVM.Pincode,
                    FirstName = registerVM.FirstName,
                    LastName = registerVM.LastName

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

    [HttpPost]
    public async Task<ActionResult<APIResponse_V<string>>> Login([FromBody] LoginVM loginVM)
    {
        APIResponse_V<string> response = new APIResponse_V<string>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (loginVM.Password == null)
            {
                response._errors.Add("Please enter password.");
            }
            if (loginVM.Email == null)
            {
                response._errors.Add("Please enter email.");
            }
            var _userExists = await _userManager.FindByEmailAsync(loginVM.Email);
            if (_userExists != null && await _userManager.CheckPasswordAsync(_userExists, loginVM.Password))
            {
                response._statusCode = StatusCodes.Status200OK;
                var tokenString = CreateToken(loginVM);
                response._result = tokenString;
                response._success.Add("Token generated successfully.");
            }
            else
            {
                response._errors.Add("Token not generated.");
            }
            return Ok(response);
        }
        catch (Exception ex) 
        {
            response._statusCode = StatusCodes.Status400BadRequest;
            response._errors.Add("Something went wrong, Please try later.");
            response.exception = "Something went wrong, Please try later.";
            return Ok(response);
        }
    }
    #endregion End IdentityUser 

    #region Service Methods
    [HttpPost]
    [Authorize]
    //[FromBody][Bind("employeeId", "name", "email", "position,departmentId")] Employee objAuth
    public async Task<ActionResult<APIResponse_V<Customer>>> CreateCustomer([FromBody][Bind("employeeId", "name", "email", "position,departmentId")] Customer customer)
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
                    await _unitOfWork.Repository<Customer>().InsertAsync(customer);
                    await _unitOfWork.Locations.SaveAsync();
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
                    await _unitOfWork.Customers.SaveAsync();
                    _unitOfWork.Commit();
                    response._statusCode = StatusCodes.Status200OK;
                    response._success.Add("Data updated successfully");
                    return Ok(response);
                }
                else
                {
                    response._statusCode = StatusCodes.Status409Conflict;
                    response._errors.Add(customer.Phone + " already exist");
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
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<Location>>> CreateLocation([FromBody][Bind("LocationId", "LocationName")] Location loc)
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
    [HttpDelete]
    [Authorize]
    public async Task<ActionResult<APIResponse_V<object>>> DeleteLocation(int id)
    {
        APIResponse_V<object> response = new APIResponse_V<object>();
        response._success = new List<string>();
        response._errors = new List<string>();
        try
        {
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
    #endregion End Service Method

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

    #region JWT token starts
    [HttpGet]
    public string CreateToken(LoginVM user)
    {
        var issuer = _configuration["Jwt:Issuer"];
        var audience = _configuration["Jwt:Audience"];
        var key = Encoding.ASCII.GetBytes
        (_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("Id", Guid.NewGuid().ToString()),
                //new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
             }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials
            (new SymmetricSecurityKey(key),
            SecurityAlgorithms.HmacSha512Signature)
        };
        var tokenHandler = new JwtSecurityTokenHandler();;
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);
        var stringToken = tokenHandler.WriteToken(token);
        return stringToken;
    }
    #endregion End Jwt token

    #region Test API
    [HttpGet]
    public IActionResult TestApi_IActionResult()
    {
        return Ok(new List<LoginVM> {
            new LoginVM() {Password="test",UserName="Rajeev" },
            new LoginVM(){Password = "123",UserName="Sonu" }
        });
    }
    [HttpGet]
    public ActionResult<List<LoginVM>> TestApi_ActionResult()
    {
        var s = 4;
        if (4 == 4)
        {
            return new List<LoginVM> {
            new LoginVM { FirstName ="Rajeev", LastName="Kumar" },
            new LoginVM {FirstName ="Rajeev", LastName="Kumar"}
        };
        }
        else
            return Ok(new List<LoginVM> {
            new LoginVM { FirstName ="Rajeev", LastName="Kumar" },
            new LoginVM {FirstName ="Rajeev", LastName="Kumar"}
        });
    }
    #endregion

}
