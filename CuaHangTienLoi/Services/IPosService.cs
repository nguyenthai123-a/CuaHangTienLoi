using System.Collections.Generic;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public interface IPosService
    {
        PosCartItem? ScanBarcode(string barcode);
        List<PosCartItem> SearchProducts(string? keyword = null);
        KhachHang? FindCustomerByPhone(string phone);
        KhachHang CreateCustomer(string name, string phone);
        PosCheckoutResult Checkout(PosCheckoutRequest request, int currentMaNV, int currentMaTK, string ip, string userAgent);
        HoaDon? GetInvoice(int maHoaDon);
        List<HoaDon> GetInvoicesByCa(int maCa);
    }
}
