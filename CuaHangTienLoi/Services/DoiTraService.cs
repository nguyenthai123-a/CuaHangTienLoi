using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public class DoiTraService : IDoiTraService
    {
        private readonly string _connectionString;
        private readonly DoiTraDao _doiTraDao;
        private readonly BanHangDao _banHangDao;
        private readonly KhoDao _khoDao;
        private readonly KhachHangDao _khachHangDao;
        private readonly NhatKyDao _nhatKyDao;

        public DoiTraService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found");
            _doiTraDao = new DoiTraDao(configuration);
            _banHangDao = new BanHangDao(configuration);
            _khoDao = new KhoDao(configuration);
            _khachHangDao = new KhachHangDao(configuration);
            _nhatKyDao = new NhatKyDao(configuration);
        }

        public int LapPhieuTra(int maHoaDon, int? maCa, string lyDo, List<ChiTietPhieuTra> items, int maNVLap, int maTK, string ip, string userAgent)
        {
            if (string.IsNullOrWhiteSpace(lyDo))
                throw new ArgumentException("Vui lòng nhập lý do đổi trả hàng.");

            if (items == null || items.Count == 0)
                throw new ArgumentException("Vui lòng chọn ít nhất một sản phẩm cần đổi trả.");

            var hd = _banHangDao.GetHoaDonById(maHoaDon);
            if (hd == null || hd.TrangThai != "HOAN_TAT")
                throw new InvalidOperationException("Hóa đơn không hợp lệ hoặc đã bị hủy.");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var pt = new PhieuTraHang
                {
                    MaHoaDon = maHoaDon,
                    MaCa = maCa,
                    LyDo = lyDo.Trim(),
                    MaNVLap = maNVLap
                };

                int maPhieuTra = _doiTraDao.CreatePhieuTra(pt, items, conn, trans);

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "LAP_PHIEU_TRA_HANG",
                    DoiTuong = "PhieuTraHang",
                    MaDoiTuong = maPhieuTra.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Lập phiếu trả hàng cho HĐ {hd.SoHoaDon}. Lý do: {lyDo} (Chờ Quản lý phê duyệt)"
                }, conn, trans);

                trans.Commit();
                return maPhieuTra;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void DuyetPhieuTra(int maPhieuTra, int maNVDuyet, int maTK, string phuongThucHoan, string? maGiaoDichHoan, string ip, string userAgent)
        {
            var pt = _doiTraDao.GetById(maPhieuTra);
            if (pt == null || pt.TrangThaiDuyet != "CHO_DUYET")
                throw new InvalidOperationException("Phiếu trả hàng không tồn tại hoặc đã được xử lý.");

            var hd = _banHangDao.GetHoaDonById(pt.MaHoaDon);
            if (hd == null)
                throw new InvalidOperationException("Không tìm thấy thông tin hóa đơn gốc.");

            // Tính toán số tiền hoàn dựa trên các sản phẩm trả
            decimal soTienHoan = 0;
            foreach (var item in pt.ChiTiet)
            {
                var cthd = hd.ChiTiet.FirstOrDefault(c => c.MaCT == item.MaCT);
                if (cthd != null)
                {
                    decimal unitPriceAfterDiscount = cthd.ThanhTien / cthd.SoLuong;
                    soTienHoan += unitPriceAfterDiscount * item.SoLuong;
                }
            }

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // Cấp số phiếu trả từ Sequence dbo.seq_PhieuTra và cập nhật trạng thái DA_DUYET
                _doiTraDao.ApprovePhieuTra(maPhieuTra, maNVDuyet, soTienHoan, phuongThucHoan, maGiaoDichHoan, conn, trans);

                // Xử lý tồn kho cho các sản phẩm trả
                foreach (var item in pt.ChiTiet)
                {
                    var cthd = hd.ChiTiet.FirstOrDefault(c => c.MaCT == item.MaCT);
                    if (cthd != null)
                    {
                        if (item.TinhTrangHang == "CON_DUNG_DUOC")
                        {
                            // Cộng lại tồn quầy
                            int tonSau = _khoDao.AddShelfStock(cthd.MaLo, item.SoLuong, conn, trans);

                            // Ghi thẻ kho
                            _khoDao.InsertTheKho(new TheKho
                            {
                                MaHang = cthd.MaHang,
                                MaLo = cthd.MaLo,
                                KhuVuc = "QUAY",
                                LoaiGiaoDich = "TRA_HANG",
                                LoaiChungTu = "PT",
                                MaChungTu = maPhieuTra,
                                SlNhap = item.SoLuong,
                                SlXuat = 0,
                                TonSau = tonSau
                            }, conn, trans);
                        }
                    }
                }

                // Trừ điểm tích lũy đã cộng trước đó của khách hàng nếu có
                if (hd.MaKH.HasValue)
                {
                    int diemTru = (int)(soTienHoan / 10000m);
                    if (diemTru > 0)
                    {
                        try
                        {
                            _khachHangDao.UpdatePoints(hd.MaKH.Value, -diemTru, conn, trans);
                            _khachHangDao.RecordPointTransaction(new GiaoDichDiem
                            {
                                MaKH = hd.MaKH.Value,
                                Loai = "TRU_TRA_HANG",
                                SoDiem = diemTru,
                                MaHoaDon = hd.MaHoaDon,
                                MaPhieuTra = maPhieuTra
                            }, conn, trans);
                        }
                        catch
                        {
                            // Nếu khách không đủ điểm để trừ, không làm sập giao dịch duyệt hoàn tiền
                        }
                    }
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "DUYET_TRA_HANG",
                    DoiTuong = "PhieuTraHang",
                    MaDoiTuong = maPhieuTra.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Quản lý đã DUYỆT phiếu trả hàng {maPhieuTra}. Số tiền hoàn: {soTienHoan:N0}đ, Hình thức: {phuongThucHoan}"
                }, conn, trans);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void TuChoiPhieuTra(int maPhieuTra, int maNVDuyet, int maTK, string ip, string userAgent)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                _doiTraDao.RejectPhieuTra(maPhieuTra, maNVDuyet, conn, trans);

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "TU_CHOI_TRA_HANG",
                    DoiTuong = "PhieuTraHang",
                    MaDoiTuong = maPhieuTra.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Quản lý TỪ CHỐI phiếu trả hàng {maPhieuTra}"
                }, conn, trans);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public List<PhieuTraHang> GetPendingReturns()
        {
            return _doiTraDao.GetPendingReturns();
        }

        public List<PhieuTraHang> GetAllReturns()
        {
            return _doiTraDao.GetAllReturns();
        }

        public PhieuTraHang? GetById(int maPhieuTra)
        {
            return _doiTraDao.GetById(maPhieuTra);
        }
    }
}
