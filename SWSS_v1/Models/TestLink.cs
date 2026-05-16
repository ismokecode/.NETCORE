using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class TestLink
    {
        [Key]
        public int LinkId { get; set; }
        [Required(ErrorMessage ="Please select total number of questions")]
        public int TotalQuestions { get; set; }
        [NotMapped]
        [Required(ErrorMessage ="Please select students")]
        public int[]? StudentsId { get; set; }
        [Required(ErrorMessage ="Please select subject")]
        public int SubjectId { get; set; }
        [Required(ErrorMessage ="Please select class")]
        public int ClassId { get; set; }
        public int? InstituteId { get; set; }
        public string? OnlineLineTestLink { get; set; }
        [Required(ErrorMessage ="Please select valid for")]
        [NotMapped]
        public int? Expiry { get; set; }
        public DateTime? ExpiryDateTime { get; set; }
        [Required(ErrorMessage ="Please select duration")]
        public int Durations { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? CreatedBy { get; set; }
        public int StudentId { get; set; }

    }
}
