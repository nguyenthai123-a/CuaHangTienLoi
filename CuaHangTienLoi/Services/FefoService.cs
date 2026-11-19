using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    /// <summary>
    /// Cài đặt chuẩn xác thuật toán FEFO (First Expired, First Out)
    /// Đảm bảo luôn lấy hàng từ lô có ngày hết hạn sớm nhất trước, tự động cắt sang lô tiếp theo khi hết
    /// </summary>
    public class FefoService : IFefoService
    {
        private readonly KhoDao _khoDao;

        public FefoService(IConfiguration configuration)
        {
            _khoDao = new KhoDao(configuration);
        }

        public List<FefoAllocationItem> AllocateLotsForSale(int maHang, int requiredQuantity)
        {
            if (requiredQuantity <= 0)
                throw new ArgumentException("Số lượng yêu cầu phải lớn hơn 0.", nameof(requiredQuantity));

            // Lấy danh sách các lô có tồn quầy > 0, sắp xếp theo HanSuDung tăng dần
            var availableLots = _khoDao.GetFefoLotsForShelf(maHang);

            int totalAvailable = availableLots.Sum(l => l.SlTonQuay);
            if (totalAvailable < requiredQuantity)
            {
                string productName = availableLots.FirstOrDefault()?.TenHang ?? $"Mã hàng {maHang}";
                throw new InvalidOperationException($"Sản phẩm '{productName}' không đủ tồn quầy để bán (Tồn quầy hiện tại: {totalAvailable}, Yêu cầu: {requiredQuantity}).");
            }

            var allocations = new List<FefoAllocationItem>();
            int remainingNeeded = requiredQuantity;

            foreach (var lot in availableLots)
            {
                if (remainingNeeded <= 0) break;

                int qtyToTake = Math.Min(remainingNeeded, lot.SlTonQuay);
                allocations.Add(new FefoAllocationItem
                {
                    MaLo = lot.MaLo,
                    SoLo = lot.SoLo,
                    HanSuDung = lot.HanSuDung,
                    SoLuongXuat = qtyToTake,
                    GiaNhap = lot.GiaBan // Giá vốn lúc bán (hoặc lấy giá nhập thực tế từ DB)
                });

                remainingNeeded -= qtyToTake;
            }

            return allocations;
        }

        public List<FefoAllocationItem> AllocateLotsForWarehouseTransfer(int maHang, int requiredQuantity)
        {
            if (requiredQuantity <= 0)
                throw new ArgumentException("Số lượng yêu cầu phải lớn hơn 0.", nameof(requiredQuantity));

            var warehouseLots = _khoDao.GetFefoLotsForWarehouse(maHang);

            int totalAvailable = warehouseLots.Sum(l => l.SlTonKho);
            if (totalAvailable < requiredQuantity)
            {
                string productName = warehouseLots.FirstOrDefault()?.TenHang ?? $"Mã hàng {maHang}";
                throw new InvalidOperationException($"Sản phẩm '{productName}' trong kho không đủ để xuất lên quầy (Tồn kho: {totalAvailable}, Yêu cầu: {requiredQuantity}).");
            }

            var allocations = new List<FefoAllocationItem>();
            int remainingNeeded = requiredQuantity;

            foreach (var lot in warehouseLots)
            {
                if (remainingNeeded <= 0) break;

                int qtyToTake = Math.Min(remainingNeeded, lot.SlTonKho);
                allocations.Add(new FefoAllocationItem
                {
                    MaLo = lot.MaLo,
                    SoLo = lot.SoLo,
                    HanSuDung = lot.HanSuDung,
                    SoLuongXuat = qtyToTake,
                    GiaNhap = 0
                });

                remainingNeeded -= qtyToTake;
            }

            return allocations;
        }
    }
}
