namespace SWSS_v1.Models
{
    public class Classes
    {
        public int ClassesId { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<Student> lstStudents { get; set; } 
    }
}
