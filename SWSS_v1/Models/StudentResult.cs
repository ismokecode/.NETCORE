using SWSS_v1.UnitOfBox;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class StudentResult
    {
        public int StudentResultId { get; set; }
        public int MarksObtained { get; set; }
        public string TestLinkGuid { get; set; }
        public DateTime? CreatedDateTime { get; set; }

        public virtual TestLink TestLink { get;}
        [ForeignKey("fk_StudentResults_TestLinks")]
        public string? OnlineTestLink { get; set; }



    }
    [Table("StudentResults")]
    public class StudentViewResult
    {
        public int StudentResultId { get; set; }
        public int MarksObtained { get; set; }
        public string TestLinkGuid { get; set; }
        public DateTime? CreatedDateTime { get; set; }
        public TestLink testLink { get; }
        public Subject subject { get; }
        public Classes classes { get; }
        public Student student { get; }
        public StudentResult studentResult = new StudentResult();
    }
}
