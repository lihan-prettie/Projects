using Microsoft.AspNetCore.Mvc;

namespace Shopping.Controllers
{
    public class MemberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Sigup()
        {
            return View();
        }
    }
}
