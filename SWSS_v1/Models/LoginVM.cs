using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SWSS_v1.Models
{
    public class LoginVM
    {

        [Required(ErrorMessage ="Please enter user name")]
        //[RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]",ErrorMessage ="Please enter email in valid format")]
        public string? UserName { get; set; }
        /// <summary>
        /// ^: first line
        ///(?=.*[a-z]) : Should have at least one lower case
        ///(?=.*[A-Z]) : Should have at least one upper case
        ///(?=.*\d) : Should have at least one number
        ///(?=.*[#$^+=!*()@%&] ) : Should have at least one special character
        ///.{ 8,} : Minimum 8 characters
        ///$ : end line
        /// </summary>
        [Required(ErrorMessage ="Please enter password")]
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[#$^+=!*()@%&]).{8,}$",ErrorMessage ="Password should be atleast 8 character long and must contains atleast one small, capital, numeric, special character")]
        public string? Password { get; set; }
 
    }
}
