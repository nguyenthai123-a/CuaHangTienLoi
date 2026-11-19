using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public class CaService : ICaService
    {
        private readonly CaDao _caDao;
        private readonly NhatKyDao _nhatKyDao;

        public CaService(IConfiguration configuration)
        {
            _caDao = new CaDao(configuration);
            _nhatKyDao = new NhatKyDao(configuration);
        }

        public int MoCa(int maQuay, int maThuNgan, decimal tienDauCa, int maTK, string ip, string userAgent)
        {
            if (tienDauCa < 0)
                throw new ArgumentException("Số tiền đầu ca không được âm.", nameof(tienDauCa));

            // Kiểm tra ràng buộc UX_Ca_QuayDangMo: Mỗi quầy chỉ có DUY NHẤT một ca mở tại một thời điểm
            var existingOpen = _caDao.GetOpenShiftByQuay(maQuay);
            if (existingOpen != null)
            {
                throw new InvalidOperationException($"Quầy {existingOpen.TenQuay} hiện đang có một ca làm việc đang mở (Thu ngân: {existingOpen.TenThuNgan}). Vui lòng đóng ca cũ trước khi mở ca mới!");
            }

            int maCa = _caDao.OpenShift(maQuay, maThuNgan, tienDauCa);

            _nhatKyDao.InsertLog(new NhatKyKiemToan
            {
                MaTK = maTK,
                HanhDong = "MO_CA",
                DoiTuong = "Ca",
                MaDoiTuong = maCa.ToString(),
                DiaChiIP = ip,
                ThietBi = userAgent,
                GiaTriMoi = $"Mở ca thành công tại Quầy {maQuay}. Tiền đầu ca khai báo: {tienDauCa:N0}đ"
            });

            return maCa;
        }

        public DongCaViewModel GetShiftCloseInfo(int maCa)
        {
            var ca = _caDao.GetById(maCa);
            if (ca == null)
                throw new InvalidOperationException("Không tìm thấy thông tin ca làm việc.");

            var revenue = _caDao.CalculateShiftRevenue(maCa);

            return new DongCaViewModel
            {
                MaCa = ca.MaCa,
                TenQuay = ca.TenQuay ?? $"Quầy {ca.MaQuay}",
                TenThuNgan = ca.TenThuNgan ?? $"NV {ca.MaThuNgan}",
                GioMo = ca.GioMo,
                TienDauCa = ca.TienDauCa,
                DoanhThuTienMat = revenue.TienMat,
                DoanhThuThe = revenue.The,
                DoanhThuQR = revenue.QR,
                TienMatThucTe = ca.TienDauCa + revenue.TienMat // Mặc định gợi ý bằng tiền hệ thống
            };
        }

        public void DongCa(DongCaViewModel model, int maNguoiChot, int maTK, string ip, string userAgent)
        {
            var ca = _caDao.GetById(model.MaCa);
            if (ca == null || ca.TrangThai != "MO")
                throw new InvalidOperationException("Ca làm việc này đã được đóng hoặc không tồn tại.");

            // Tính toán doanh thu thực tế từ CSDL
            var revenue = _caDao.CalculateShiftRevenue(model.MaCa);
            decimal tienMatHeThong = ca.TienDauCa + revenue.TienMat;
            decimal chenhLech = model.TienMatThucTe - tienMatHeThong;

            // Nếu có chênh lệch tiền mặt, bắt buộc phải có giải trình lý do
            if (chenhLech != 0 && string.IsNullOrWhiteSpace(model.LyDoChenhLech))
            {
                throw new InvalidOperationException($"Số tiền mặt thực tế ({model.TienMatThucTe:N0}đ) lệch so với hệ thống ({tienMatHeThong:N0}đ) là {chenhLech:N0}đ. Vui lòng nhập lý do giải trình chênh lệch.");
            }

            _caDao.CloseShift(model.MaCa, maNguoiChot, tienMatHeThong, model.TienMatThucTe, model.LyDoChenhLech, revenue.TienMat, revenue.The, revenue.QR);

            _nhatKyDao.InsertLog(new NhatKyKiemToan
            {
                MaTK = maTK,
                HanhDong = "DONG_CA",
                DoiTuong = "Ca",
                MaDoiTuong = model.MaCa.ToString(),
                DiaChiIP = ip,
                ThietBi = userAgent,
                GiaTriMoi = $"Đóng ca {model.MaCa}. Tiền hệ thống: {tienMatHeThong:N0}đ, Tiền thực tế: {model.TienMatThucTe:N0}đ, Chênh lệch: {chenhLech:N0}đ, Doanh thu TM: {revenue.TienMat:N0}đ, Thẻ: {revenue.The:N0}đ, QR: {revenue.QR:N0}đ"
            });
        }

        public BaoCaoCa? GetBaoCaoCa(int maCa)
        {
            return _caDao.GetBaoCaoCa(maCa);
        }

        public List<Ca> GetAllShifts()
        {
            return _caDao.GetAllShifts(100);
        }
    }
}
