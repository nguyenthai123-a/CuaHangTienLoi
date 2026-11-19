using System.Collections.Generic;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public interface IDoiTraService
    {
        int LapPhieuTra(int maHoaDon, int? maCa, string lyDo, List<ChiTietPhieuTra> items, int maNVLap, int maTK, string ip, string userAgent);
        void DuyetPhieuTra(int maPhieuTra, int maNVDuyet, int maTK, string phuongThucHoan, string? maGiaoDichHoan, string ip, string userAgent);
        void TuChoiPhieuTra(int maPhieuTra, int maNVDuyet, int maTK, string ip, string userAgent);
        List<PhieuTraHang> GetPendingReturns();
        List<PhieuTraHang> GetAllReturns();
        PhieuTraHang? GetById(int maPhieuTra);
    }
}
