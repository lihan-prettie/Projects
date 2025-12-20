using Microsoft.AspNetCore.Mvc;
using Shopping.Helpers;
using Shopping.Models;
using Shopping.Models.DTOs;

namespace Shopping.Controllers
{
    
    //改成遵照RESTful api格式
    public class CartController : Controller
    {
        private readonly ShoppingContext _context;
        public CartController(ShoppingContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObject<List<CartItemSessionDTO>>("Cart") ?? new List<CartItemSessionDTO>();
            return View(cart);
        }

        [HttpPost]
        public IActionResult AddToCart(int ProductId,int Quantity)
        {
            //檢查商品
            var product = _context.Products.FirstOrDefault(p => p.ProductId == ProductId);
            if (product == null)
            {
                return NotFound();
            }

            //取session
            var cart = HttpContext.Session.GetObject<List<CartItemSessionDTO>>("Cart") ?? new List<CartItemSessionDTO>();

            //檢查是否存在
            var existItem = cart.FirstOrDefault(p => p.ProductId == ProductId);
            if (existItem != null)
            {
                existItem.Quantity += Quantity;
            }
            else
            {
                cart.Add(new CartItemSessionDTO 
                {
                    ProductId = ProductId,
                    ProductName = product.ProductName,
                    Price = product.Price,
                    Quantity = Quantity
                });
            }

            //存回session
            HttpContext.Session.SetObject("Cart",cart);

            return RedirectToAction("Index","Cart");
        }

        //[HttpPost]
        //public IActionResult UpdateQuantity(int ProductId, int Quantity)
        //{
        //    var cart = HttpContext.Session.GetObject<List<CartItemSessionDTO>>("Cart") ?? new List<CartItemSessionDTO>();

        //    if(cart.)
        //}
    }
}