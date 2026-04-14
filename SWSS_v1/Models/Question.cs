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
        [NotMapped]
        public int? AnswerOptionId { get; set; }   
        public string QuestionText { get; set; }
        public List<Option>? Options = new List<Option>();
        public virtual Classes? Classes { get; }
        public virtual Subject? Subjects { get; }
        [NotMapped]
        public string? Option1 { get; set; }
        [NotMapped]
        public string? Option2 { get; set; }
        [NotMapped]
        public string? Option3 { get; set; }
        [NotMapped]
        public string? Option4 { get; set; }
        //public Question(string questionText, List<Option> options, int correctAnswerId)
        //{
        //    QuestionText = questionText;
        //    Options = options;
        //    CorrectAnswerId = correctAnswerId;
        //}
    }
}


