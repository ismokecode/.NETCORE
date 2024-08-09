using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SWSS_v1.Models;

public partial class Location
{
    public int LocationId { get; set; }
    [Required(ErrorMessage ="Please enter location")]
    [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
    public string? LocationName { get; set; }
    public string GoogleLocation { get; set; }

    public Customer? Customer { get; set; }
}
