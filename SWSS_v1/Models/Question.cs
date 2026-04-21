using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class Question
    {
        public Question()
        {
            Options = new List<Option>();
        }
        public int QuestionId { get; set; }
        [ForeignKey("FK_QuestionClass")]
        public int ClassID { get; set; }
        [ForeignKey("FK_QuestionSubject")]
        public int SubjectId { get; set; }
        [NotMapped]
        public int? AnswerOptionId { get; set; }   
        public string QuestionText { get; set; }
        //While create The Options field is required. So make it optional
        public List<Option>? Options { get; }
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
        [NotMapped]
        public int OptionId1 { get; set; }
        [NotMapped]
        public int OptionId2 { get; set; }
        [NotMapped]
        public int OptionId3 { get; set; }
        [NotMapped]
        public int OptionId4 { get; set; }
        //public Question(string questionText, List<Option> options, int correctAnswerId)
        //{
        //    QuestionText = questionText;
        //    Options = options;
        //    CorrectAnswerId = correctAnswerId;
        //}
    }
}


