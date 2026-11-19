using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace CuaHangTienLoi.Common
{
    /// <summary>
    /// Các phương thức mở rộng quản lý Session kiểu dữ liệu phức tạp
    /// </summary>
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value)
        {
            session.SetString(key, JsonSerializer.Serialize(value));
        }

        public static T? GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default : JsonSerializer.Deserialize<T>(value);
        }
    }
}
