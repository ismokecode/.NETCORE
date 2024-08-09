using System.ComponentModel.DataAnnotations;

namespace SWSS_v1.Models
{
    public class RegisterVM
    {
        [Required(ErrorMessage = "Please enter first name")]
        [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Please enter last name")]
        [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
        public string LastName { get; set; }
        [Required(ErrorMessage = "Please enter email")]
        [RegularExpression("^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$")]
        public string Email { get; set; }
        [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "Please enter password")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[#$^+=!*()@%&]).{8,}$")]
        public string Password { get; set; }
        public string UserRole { get; set; }
        [Required(ErrorMessage = "Please enter phone number")]
        [RegularExpression("^[0-9]{10}$")]
        public string Phone { get; set; }
        [RegularExpression("^[0-9]{6}$")]
        public string? Pincode { get; set; }
    }
}
