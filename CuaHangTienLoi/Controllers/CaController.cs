using System;
using Microsoft.AspNetCore.Mvc;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Filters;
using CuaHangTienLoi.Models;
using CuaHangTienLoi.Services;

namespace CuaHangTienLoi.Controllers
{
    [AuthorizePermission("MO_CHOT_CA")]
    public class CaController : Controller
    {
        private readonly ICaService _caService;
        private readonly NhanVienDao _nhanVienDao;

        public CaController(ICaService caService, NhanVienDao nhanVienDao)
        {
            _caService = caService;
            _nhanVienDao = nhanVienDao;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            ViewBag.CurrentUser = user;
            var list = _caService.GetAllShifts();
            return View(list);
        }

        [HttpGet]
        public IActionResult MoCa()
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            // Nếu người này đã có ca mở, thông báo và cho chuyển vào POS
            if (user.CurrentMaCa.HasValue)
            {
                TempData["InfoMessage"] = $"Bạn hiện đang có ca làm việc #{user.CurrentMaCa} tại {user.CurrentTenQuay}.";
                return RedirectToAction("Index", "Pos");
            }

            var quays = _nhanVienDao.GetAllQuay();
            var model = new MoCaViewModel
            {
                DanhSachQuay = quays,
                TienDauCa = 1000000 // Mặc định 1 triệu tiền lẻ đầu ca
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MoCa(MoCaViewModel model)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                model.DanhSachQuay = _nhanVienDao.GetAllQuay();
                return View(model);
            }

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                int maCa = _caService.MoCa(model.MaQuay, user.MaNV, model.TienDauCa, user.MaTK, ip, userAgent);

                // Cập nhật lại session
                var quays = _nhanVienDao.GetAllQuay();
                var selectedQuay = quays.Find(q => q.MaQuay == model.MaQuay);

                user.CurrentMaCa = maCa;
                user.CurrentMaQuay = model.MaQuay;
                user.CurrentTenQuay = selectedQuay?.TenQuay ?? $"Quầy {model.MaQuay}";
                HttpContext.Session.SetObject("CurrentUser", user);

                TempData["SuccessMessage"] = $"Mở ca #{maCa} tại {user.CurrentTenQuay} thành công!";
                return RedirectToAction("Index", "Pos");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                model.DanhSachQuay = _nhanVienDao.GetAllQuay();
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult DongCa(int? id = null)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            int maCaToClose = id ?? user.CurrentMaCa ?? 0;
            if (maCaToClose == 0)
            {
                TempData["WarningMessage"] = "Không tìm thấy ca làm việc cần đóng.";
                return RedirectToAction("Index");
            }

            try
            {
                var model = _caService.GetShiftCloseInfo(maCaToClose);
                model.LaChotThay = (id.HasValue && id.Value != user.CurrentMaCa);
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DongCa(DongCaViewModel model)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                _caService.DongCa(model, user.MaNV, user.MaTK, ip, userAgent);

                // Nếu thu ngân tự đóng ca của mình thì xóa CurrentMaCa trong session
                if (user.CurrentMaCa == model.MaCa)
                {
                    user.CurrentMaCa = null;
                    user.CurrentMaQuay = null;
                    user.CurrentTenQuay = null;
                    HttpContext.Session.SetObject("CurrentUser", user);
                }

                TempData["SuccessMessage"] = $"Đóng ca #{model.MaCa} thành công và đã tổng hợp Báo cáo ca!";
                return RedirectToAction("BaoCaoCa", new { id = model.MaCa });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult BaoCaoCa(int id)
        {
            var bc = _caService.GetBaoCaoCa(id);
            if (bc == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy báo cáo ca làm việc này.";
                return RedirectToAction("Index");
            }
            return View(bc);
        }
    }
}
