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

        public virtual TestLink? TestLinks { get; set; }
        [NotMapped]
        public string? OnlineTestLink { get; set; }
        [ForeignKey("fk_StudentResults_TestLinks")]
        public int TestLinkId { get; set; }

        #region VIEW
        [NotMapped]
        public int? InstituteId { get; set; }
        [NotMapped]
        public virtual StudentResult? StudentResults { get; set; }
        [NotMapped]
        public virtual Student? Students { get; }
        [NotMapped]
        public virtual Classes? Classes { get; }
        [NotMapped]
        public virtual Subject? Subjects { get; }
        [NotMapped]
        public string? ClassName { get; set; }
        [NotMapped]
        public string? SubjectName { get; set; }
        [NotMapped]
        public string? StudentName { get; set; }
        [NotMapped]
        public string? Email { get; set; }
        [NotMapped]
        public string? Phone { get; set; }
        [NotMapped]
        public int? ClassId { get; set; }
        [NotMapped]
        public int? SubjectId { get; set; }
        [NotMapped]
        public int? StudentId { get; set; }
        [NotMapped]
        public int? TotalQuestions { get; set; }
        [NotMapped]
        public int? LinkId { get; set; }
        #endregion
    }
    //[Table("StudentResults")]
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
