using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        // Foreign Key
        [ForeignKey("FK_StudentsClass")]
        public int ClassesId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AlternatePhoneNumber { get; set; }
        public string Address { get; set; }
        public string PinCode { get; set; }
        // Navigation Property to Reference Table
        //public Classes Classes = new Classes();
        public virtual Classes? Classes { get; }
        public string? CreatedBy { get; set; }
        public string? LastModifiedBy { get; set; }
        public bool isActive { get; set; }
        //public DateTime CreatedDate = DateTime.Now;
    }
}
