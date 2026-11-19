using Microsoft.AspNetCore.Mvc;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null)
            {
                return RedirectToAction("Login", "Auth");
            }

            return user.MaVaiTro switch
            {
                "QUAN_LY" => RedirectToAction("Dashboard", "Admin"),
                "NHAN_VIEN_KHO" => RedirectToAction("Index", "Kho"),
                "THU_NGAN" => RedirectToAction("Index", "Pos"),
                _ => RedirectToAction("Login", "Auth")
            };
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
