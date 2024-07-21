﻿using SWSS_v1.UnitOfBox;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace SWSS_v1.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        //[Display(Name = "Department Name")]
        [ForeignKey("DepartmentId")]
        public int DepartmentId { get; set; }

    }
    public class EmployeeView : Employee
    {
        public Department? Department { get; set; }
    }
}