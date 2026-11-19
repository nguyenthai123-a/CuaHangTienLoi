using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Filters;
using CuaHangTienLoi.Models;
using CuaHangTienLoi.Services;

namespace CuaHangTienLoi.Controllers
{
    [AuthorizePermission("KHO_LAP")]
    public class KhoController : Controller
    {
        private readonly IKhoService _khoService;
        private readonly HangHoaDao _hangHoaDao;
        private readonly KhoDao _khoDao;
        private readonly IExcelExportService _excelService;

        public KhoController(IKhoService khoService, HangHoaDao hangHoaDao, KhoDao khoDao, IExcelExportService excelService)
        {
            _khoService = khoService;
            _hangHoaDao = hangHoaDao;
            _khoDao = khoDao;
            _excelService = excelService;
        }

        [HttpGet]
        public IActionResult Index(int? maHang = null)
        {
            var lots = _khoService.GetAllLots(maHang);
            var nearExpiry = _khoService.GetNearExpiryLots(30);
            ViewBag.NearExpiryLots = nearExpiry;
            ViewBag.Products = _hangHoaDao.GetAll();
            ViewBag.SelectedMaHang = maHang;
            return View(lots);
        }

        [HttpGet]
        public IActionResult NhapKho()
        {
            ViewBag.Suppliers = _hangHoaDao.GetAllSuppliers();
            ViewBag.Products = _hangHoaDao.GetAll();
            return View(new NhapKhoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult NhapKho(NhapKhoViewModel model)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            if (!ModelState.IsValid)
            {
                ViewBag.Suppliers = _hangHoaDao.GetAllSuppliers();
                ViewBag.Products = _hangHoaDao.GetAll();
                return View(model);
            }

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                int maPN = _khoService.NhapKho(model, user.MaNV, user.MaTK, ip, userAgent);
                TempData["SuccessMessage"] = $"Tạo phiếu nhập kho #{maPN} thành công! Đã khởi tạo các lô hàng mới.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Suppliers = _hangHoaDao.GetAllSuppliers();
                ViewBag.Products = _hangHoaDao.GetAll();
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult XuatKho()
        {
            ViewBag.Products = _hangHoaDao.GetAll();
            ViewBag.Suppliers = _hangHoaDao.GetAllSuppliers();
            ViewBag.Lots = _khoService.GetAllLots(null);
            return View(new XuatKhoViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult XuatKho(XuatKhoViewModel model)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                if (model.LoaiXuat == "LEN_QUAY")
                {
                    // Xuất lên quầy theo nguyên tắc FEFO tự động
                    foreach (var item in model.ChiTiet)
                    {
                        if (item.SoLuong > 0)
                        {
                            _khoService.XuatLenQuay(item.MaHang, item.SoLuong, user.MaNV, user.MaTK, ip, userAgent);
                        }
                    }
                    TempData["SuccessMessage"] = "Xuất hàng lên quầy thành công theo chuẩn thuật toán FEFO!";
                }
                else if (model.LoaiXuat == "HUY")
                {
                    // Lập phiếu xuất hủy chờ duyệt
                    int maPX = _khoService.TaoPhieuXuatHuy(model, user.MaNV, user.MaTK, ip, userAgent);
                    TempData["SuccessMessage"] = $"Đã lập phiếu xuất hủy #{maPX}. Vui lòng chờ Quản lý phê duyệt.";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.Products = _hangHoaDao.GetAll();
                ViewBag.Suppliers = _hangHoaDao.GetAllSuppliers();
                ViewBag.Lots = _khoService.GetAllLots(null);
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult KiemKe()
        {
            ViewBag.PhieuList = _khoDao.GetAllPhieuKiemKe();
            ViewBag.Lots = _khoService.GetAllLots(null);
            return View(new KiemKeViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult KiemKe(KiemKeViewModel model)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                int maKK = _khoService.TaoPhieuKiemKe(model, user.MaNV, user.MaTK, ip, userAgent);
                TempData["SuccessMessage"] = $"Đã tạo phiếu kiểm kê #{maKK}. Chờ Quản lý phê duyệt điều chỉnh tồn.";
                return RedirectToAction("KiemKe");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                ViewBag.PhieuList = _khoDao.GetAllPhieuKiemKe();
                ViewBag.Lots = _khoService.GetAllLots(null);
                return View(model);
            }
        }

        [HttpPost]
        [AuthorizePermission("KIEM_KE_DUYET")]
        public IActionResult DuyetKiemKe(int id, bool dongY)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                _khoService.DuyetPhieuKiemKe(id, user.MaNV, user.MaTK, dongY, ip, userAgent);
                TempData["SuccessMessage"] = $"Đã {(dongY ? "phê duyệt điều chỉnh" : "từ chối")} phiếu kiểm kê #{id}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("KiemKe");
        }

        [HttpGet]
        public IActionResult TheKho(int? maHang = null, int? maLo = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            ViewBag.Products = _hangHoaDao.GetAll();
            ViewBag.Lots = _khoService.GetAllLots(null);
            ViewBag.SelectedMaHang = maHang;
            ViewBag.SelectedMaLo = maLo;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");

            var history = _khoService.GetTheKhoHistory(maHang, maLo, fromDate, toDate);
            return View(history);
        }

        [HttpGet]
        public IActionResult XuatExcel()
        {
            var lots = _khoService.GetAllLots(null);
            var excelBytes = _excelService.ExportInventory(lots);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"TonKho_FEFO_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }
    }
}
