using Microsoft.AspNetCore.Mvc;
using Shopping.Helpers;
using Shopping.Models.DTOs;

namespace Shopping.Controllers
{
    
    public class CartController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}