﻿using System.ComponentModel.DataAnnotations;

namespace SWSS_v1.Models
{
    public class LoginVM
    {
        [Required(ErrorMessage = "Please enter first name")]
        [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
        public string FirstName { get; set; }
        [Required(ErrorMessage = "Please enter last name")]
        [RegularExpression("[a-zA-Z0-9][a-zA-Z0-9.,'\\-_ ]*[a-zA-Z0-9]")]
        public string LastName { get; set; }
        [Required(ErrorMessage ="Please enter email")]
        [RegularExpression("^[\\w-\\.]+@([\\w-]+\\.)+[\\w-]{2,4}$")]
        public string Email { get; set; }
        public string UserName { get; set; }
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
        [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[#$^+=!*()@%&]).{8,}$")]
        public string Password { get; set; }
        public DateTime DateOfJoing { get; set; }
    }
}
