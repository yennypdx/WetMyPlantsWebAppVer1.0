using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApp.Models.AccountViewModels
{
    public class ChangePasswordViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password), Display(Name = "Current Password")]
        public string Password { get; set; }

        [Required]
        [DataType(DataType.Password), Display(Name = "New Password")]
        public string NewPassword { get; set; }
    }
}