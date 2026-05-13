namespace SWSS_v1.Models
{
    public class TestLink
    {
        public int LinkId { get; set; }
        public int StudentId { get; set; }
        public int SubjectId { get; set; }
        public int InstituteId { get; set; }
        public string OnlineLineTestLink { get; set; }
        public DateTime ExpiryDateTime { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime CreatedBy { get; set; }
            
    }
}
