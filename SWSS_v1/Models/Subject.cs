namespace SWSS_v1.Models
{
    public class Subject
    {
        public int SubjectID { get; set; }
        public string SubjectName { get; set; }
        //public virtual ICollection<Question>? Questions { get; set; }
        public IEnumerable<Question> Questions = new List<Question>();
    }
}
