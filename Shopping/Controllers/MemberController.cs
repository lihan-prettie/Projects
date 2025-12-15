using Microsoft.AspNetCore.Mvc;
using Shopping.Models;
using Shopping.Models.DTOs;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
namespace Shopping.Controllers
{
    public class MemberController : Controller
    {
        private readonly ShoppingContext _content;
        public MemberController(ShoppingContext content)
        {
            _content = content;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Signup(SignupDTO dto)
        {
            if (!ModelState.IsValid) return Json(new { success=false,message="註冊資訊未填寫完整"});
            if (!new EmailAddressAttribute().IsValid(dto.Email)) return Json(new { success=false,message= "電子郵件格式不正確" });
            if (!Regex.IsMatch(dto.Password, @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[\W]).{8,15}$")) return Json(new {success=false,message = "密碼格式不合"});
            if (_content.Members.Any(a => a.Email == dto.Email)) return Json(new { success=false,message = "電子郵件已註冊"});

            //生成位元的salt
            var saltBytes = RandomNumberGenerator.GetBytes(16);
            //將位元的salt轉為string
            var salt = Convert.ToBase64String(saltBytes);
            //加密
            string passwordHash = HashPassword(dto.Password, salt);
            //創一個實體模型
            var member = new Member
            {
                UserName = dto.UserName,
                Email = dto.Email,
                PasswordHash = passwordHash,
                RegisterDate = DateTime.Now,
                PasswordSalt = salt,
            };
            //寫入資料庫
            _content.Add(member);
            _content.SaveChanges();

            return Json(new { success=true,message="註冊成功" });
        }

        [HttpPost]
        public IActionResult CheckEmail(string email)
        {
            bool exist = _content.Members.Any(a => a.Email == email);
            if (exist)
            {
                return Json(new { success = true, response = "電子郵件已註冊" });
            }
            else
            {
                return Json(new { success = false, response = "電子郵件未註冊" });
            }
        }

        [HttpPost]
        public IActionResult Login()
        {

        }

        private string HashPassword(string password, string salt)
        {
            //將字串(密碼加上鹽值的組合)轉成byte以進行加密操作
            var combine = Encoding.UTF8.GetBytes(password + salt);

            //新增一個雜湊物件
            using var sha256 = SHA256.Create();

            //進行雜湊加密
            var hash = sha256.ComputeHash(combine);

            //將計算出的值轉回字串回傳
            return Convert.ToBase64String(hash);
        }
    }
}