using System.ComponentModel.DataAnnotations;

namespace SWSS_v1.Models
{
    public class ResetPassword
    {
        [Required(ErrorMessage = "Please enter old password")]
        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Please enter new password")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[#$^+=!*()@%&]).{8,}$",ErrorMessage = "A new password minimum of 8 characters, atleast one upper, lower case and special character.")]       
        public string NewPassword { get; set; }

    }
    public class ResetPasswordViewModel
    {
        public string Email { get; set; }
        public string Token { get; set; }
        public string NewPassword { get; set; }
    }
}
