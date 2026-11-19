using System.Collections.Generic;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    /// <summary>
    /// Giao diện xử lý thuật toán FEFO (First Expired, First Out - Hết hạn trước, xuất trước)
    /// </summary>
    public interface IFefoService
    {
        /// <summary>
        /// Phân bổ số lượng bán từ các lô hàng trên quầy theo nguyên tắc FEFO
        /// </summary>
        List<FefoAllocationItem> AllocateLotsForSale(int maHang, int requiredQuantity);

        /// <summary>
        /// Phân bổ số lượng xuất từ kho lên quầy theo nguyên tắc FEFO
        /// </summary>
        List<FefoAllocationItem> AllocateLotsForWarehouseTransfer(int maHang, int requiredQuantity);
    }
}
