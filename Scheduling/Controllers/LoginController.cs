using Microsoft.AspNetCore.Mvc;
using Scheduling.Models;
using Scheduling.Models.ViewModels;

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
            var user = _context.Users.FirstOrDefault(u => u.UserEmail == model.UserEmail);
            if (user == null) {
                return Json(new { success = false, message = "使用者不存在" });
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
    }
}
