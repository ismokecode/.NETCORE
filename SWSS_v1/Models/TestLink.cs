namespace SWSS_v1.Models
{
    public class TestLink
    {
        public int LinkId { get; set; }
        public string TestUrl { get; set; }
        public int NumberOfQuestion { get; set; }
        public int[] StudentsId { get; set; }
        public int SubjectId { get; set; }
        public int ClassId { get; set; }
        public int InstituteId { get; set; }
        public string OnlineLineTestLink { get; set; }
        public int? Expiry { get; set; }
        public DateTime ExpiryDateTime { get; set; }
        public int Duration { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime CreatedBy { get; set; }
            
    }
}
