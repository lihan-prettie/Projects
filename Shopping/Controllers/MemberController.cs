using Microsoft.AspNetCore.Mvc;
using Shopping.Models;
namespace Shopping.Controllers
{
    public class MemberController : Controller
    {
        private readonly ShoppingContext _content;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login()
        {
            
        }

        [HttpPost]
        public IActionResult Sigup()
        {
            return View();
        }
    }
}
