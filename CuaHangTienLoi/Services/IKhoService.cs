using System;
using System.Collections.Generic;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public interface IKhoService
    {
        int NhapKho(NhapKhoViewModel model, int maNVLap, int maTK, string ip, string userAgent);
        void XuatLenQuay(int maHang, int soLuong, int maNVLap, int maTK, string ip, string userAgent);
        int TaoPhieuXuatHuy(XuatKhoViewModel model, int maNVLap, int maTK, string ip, string userAgent);
        void DuyetPhieuXuatHuy(int maPX, int maNVDuyet, int maTK, bool dongY, string ip, string userAgent);
        int TaoPhieuKiemKe(KiemKeViewModel model, int maNVLap, int maTK, string ip, string userAgent);
        void DuyetPhieuKiemKe(int maKK, int maNVDuyet, int maTK, bool dongY, string ip, string userAgent);
        List<TheKho> GetTheKhoHistory(int? maHang, int? maLo, DateTime? fromDate, DateTime? toDate);
        List<LoHang> GetNearExpiryLots(int days);
        List<LoHang> GetAllLots(int? maHang);
    }
}
