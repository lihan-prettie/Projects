using Microsoft.AspNetCore.Mvc;
using Scheduling.Models;

namespace Scheduling.Controllers
{
    public class SignupController : Controller
    {
        private readonly SchedulingContext _context;
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

    }
}
