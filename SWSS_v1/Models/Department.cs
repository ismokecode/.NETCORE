namespace SWSS_v1.Models
{
    public class Department
    {
        public int DepartmentId { get; set; }
        public string Name { get; set; }
    }
    public class DepartmentView:Department
    {
        public List<Employee> Employees { get; set; }

    }
}
