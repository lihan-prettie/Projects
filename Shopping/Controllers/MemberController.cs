using Microsoft.AspNetCore.Mvc;
using Shopping.Models;
using Shopping.Models.DTOs;
using System.Security.Cryptography;
using System.Text;
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
        public IActionResult Sigup(SignupDTO dto)
        {
            //生成位元的salt
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            //將位元的salt轉為string
            var salt = Convert.ToBase64String(saltBytes);
            //加密
            string passwordHash = HashPassword(dto.Password,salt);
            //創一個實體模型
            var member = new Member 
            {
                UserName=dto.UserName,
                Email=dto.Email,
                PasswordHash=passwordHash,
                RegisterDate=DateTime.Now
            };
             //寫入資料庫
             _content.Add(member);
             _content.SaveChanges();

            return RedirectToAction("Login");
        }

        [HttpPost]
        public IActionResult Login()
        {

        }
        
        private string HashPassword(string password,string salt)
        {
            //將字串(密碼加上鹽值的組合)轉成byte以進行加密操作
            var combine = Encoding.UTF8.GetBytes(password+salt);

            //新增一個雜湊物件
            using var sha256 = SHA256.Create();

            //進行雜湊加密
            var hash = sha256.ComputeHash(combine);

            //將計算出的值轉回字串回傳
            return Convert.ToBase64String(hash);
        }
    }
}