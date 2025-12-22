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

        public IActionResult OrderPage()
        {
            return View();
        }
    }
}