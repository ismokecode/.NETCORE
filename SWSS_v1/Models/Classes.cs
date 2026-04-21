namespace SWSS_v1.Models
{
    public class Classes
    {
        public int ClassesId { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<Student> lstStudents = new List<Student>();
        public IEnumerable<Question> lstQuestion = new List<Question>();
        public Guid CreatedBy { get; set; }
        public Guid LastModifiedBy { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public DateTime CreatedDate { get;set; }
        public bool isActive { get; set; }
    }
} 
