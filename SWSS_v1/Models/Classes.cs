using System.ComponentModel.DataAnnotations.Schema;
namespace SWSS_v1.Models
{
    public class Classes
    {
        public int ClassesId { get; set; }
        public string ClassName { get; set; }
        public IEnumerable<Student>? lstStudents { get; }
        public IEnumerable<Question>? lstQuestion { get; }
        public IEnumerable<Subject>? lstSubject { get; }
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate = null;
        //[NotMapped]
        public DateTime? CreatedDate = null;
        public bool? isActive { get; set; }
        public int? InstituteId { get; set; }
    }
} 
