using System.Security.Cryptography;
using System.Text;

namespace Scheduling.Helpers
{
    public static class PasswordHelper
    {
        //一段字串（例如密碼 + Salt）轉成 SHA256 雜湊
        public static string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                //將輸入轉成位元組後計算雜湊值
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                //產生字串容器以拼接字串
                StringBuilder sb = new StringBuilder();

                //用迴圈將位元組轉成十六進位
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
        public static bool VerifyPassword(string inputPassword, string salt, string storedHash)
        {
            //輸入的密碼加上salt生成雜湊結果
            string hashInput = ComputeSha256Hash(inputPassword + salt);
            //比對
            return storedHash == hashInput;
        }
        public static string GenerateSalt()
        {
            byte[] bytes = new byte[16];

            //RandomNumberGenerator可以產生安全加密等級的亂數
            using (var rng = RandomNumberGenerator.Create())
            {
                //用亂數填滿整個陣列
                rng.GetBytes(bytes);
            }
            //轉成64位元組字串儲存
            return Convert.ToBase64String(bytes);
        }

    }
}