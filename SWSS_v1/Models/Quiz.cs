namespace SWSS_v1.Models
{
    public class Quiz
    {
        public string QuestionText { get; set; }
        public string[] Options { get; set; }
        public string Answer { get; set; }
        public int QuestionId { get; set; }
        public string _success { get; set; }
        public string _error { get; set; }
    }
}
