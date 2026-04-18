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
    private readonly IStudentRepository _istudentRepos;
    private readonly IQuestionRepository _iquestionRepos;
    private readonly IOptionRepository _iOptionRepos;
    private readonly IMailCommunication _imailCommunication;
    public AppController(UserManager<IdentityUser> userManager,
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
                cls.ClassName.Trim();
                if (!_unitOfWork.Classes.IsExist(cls) && cls.ClassesId == 0)
                {
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
        try
        {
            response._results = await _unitOfWork.Classes.GetAllAsync();
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
        APIResponse_V<Subject> response = new APIResponse_V<Subject>();
        response._success = new List<string>();
        response._errors = new List<string>();
        response._results = null;
        response._result = null;
        response.exception = null;
        try
        {
            if (ModelState.IsValid)
            {
                sub.SubjectName.Trim();
                if (!_unitOfWork.Subjects.IsExist(sub) && sub.SubjectID == 0)
                {
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
        try
        {
            response._results = await _unitOfWork.Subjects.GetAllAsync();
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
                stu.StudentName.Trim();
                if (!_unitOfWork.Students.IsExist(stu) && stu.StudentId == 0)
                {
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
        try
        {
            //var s = await _istudentReposs.GetAllAsync();
            response._results = await _istudentRepos.GetStudentClassDetailsAsync();
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
                question.QuestionText.Trim();
                
                if (!_unitOfWork.Questions.IsExist(question) && question.QuestionId == 0)
                {
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
                    _unitOfWork.BeginTransaction();
                    await _unitOfWork.Questions.UpdateAsync(question);
                    await _unitOfWork.Questions.SaveAsync();
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
                    var smtpSection = _configuration.GetSection("SmtpSettings");
                    var from = smtpSection["SenderEmail"]; // Accessing child key
                    var to = "r.jcool1.co.in@gmail.com"; // Accessing child key
                    var password = smtpSection["Password"];
                    _imailCommunication.Send(from, to, "Test mail", "Test link is workig.", password);
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
        try
        {
            response._results = await _iquestionRepos.GetQuestionOptionsAsync();
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

    #endregion olt

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
        return jwtToken;
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