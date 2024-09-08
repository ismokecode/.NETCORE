using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models;

public partial class Customer
{
    public int CustomerId { get; set; }
    [Required(ErrorMessage ="Please enter customer name")]
    [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
    public string Name { get; set; } = null!;
    [RegularExpression("^[0-9]{10}$")]
    public string? Phone { get; set; }
    [RegularExpression("^[0-9]{10}$")]
    public string? SecondaryNumber { get; set; }

    public int? LocationId { get; set; }
    public int? DepartmentId { get; set; }
    //[ForeignKey("LocationId")]
    public virtual Location? Location { get; set; }
    //[ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

}
