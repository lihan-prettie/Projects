using Microsoft.AspNetCore.Mvc;

namespace Shopping.Controllers
{
    public class ProductController : Controller
    {
        public IActionResult Index()
        {
            //ViewBag.UserName = HttpContext.Session.GetString("UserName");
            //ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            //ViewBag.Email = HttpContext.Session.GetString("Email");
            
            return View();
        }

    }
}
