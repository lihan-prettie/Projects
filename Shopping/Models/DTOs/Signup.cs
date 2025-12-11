using System.ComponentModel.DataAnnotations;

namespace Shopping.Models.DTOs
{
    public class Signup
    {
        [Required]
        public string Email { get; set; }

        [Required]
        public string UserName { get; set; }

        [Required]
        public string Password { get; set; }
    }
}
