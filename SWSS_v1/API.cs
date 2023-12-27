
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json.Linq;
using SWSS_v1;
using SWSS_v1.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

public class API : IMapEndPoints
{
    public void MapEndpoints(IEndpointRouteBuilder route)
    {        
        route.MapPost("GetHeaderDetails", GetHeaderDetails);
        route.MapPost("GetValueFromQuery", GetValueFromQuery);
        route.MapGet("GetValueFromQuery", GetValueFromQuery);
        route.MapGet("GetValueFromExplicitQuery", GetValueFromExplicitQuery);
        //route.MapGet("GetEmployeeDetails", GetEmployeeDetails);
    }

    #region Model binding [FromBody],[FromRoute],[FromQuery],[FromHeader],[FromForm],[FromServices]--for DI
    public APIResponse<string> GetHeaderDetails([FromHeader(Name = "Token")] string token)
    {
        return new APIResponse<string>(200,token,null,null);
    }
    public APIResponse<string> GetValueFromQuery([FromQuery] string token)
    {
        return new APIResponse<string>(200, token, null, null);
    }
    public APIResponse<string> GetValueFromExplicitQuery([FromQuery(Name ="Name")] string token)
    {
        //api/Setting?Name=Rajeev&Email=rajeevkr.co.in@gmail.com
        return new APIResponse<string>(200, token, null, null);
    }
    public APIResponse<string> GetValueFromExplicitQueryFromCollectionItems([FromQuery(Name = "items")] string[] items)
    {
        //api/Setting?items=item1&items=item2
        return new APIResponse<string>(200, null, null, null);
    }
    public APIResponse<string> GetValueFromForm([FromForm] Employee emp)
    {
        return new APIResponse<string>(200, "", null, null);
    }
    public APIResponse<string> GetIndividualValueFromForm([FromForm] string name,[FromForm] string email)
    {
        //when from will submitted and model of ui should be same in controller parameter
        return new APIResponse<string>(200, "", null, null);
    }

    [HttpPut("{facilityId}/{bandwidthChange}")] // constructor takes a template as parameter
    public void UpdateBandwidthChangeHangup([FromRoute] int facilityId, [FromRoute] int bandwidthChange) // use multiple FromRoute attributes, one for each parameter you are expecting to be bound from the routing data
    {
        //url def: api/Setting/163/10
    }
    #endregion
}
public class Error
{ 
    public string _error { get; set; }
    public string _description { get; set; }
}
public class APIResponse<T>
{
    public APIResponse(int statusCode, string token, List<Error> errors, T result)
    {
        this._statusCode = statusCode;
        this._message = token;
        this._errors = errors;
        this._result = result;
    }
    public List<Error> _errors = new List<Error>();
    public int _statusCode { get; }
    public T _result { get;  }
    public string _message { get; }
}