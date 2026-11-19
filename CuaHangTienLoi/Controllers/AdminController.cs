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
    [AuthorizePermission("QUAN_LY")]
    public class AdminController : Controller
    {
        private readonly BaoCaoDao _baoCaoDao;
        private readonly HangHoaDao _hangHoaDao;
        private readonly NhanVienDao _nhanVienDao;
        private readonly TaiKhoanDao _taiKhoanDao;
        private readonly KhuyenMaiDao _khuyenMaiDao;
        private readonly KhachHangDao _khachHangDao;
        private readonly NhatKyDao _nhatKyDao;
        private readonly IDoiTraService _doiTraService;
        private readonly IKhoService _khoService;
        private readonly BanHangDao _banHangDao;
        private readonly IExcelExportService _excelService;

        public AdminController(
            BaoCaoDao baoCaoDao,
            HangHoaDao hangHoaDao,
            NhanVienDao nhanVienDao,
            TaiKhoanDao taiKhoanDao,
            KhuyenMaiDao khuyenMaiDao,
            KhachHangDao khachHangDao,
            NhatKyDao nhatKyDao,
            IDoiTraService doiTraService,
            IKhoService khoService,
            BanHangDao banHangDao,
            IExcelExportService excelService)
        {
            _baoCaoDao = baoCaoDao;
            _hangHoaDao = hangHoaDao;
            _nhanVienDao = nhanVienDao;
            _taiKhoanDao = taiKhoanDao;
            _khuyenMaiDao = khuyenMaiDao;
            _khachHangDao = khachHangDao;
            _nhatKyDao = nhatKyDao;
            _doiTraService = doiTraService;
            _khoService = khoService;
            _banHangDao = banHangDao;
            _excelService = excelService;
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var stats = _baoCaoDao.GetDashboardStats();
            stats.DanhSachSapHetHan = _khoService.GetNearExpiryLots(30);
            stats.HoaDonGanDay = _banHangDao.GetRecentInvoices(10);
            return View(stats);
        }

        [HttpGet]
        public IActionResult NhanVien()
        {
            ViewBag.Accounts = _taiKhoanDao.GetAll();
            ViewBag.Roles = _nhanVienDao.GetAllVaiTro();
            var staffList = _nhanVienDao.GetAll();
            return View(staffList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoNhanVien(NhanVien nv, string tenDangNhap, string matKhau, string maVaiTro)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                int maNV = _nhanVienDao.Insert(nv);
                string hash = PasswordHasher.HashPassword(matKhau);
                _taiKhoanDao.CreateAccount(maNV, tenDangNhap.Trim(), hash, maVaiTro);

                if (user != null)
                {
                    _nhatKyDao.InsertLog(new NhatKyKiemToan
                    {
                        MaTK = user.MaTK,
                        HanhDong = "TAO_NHAN_VIEN",
                        DoiTuong = "NhanVien",
                        MaDoiTuong = maNV.ToString(),
                        DiaChiIP = ip,
                        ThietBi = userAgent,
                        GiaTriMoi = $"Thêm nhân viên {nv.HoTen}, tài khoản {tenDangNhap}, vai trò {maVaiTro}"
                    });
                }

                TempData["SuccessMessage"] = $"Tạo nhân viên {nv.HoTen} thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("NhanVien");
        }

        [HttpPost]
        public IActionResult KhoaMoTaiKhoan(int id, bool moKhoa)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                _taiKhoanDao.SetLockStatus(id, moKhoa);

                if (user != null)
                {
                    _nhatKyDao.InsertLog(new NhatKyKiemToan
                    {
                        MaTK = user.MaTK,
                        HanhDong = moKhoa ? "MO_KHOA_TAI_KHOAN" : "KHOA_TAI_KHOAN",
                        DoiTuong = "TaiKhoan",
                        MaDoiTuong = id.ToString(),
                        DiaChiIP = ip,
                        ThietBi = userAgent,
                        GiaTriMoi = $"Quản lý {(moKhoa ? "MỞ KHÓA" : "KHÓA")} tài khoản #{id}"
                    });
                }

                TempData["SuccessMessage"] = $"Đã {(moKhoa ? "mở khóa" : "khóa")} tài khoản #{id} thành công.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("NhanVien");
        }

        [HttpGet]
        public IActionResult HangHoa(int? maNhom = null, string? search = null)
        {
            ViewBag.Categories = _hangHoaDao.GetAllCategories();
            ViewBag.SelectedMaNhom = maNhom;
            ViewBag.Search = search;
            var list = _hangHoaDao.GetAll(maNhom, search, true);
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoHangHoa(HangHoa h)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                int maHang = _hangHoaDao.Insert(h);
                if (user != null)
                {
                    _nhatKyDao.InsertLog(new NhatKyKiemToan
                    {
                        MaTK = user.MaTK,
                        HanhDong = "TAO_HANG_HOA",
                        DoiTuong = "HangHoa",
                        MaDoiTuong = maHang.ToString(),
                        DiaChiIP = ip,
                        ThietBi = userAgent,
                        GiaTriMoi = $"Thêm sản phẩm mới: {h.TenHang}, Mã vạch: {h.MaVach}, Giá bán: {h.GiaBan:N0}đ"
                    });
                }
                TempData["SuccessMessage"] = $"Thêm sản phẩm '{h.TenHang}' thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("HangHoa");
        }

        [HttpPost]
        public IActionResult CapNhatGia(int id, decimal giaMoi)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                var h = _hangHoaDao.GetById(id);
                decimal oldPrice = h?.GiaBan ?? 0;
                _hangHoaDao.UpdatePrice(id, giaMoi);

                if (user != null)
                {
                    _nhatKyDao.InsertLog(new NhatKyKiemToan
                    {
                        MaTK = user.MaTK,
                        HanhDong = "SUA_GIA_BAN",
                        DoiTuong = "HangHoa",
                        MaDoiTuong = id.ToString(),
                        DiaChiIP = ip,
                        ThietBi = userAgent,
                        GiaTriCu = $"{oldPrice:N0}đ",
                        GiaTriMoi = $"{giaMoi:N0}đ"
                    });
                }

                return Json(new { success = true, message = "Cập nhật giá bán thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult KhuyenMai()
        {
            ViewBag.Products = _hangHoaDao.GetAll();
            var list = _khuyenMaiDao.GetAll();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TaoKhuyenMai(KhuyenMai km, List<int> selectedProducts)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                km.DanhSachMaHang = selectedProducts ?? new List<int>();
                int maKM = _khuyenMaiDao.Insert(km);

                if (user != null)
                {
                    _nhatKyDao.InsertLog(new NhatKyKiemToan
                    {
                        MaTK = user.MaTK,
                        HanhDong = "TAO_KHUYEN_MAI",
                        DoiTuong = "KhuyenMai",
                        MaDoiTuong = maKM.ToString(),
                        DiaChiIP = ip,
                        ThietBi = userAgent,
                        GiaTriMoi = $"Tạo CTKM '{km.TenKM}': Giảm {km.GiaTri} ({km.LoaiGiam}) cho {km.DanhSachMaHang.Count} sản phẩm"
                    });
                }

                TempData["SuccessMessage"] = "Tạo chương trình khuyến mãi thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("KhuyenMai");
        }

        [HttpPost]
        public IActionResult KetThucKhuyenMai(int id)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                _khuyenMaiDao.EndPromotionEarly(id);
                if (user != null)
                {
                    _nhatKyDao.InsertLog(new NhatKyKiemToan
                    {
                        MaTK = user.MaTK,
                        HanhDong = "KET_THUC_KHUYEN_MAI",
                        DoiTuong = "KhuyenMai",
                        MaDoiTuong = id.ToString(),
                        DiaChiIP = ip,
                        ThietBi = userAgent,
                        GiaTriMoi = $"Kết thúc sớm CTKM #{id}"
                    });
                }
                TempData["SuccessMessage"] = $"Đã kết thúc sớm chương trình khuyến mãi #{id}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("KhuyenMai");
        }

        [HttpGet]
        public IActionResult KhachHang()
        {
            var list = _khachHangDao.GetAll();
            return View(list);
        }

        [HttpGet]
        public IActionResult DoiTra()
        {
            var list = _doiTraService.GetAllReturns();
            return View(list);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DuyetDoiTra(int id, string phuongThucHoan, string? maGiaoDichHoan)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                _doiTraService.DuyetPhieuTra(id, user.MaNV, user.MaTK, phuongThucHoan, maGiaoDichHoan, ip, userAgent);
                TempData["SuccessMessage"] = $"Đã duyệt phiếu trả hàng #{id} và cấp số phiếu hoàn trả thành công!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("DoiTra");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult TuChoiDoiTra(int id)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                _doiTraService.TuChoiPhieuTra(id, user.MaNV, user.MaTK, ip, userAgent);
                TempData["SuccessMessage"] = $"Đã từ chối phiếu trả hàng #{id}.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("DoiTra");
        }

        [HttpGet]
        public IActionResult NhatKyKiemToan(string? search = null)
        {
            ViewBag.Search = search;
            var list = _nhatKyDao.GetRecentLogs(200, search);
            return View(list);
        }
    }
}
