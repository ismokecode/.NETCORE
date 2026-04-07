namespace SWSS_v1.Models
{
    public class Option
    {
        public int OptionId {get; set;}
        public int QuestionId { get; set; }
        public string OptionText { get; set; } // e.g., 'A', 'B', 'C'

        // Constructor to easily create an Option object
        //public Option(int questionId, string optionText)
        //{
        //    QuestionId = questionId;
        //    OptionText = optionText;
        //}
    }
}
