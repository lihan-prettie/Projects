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
                //用迴圈將位元組轉成十六進位

                //產生字串容器以拼接字串
                StringBuilder sb = new StringBuilder();
                for ()
                {

                }

            }
        }
    }
}