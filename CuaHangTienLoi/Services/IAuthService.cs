using System.Threading.Tasks;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public interface IAuthService
    {
        (bool Success, string Message, UserSessionModel? User) Authenticate(string username, string password, string ip, string userAgent);
        (bool Success, string Message) ChangePassword(int maTK, string oldPassword, string newPassword, string ip, string userAgent);
        void WriteAuditLog(int maTK, string action, string objectType, string objectId, string ip, string device, string? oldVal = null, string? newVal = null);
    }
}
