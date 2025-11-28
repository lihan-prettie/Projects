using Microsoft.AspNetCore.Mvc;
using Scheduling.Models;
using Scheduling.Models.ViewModels;
using Scheduling.Helpers;

namespace Scheduling.Controllers
{
    public class LoginController : Controller
    {
        private readonly SchedulingContext _context;
        public LoginController(SchedulingContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        //POST: Login/LoginCheck
        [HttpPost]
        public IActionResult LoginCheck(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "輸入資料不完整" });
            }
            var user = _context.Users.FirstOrDefault(u => u.UserEmail == model.UserEmail && u.IsActive==true);
            if (user == null) {
                return Json(new { success = false, message = "使用者不存在" });
            }

            string salt = user.PasswordSalt ?? "";
            string storeHash = user.UserPassword;

            bool isValid = PasswordHelper.VerifyPassword(model.UserPassword, salt, storeHash);
            if (!isValid)
            {
                return Json(new{ success=false,message="密碼錯誤"});
            }

            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.UserName);
            HttpContext.Session.SetInt32("RoleId", user.RoleId);
            string redirectUrl = user.RoleId switch
            {
                1 => "/Dashboard/Boss",
                2 => "/Dashboard/Manager",
                3 => "/Dashboard/Employee",
                _ => "/"
            };
            return Json(new { success = true, redirectUrl });
        }

        // ✅ 登出功能（Logout Action）
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // 清除使用者的 Session
            HttpContext.Session.Clear();

            // 可選：顯示提示訊息或導向登入頁
            TempData["LogoutMessage"] = "您已成功登出。";

            return RedirectToAction("Index", "Home");
        }
    }
}
