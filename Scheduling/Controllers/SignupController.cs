using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using Scheduling.Models;
using Scheduling.Models.ViewModels;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Scheduling.Helpers;

namespace Scheduling.Controllers
{
    public class SignupController : Controller
    {
        private readonly SchedulingContext _context;
        private static readonly Regex regex = new(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$", RegexOptions.Compiled);
        public SignupController(SchedulingContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        //POST:Signup/EmailCheck
        [HttpPost]
        public IActionResult EmailCheck(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { exists = false, message = "Email 不可為空" });
            }

            string normalizedEmail = email.Trim().ToLower();
            bool exists = _context.Users.Any(u => u.UserEmail.ToLower() == normalizedEmail);

            return Json(new { exists });
        }

        //POST : Signup/Signup
        [HttpPost]
        public IActionResult SignUp(SignupViewModel model)
        {
            //驗證input
            if (!ModelState.IsValid) return Json(new { success = false, message = "註冊資訊未填寫完整" });

            if (!(model.UserPassword == model.ConfirmPassword)) return Json(new { success = false, message = "密碼與確認密碼不相符" });

            if (!new EmailAddressAttribute().IsValid(model.UserEmail)) return Json(new { success = false, message = "Email 格式不正確" });

            if (!Regex.IsMatch(model.UserPassword, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W_]).{8,}$"))
                return Json(new { success = false, message = "密碼需含大小寫英文、數字與特殊符號且至少8碼" });

            string normalizedEmail = model.UserEmail.Trim().ToLower();
            if (_context.Users.Any(u => u.UserEmail.ToLower() == normalizedEmail)) return Json(new { success = false, message = "電子郵件已註冊"  });

            //密碼加密
            string salt = PasswordHelper.GenerateSalt();
            string hashedPassword = PasswordHelper.ComputeSha256Hash(model.UserPassword + salt);

            //生成一個user實體
            var newUser = new User
            {
                UserEmail = normalizedEmail,
                UserPassword = hashedPassword,
                PasswordSalt = salt,
                UserName = model.UserName,
                RoleId = 3,
                IsActive =true,
                CreateDate = DateTime.Now,
                UpdateDate = DateTime.Now,
            };
            //寫入資料庫
            _context.Add(newUser);
            _context.SaveChanges();

            //回傳成功訊息
            return Json(new { success = true, message = "註冊成功" });
        }

    }
}
