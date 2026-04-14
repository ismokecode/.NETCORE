using System.ComponentModel.DataAnnotations.Schema;
namespace SWSS_v1.Models
{
    public class Option
    {
        public int OptionId {get; set;}
        [ForeignKey("FK_OptionQuestion")]
        public int QuestionId { get; set; }
        public string OptionText { get; set; }
        public bool isAnswer { get; set; }
        public virtual Question? Question { get; set; }
    }
}
