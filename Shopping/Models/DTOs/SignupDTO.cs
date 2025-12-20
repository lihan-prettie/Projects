using System.ComponentModel.DataAnnotations;

namespace Shopping.Models.DTOs
{
    public class SignupDTO
    {
        [Required(ErrorMessage = "電子郵件必填")]
        [EmailAddress(ErrorMessage ="電子郵件格式不正確")]
        public string Email { get; set; }

        

        [Required(ErrorMessage ="名稱必填")]
        public string UserName { get; set; }

        [Required(ErrorMessage ="必碼必填")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{8,15}$", ErrorMessage ="密碼未包含大小寫英文字母、阿拉伯數字和特殊符號")]
        public string Password { get; set; }
    }
}
