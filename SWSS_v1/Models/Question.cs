using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class Question
    {
        public int QuestionId { get; set; }
        [ForeignKey("FK_QuestionClass")]
        public int ClassID { get; set; }
        [ForeignKey("FK_QuestionSubject")]
        public int SubjectId { get; set; }
        public int AnswerOptionId { get; set; }   
        public string QuestionText { get; set; }
        public List<Option>? Options = new List<Option>();
        public virtual Classes? Classes { get; }
        public virtual Subject? Subjects { get; }
        public string? option1 { get; set; }
        public string? option2 { get; set; }
        public string? option3 { get; set; }
        public string? option4 { get; set; }
        //public Question(string questionText, List<Option> options, int correctAnswerId)
        //{
        //    QuestionText = questionText;
        //    Options = options;
        //    CorrectAnswerId = correctAnswerId;
        //}
    }
}


