using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        //非同步
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            ViewBag.UserName = HttpContext.Session.GetString("UserName");
            ViewBag.UserId = HttpContext.Session.GetInt32("UserId");
            ViewBag.Email = HttpContext.Session.GetString("Email");

            var products = await _context.Products.ToListAsync();

            return View(products);
        }
    }
}
