using System;
using Microsoft.AspNetCore.Mvc;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;
using CuaHangTienLoi.Services;

namespace CuaHangTienLoi.Controllers
{
    public class AuthController : Controller
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // Nếu đã đăng nhập thì điều hướng về trang tương ứng
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user != null)
            {
                return RedirectBasedOnRole(user);
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            var result = _authService.Authenticate(model.TenDangNhap, model.MatKhau, ip, userAgent);

            if (!result.Success || result.User == null)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            // Lưu thông tin phiên đăng nhập Session
            HttpContext.Session.SetObject("CurrentUser", result.User);

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectBasedOnRole(result.User);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user != null)
            {
                string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
                string userAgent = Request.Headers.UserAgent.ToString();
                _authService.WriteAuditLog(user.MaTK, "DANG_XUAT", "TaiKhoan", user.TenDangNhap, ip, userAgent, null, "Người dùng đã đăng xuất");
            }

            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DoiMatKhau()
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login");

            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DoiMatKhau(ChangePasswordViewModel model)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login");

            if (!ModelState.IsValid) return View(model);

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            var result = _authService.ChangePassword(user.MaTK, model.MatKhauCu, model.MatKhauMoi, ip, userAgent);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectBasedOnRole(user);
        }

        private IActionResult RedirectBasedOnRole(UserSessionModel user)
        {
            return user.MaVaiTro switch
            {
                "QUAN_LY" => RedirectToAction("Dashboard", "Admin"),
                "NHAN_VIEN_KHO" => RedirectToAction("Index", "Kho"),
                "THU_NGAN" => RedirectToAction("Index", "Pos"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
