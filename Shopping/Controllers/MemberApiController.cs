using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Shopping.Models;
using Shopping.Models.DTOs;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Shopping.Controllers
{
    [Route("api/members")]
    [ApiController]
    public class MemberApiController : ControllerBase
    {
        private readonly ShoppingContext _content;
        private readonly IConfiguration _configuration;
        public MemberApiController(ShoppingContext content, IConfiguration configuration)
        {
            _content = content;
            _configuration = configuration;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> Signup([FromBody] SignupDTO dto)
        {
            if ( await _content.Members.AnyAsync(a => a.Email == dto.Email)) return Conflict(new { message = "電子郵件已註冊" });

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
                RegisterDate = DateTime.UtcNow,
                PasswordSalt = salt,
            };
            //寫入資料庫
            await _content.AddAsync(member);
            await _content.SaveChangesAsync();

            return StatusCode(201, new { message = "註冊成功" });
        }

        [HttpGet("check-email")]
        public async Task<IActionResult> CheckEmail([FromQuery]string email)
        {
            bool exist = await _content.Members.AnyAsync(a => a.Email == email);
            
                return Ok(new { exist });
            
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            var user = await _content.Members.FirstOrDefaultAsync(f => f.Email == dto.Email);
            if (user == null) return Unauthorized(new { message = "帳號或密碼錯誤" });

            var hash = HashPassword(dto.Password, user.PasswordSalt ??"");
            if (hash != user.PasswordHash) return Unauthorized(new { message = "帳號或密碼錯誤"});

            var token = GenerateJwt(user);
            return Ok(new { token, expireMin = int.Parse(_configuration["Jwt:ExpireMinutes"]!)});

        }

        //密碼加密
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

        //Json Web Token
        private string GenerateJwt(Member user)
        {

            //新增claim以儲存身分基本資訊
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.MemberId.ToString()),
                new Claim(ClaimTypes.Email,user.Email),
                new Claim("UserName",user.UserName)
            };

            //產生加密用金鑰
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new Exception("JWT Key未設定")));

            //用key搭配HmacSha256(HS256)簽章演算法
            var creds = new SigningCredentials(key,SecurityAlgorithms.HmacSha256);

            //建立jwt本體(header、payload和signature)
            var token = new JwtSecurityToken(
                    audience: _configuration["Jwt:Audience"],
                    issuer: _configuration["Jwt:Issuer"],
                    claims: claims,
                    expires: DateTime.UtcNow.AddMinutes(int.Parse(_configuration["Jwt:ExpireMinutes"])),
                    signingCredentials:creds
            );

            //回傳結果
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

    }
}
