using Microsoft.AspNetCore.Mvc;

namespace Shopping.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult Checkout()
        {
            return View();
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}
