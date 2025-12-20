using Microsoft.AspNetCore.Mvc;
namespace Shopping.Controllers
{
    
    //改成遵照RESTful api格式
    public class MemberController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}