using System.Collections.Generic;
using System.IO;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public interface IExcelExportService
    {
        byte[] ExportInventory(List<LoHang> lots);
        byte[] ExportSalesReport(List<HoaDon> invoices);
        List<HangHoa> ImportProductsFromExcel(Stream stream);
    }
}
