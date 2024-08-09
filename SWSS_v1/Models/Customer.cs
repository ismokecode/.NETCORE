using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWSS_v1.Models;

public partial class Customer
{
    public int CustomerId { get; set; }
    [Required(ErrorMessage ="Please enter customer name")]
    [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
    public string Name { get; set; } = null!;
    [RegularExpression("^[0-9]{10}$")]
    public string? Phone { get; set; }

    public int? Type { get; set; }

    public int? LocationId { get; set; }

    public virtual Location? Location { get; set; }
}
