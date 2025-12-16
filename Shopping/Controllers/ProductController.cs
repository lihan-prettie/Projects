using Microsoft.AspNetCore.Mvc;
using Shopping.Models;

namespace Shopping.Controllers
{
    public class ProductController : Controller
    {
        private readonly ShoppingContext _context;
        public ProductController(ShoppingContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Email = HttpContext.Session.GetString("Email");

            var products = _context.Products.ToList();

            return View(products);
        }
    }
}
