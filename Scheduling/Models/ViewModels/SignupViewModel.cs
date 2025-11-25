using System.ComponentModel.DataAnnotations;

namespace Scheduling.Models.ViewModels
{
    public class SignupViewModel
    {
        [Required, EmailAddress]
        public string UserEmail { get; set; }

        [Required, MinLength(8)]
        public string UserPassword { get; set; }

        [Required]
        public string ConfirmPassword { get; set; }

        [Required]
        public string UserName { get; set; }
    }
}
