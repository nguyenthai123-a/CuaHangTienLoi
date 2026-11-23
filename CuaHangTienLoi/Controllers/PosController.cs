using System;
using Microsoft.AspNetCore.Mvc;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Filters;
using CuaHangTienLoi.Models;
using CuaHangTienLoi.Services;

namespace CuaHangTienLoi.Controllers
{
    [AuthorizePermission("BAN_HANG")]
    public class PosController : Controller
    {
        private readonly IPosService _posService;
        private readonly ICaService _caService;

        public PosController(IPosService posService, ICaService caService)
        {
            _posService = posService;
            _caService = caService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null) return RedirectToAction("Login", "Auth");

            // Kiểm tra xem thu ngân đã mở ca chưa
            if (!user.CurrentMaCa.HasValue)
            {
                TempData["WarningMessage"] = "Vui lòng mở ca làm việc và khai báo tiền đầu ca trước khi tiến hành bán hàng.";
                return RedirectToAction("MoCa", "Ca");
            }

            ViewBag.CurrentUser = user;
            var products = _posService.SearchProducts(null);
            return View(products);
        }

        [HttpGet]
        public IActionResult ScanBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
            {
                return Json(new { success = false, message = "Mã vạch không hợp lệ." });
            }

            var item = _posService.ScanBarcode(barcode);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm với mã vạch này hoặc sản phẩm đã ngừng kinh doanh." });
            }

            if (item.TonQuayHienTai <= 0)
            {
                return Json(new { success = false, message = $"Sản phẩm '{item.TenHang}' hiện đã hết hàng tồn trên quầy." });
            }

            return Json(new { success = true, data = item });
        }

        [HttpGet]
        public IActionResult Search(string q)
        {
            var items = _posService.SearchProducts(q);
            return Json(new { success = true, data = items });
        }

        [HttpGet]
        public IActionResult Customer(string phone)
        {
            var customer = _posService.FindCustomerByPhone(phone);
            if (customer == null)
            {
                return Json(new { success = false, message = "Chưa có thông tin khách hàng này." });
            }
            return Json(new { success = true, data = customer });
        }

        [HttpPost]
        public IActionResult CreateCustomer([FromBody] KhachHang model)
        {
            try
            {
                var customer = _posService.CreateCustomer(model.HoTen, model.SoDienThoai);
                return Json(new { success = true, data = customer });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult Checkout([FromBody] PosCheckoutRequest request)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            if (user == null)
            {
                return Json(new { success = false, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại." });
            }

            if (!user.CurrentMaCa.HasValue)
            {
                return Json(new { success = false, message = "Bạn chưa có ca làm việc nào đang mở. Vui lòng mở ca trước khi thanh toán." });
            }

            request.MaCa = user.CurrentMaCa.Value;
            string ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            string userAgent = Request.Headers.UserAgent.ToString();

            try
            {
                var result = _posService.Checkout(request, user.MaNV, user.MaTK, ip, userAgent);
                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public IActionResult InHoaDon(int id)
        {
            var hd = _posService.GetInvoice(id);
            if (hd == null)
            {
                return NotFound("Không tìm thấy hóa đơn.");
            }
            return View(hd);
        }

        [HttpGet]
        public IActionResult GetInvoicesByCa(int? maCa)
        {
            var user = HttpContext.Session.GetObject<UserSessionModel>("CurrentUser");
            int targetMaCa = maCa ?? user?.CurrentMaCa ?? 0;
            if (targetMaCa <= 0)
            {
                return Json(new { success = false, message = "Không xác định được ca làm việc." });
            }

            var invoices = _posService.GetInvoicesByCa(targetMaCa);
            return Json(new { success = true, data = invoices, maCa = targetMaCa });
        }

        [HttpGet]
        public IActionResult GetInvoiceDetail(int id)
        {
            var hd = _posService.GetInvoice(id);
            if (hd == null)
            {
                return Json(new { success = false, message = "Không tìm thấy hóa đơn." });
            }
            return Json(new { success = true, data = hd });
        }
    }
}
