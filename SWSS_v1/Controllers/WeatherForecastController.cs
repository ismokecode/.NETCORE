using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using SWSS_v1.Models.AppliactionModels;

namespace SWSS_v1.Controllers;

[ApiController]
//controller route
[Route("api/[controller]/[action]")]
//explicit route for action as well:
//[Route("GetById")]
public class WeatherForecastController : ControllerBase
{

    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    [HttpGet(Name = "Details")]
    public string Details()
    {
        return _service.GetEmployeeDetails();
    }

    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IEmployee _service;
    public WeatherForecastController(ILogger<WeatherForecastController> logger, IEmployee service)
    {
        _logger = logger;
        _service = service;
    }

    [HttpGet(Name = "GetWeatherForecast")]
    public IEnumerable<WeatherForecast> Get()
    {
        return Enumerable.Range(1, 5).Select(index => new WeatherForecast
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        })
        .ToArray();
    }
}
