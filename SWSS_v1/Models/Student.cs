using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class Student
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        //[ForeignKey("StudentClass")]
        public int ClassId { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string AlternatePhoneNumber { get; set; }
        public string Address { get; set; }
        public string PinCode { get; set; }

        public Classes StudentClass { get; set; }
    }
}
