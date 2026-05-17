namespace SWSS_v1.Models
{
    public class Subject
    {
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        //public virtual ICollection<Question>? Questions { get; set; }
        public IEnumerable<Question> Questions = new List<Question>();
        public string? CreatedBy { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? ModifiedDate = null;
        //[NotMapped]
        public DateTime? CreatedDate = null;
        public bool? isActive { get; set; }
        public int? InstituteId { get; set; }
        public List<Classes>? Classes { get; }
        public int? ClassId { get; set; }
    }
}
