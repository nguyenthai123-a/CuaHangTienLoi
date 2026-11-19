using System;
using Microsoft.AspNetCore.Mvc;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Filters;
using CuaHangTienLoi.Services;

namespace CuaHangTienLoi.Controllers
{
    [AuthorizePermission("BAO_CAO")]
    public class BaoCaoController : Controller
    {
        private readonly BanHangDao _banHangDao;
        private readonly CaDao _caDao;
        private readonly IExcelExportService _excelService;

        public BaoCaoController(BanHangDao banHangDao, CaDao caDao, IExcelExportService excelService)
        {
            _banHangDao = banHangDao;
            _caDao = caDao;
            _excelService = excelService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var invoices = _banHangDao.GetRecentInvoices(100);
            ViewBag.Shifts = _caDao.GetAllShifts(50);
            return View(invoices);
        }

        [HttpGet]
        public IActionResult XuatExcel()
        {
            var invoices = _banHangDao.GetRecentInvoices(500);
            var excelBytes = _excelService.ExportSalesReport(invoices);
            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"BaoCaoDoanhThu_{DateTime.Now:yyyyMMdd_HHmm}.xlsx");
        }
    }
}
