using System;
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Generators;

namespace CuaHangTienLoi.Common
{
    /// <summary>
    /// Xử lý băm và kiểm tra mật khẩu sử dụng chuẩn Bcrypt (đáp ứng NFR02: không lưu mật khẩu dạng rõ)
    /// </summary>
    public static class PasswordHasher
    {
        private const int CostFactor = 10;

        /// <summary>
        /// Tạo chuỗi băm Bcrypt kèm muối ngẫu nhiên 16 bytes
        /// </summary>
        public static string HashPassword(string rawPassword)
        {
            if (string.IsNullOrEmpty(rawPassword))
                throw new ArgumentNullException(nameof(rawPassword));

            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return OpenBsdBCrypt.Generate(rawPassword.ToCharArray(), salt, CostFactor);
        }

        /// <summary>
        /// Kiểm tra mật khẩu thô với mã băm đã lưu
        /// </summary>
        public static bool VerifyPassword(string rawPassword, string hashedPassword)
        {
            if (string.IsNullOrEmpty(rawPassword) || string.IsNullOrEmpty(hashedPassword))
                return false;

            try
            {
                return OpenBsdBCrypt.CheckPassword(hashedPassword, rawPassword.ToCharArray());
            }
            catch
            {
                return false;
            }
        }
    }
}
