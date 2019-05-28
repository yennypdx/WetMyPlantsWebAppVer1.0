using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApp.Models.AccountViewModels
{
    public class PinViewModel
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int Pin { get; set; }
    }
}