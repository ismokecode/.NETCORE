namespace SWSS_v1.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        public int ClassId { get; set; }
        public int SubjectId { get; set; }
        public int CorrectAnswerId { get; set; }   
        public string QuestionText { get; set; }
        public List<Option> Options { get; set; }
        //public Question(string questionText, List<Option> options, int correctAnswerId)
        //{
        //    QuestionText = questionText;
        //    Options = options;
        //    CorrectAnswerId = correctAnswerId;
        //}
    }
}


