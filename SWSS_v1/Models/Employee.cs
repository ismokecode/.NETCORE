using SWSS_v1.UnitOfBox;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SWSS_v1.Models
{
    public class Employee
    {
        public int? EmployeeId { get; set; }
        [Required(ErrorMessage = "Please enter name")]
        [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
        public string? Name { get; set; }
        [RegularExpression("^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$")]
        public string? Email { get; set; }
        public string? Position { get; set; }
        //[Display(Name = "Department Name")]        
        public int DepartmentId { get; set; }
        [ForeignKey("DepartmentId")]
        public Department? Department { get; set; }
    }
    public class EmployeeView : Employee
    {
        public Department? Department { get; set; }
    }
}