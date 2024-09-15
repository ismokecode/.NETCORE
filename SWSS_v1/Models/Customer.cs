using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SWSS_v1.Models;

public partial class Customer
{
    public int CustomerId { get; set; }
    [Required(ErrorMessage ="Name field is required")]
    [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]", ErrorMessage = "Name field is invalid")]
    public string Name { get; set; } = null!;
    [RegularExpression("^[0-9]{10}$", ErrorMessage = "Phone field is invalid")]
    public string? Phone { get; set; }
    [RegularExpression("^[0-9]{10}$")]
    public string? SecondaryNumber { get; set; }
    [ForeignKey("Location")]
    public int? LocationId { get; set; }
    [ForeignKey("Department")]
    public int? DepartmentId { get; set; }
    [JsonIgnore]
    public Location? Location { get; set; }

    [JsonIgnore]
    public Department? Department { get; set; }

}
