using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public class KhoService : IKhoService
    {
        private readonly string _connectionString;
        private readonly KhoDao _khoDao;
        private readonly HangHoaDao _hangHoaDao;
        private readonly NhatKyDao _nhatKyDao;
        private readonly IFefoService _fefoService;

        public KhoService(IConfiguration configuration, IFefoService fefoService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found");
            _khoDao = new KhoDao(configuration);
            _hangHoaDao = new HangHoaDao(configuration);
            _nhatKyDao = new NhatKyDao(configuration);
            _fefoService = fefoService;
        }

        public int NhapKho(NhapKhoViewModel model, int maNVLap, int maTK, string ip, string userAgent)
        {
            if (model.ChiTiet == null || model.ChiTiet.Count == 0)
                throw new InvalidOperationException("Vui lòng thêm ít nhất một sản phẩm nhập kho.");

            decimal tongTien = model.ChiTiet.Sum(c => c.SoLuong * c.GiaNhap);

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var pn = new PhieuNhap
                {
                    SoPhieuGiao = model.SoPhieuGiao.Trim(),
                    TongTien = tongTien,
                    MaNCC = model.MaNCC,
                    MaNVLap = maNVLap
                };

                int maPN = _khoDao.InsertPhieuNhap(pn, conn, trans);

                foreach (var item in model.ChiTiet)
                {
                    // Mỗi dòng chi tiết tạo ra 1 Lô hàng mới (MaLo duy nhất)
                    int maLo = _khoDao.CreateLoHang(item.MaHang, item.SoLo, item.HanSuDung, item.SoLuong, conn, trans);

                    _khoDao.InsertChiTietPhieuNhap(new ChiTietPhieuNhap
                    {
                        MaPN = maPN,
                        MaLo = maLo,
                        SoLuong = item.SoLuong,
                        GiaNhap = item.GiaNhap,
                        HanSuDung = item.HanSuDung
                    }, conn, trans);

                    // Ghi nhận Thẻ kho
                    _khoDao.InsertTheKho(new TheKho
                    {
                        MaHang = item.MaHang,
                        MaLo = maLo,
                        KhuVuc = "KHO",
                        LoaiGiaoDich = "NHAP",
                        LoaiChungTu = "PN",
                        MaChungTu = maPN,
                        SlNhap = item.SoLuong,
                        SlXuat = 0,
                        TonSau = item.SoLuong
                    }, conn, trans);
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "NHAP_KHO",
                    DoiTuong = "PhieuNhap",
                    MaDoiTuong = maPN.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Nhập kho thành công: {model.ChiTiet.Count} mặt hàng, Tổng tiền: {tongTien:N0}đ, Số phiếu giao: {model.SoPhieuGiao}"
                }, conn, trans);

                trans.Commit();
                return maPN;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void XuatLenQuay(int maHang, int soLuong, int maNVLap, int maTK, string ip, string userAgent)
        {
            if (soLuong <= 0)
                throw new ArgumentException("Số lượng xuất lên quầy phải lớn hơn 0.");

            var product = _hangHoaDao.GetById(maHang);
            if (product == null)
                throw new InvalidOperationException("Không tìm thấy hàng hóa.");

            // Áp dụng thuật toán FEFO để chọn các lô trong kho có HSD sớm nhất
            var allocations = _fefoService.AllocateLotsForWarehouseTransfer(maHang, soLuong);

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var px = new PhieuXuat
                {
                    LoaiXuat = "LEN_QUAY",
                    TrangThai = "HOAN_TAT",
                    LyDo = $"Bổ sung hàng lên quầy theo nguyên tắc FEFO cho sản phẩm {product.TenHang}",
                    MaNVLap = maNVLap
                };

                int maPX = _khoDao.InsertPhieuXuat(px, conn, trans);

                foreach (var alloc in allocations)
                {
                    // Chuyển SlTonKho sang SlTonQuay
                    int tonQuaySau = _khoDao.TransferWarehouseToShelf(alloc.MaLo, alloc.SoLuongXuat, conn, trans);

                    _khoDao.InsertChiTietPhieuXuat(new ChiTietPhieuXuat
                    {
                        MaPX = maPX,
                        MaLo = alloc.MaLo,
                        SoLuong = alloc.SoLuongXuat,
                        GhiChu = $"Chuyển lên quầy lô {alloc.SoLo} (HSD: {alloc.HanSuDung:dd/MM/yyyy})"
                    }, conn, trans);

                    // Ghi thẻ kho: Giảm kho
                    var lot = _khoDao.GetLotById(alloc.MaLo, conn, trans);
                    _khoDao.InsertTheKho(new TheKho
                    {
                        MaHang = maHang,
                        MaLo = alloc.MaLo,
                        KhuVuc = "KHO",
                        LoaiGiaoDich = "XUAT_LEN_QUAY",
                        LoaiChungTu = "PX",
                        MaChungTu = maPX,
                        SlNhap = 0,
                        SlXuat = alloc.SoLuongXuat,
                        TonSau = lot?.SlTonKho ?? 0
                    }, conn, trans);

                    // Ghi thẻ kho: Tăng quầy
                    _khoDao.InsertTheKho(new TheKho
                    {
                        MaHang = maHang,
                        MaLo = alloc.MaLo,
                        KhuVuc = "QUAY",
                        LoaiGiaoDich = "XUAT_LEN_QUAY",
                        LoaiChungTu = "PX",
                        MaChungTu = maPX,
                        SlNhap = alloc.SoLuongXuat,
                        SlXuat = 0,
                        TonSau = tonQuaySau
                    }, conn, trans);
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "XUAT_LEN_QUAY",
                    DoiTuong = "PhieuXuat",
                    MaDoiTuong = maPX.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Xuất lên quầy {soLuong} {product.DonViTinh} sản phẩm {product.TenHang} theo FEFO"
                }, conn, trans);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public int TaoPhieuXuatHuy(XuatKhoViewModel model, int maNVLap, int maTK, string ip, string userAgent)
        {
            if (model.ChiTiet == null || model.ChiTiet.Count == 0)
                throw new InvalidOperationException("Vui lòng chọn ít nhất một lô hàng để xuất hủy.");

            if (string.IsNullOrWhiteSpace(model.LyDo))
                throw new InvalidOperationException("Vui lòng nhập lý do xuất hủy.");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var px = new PhieuXuat
                {
                    LoaiXuat = "HUY",
                    TrangThai = "CHO_DUYET", // Bắt buộc Quản lý duyệt
                    KhuVuc = model.KhuVuc ?? "KHO",
                    LyDo = model.LyDo.Trim(),
                    MaNVLap = maNVLap
                };

                int maPX = _khoDao.InsertPhieuXuat(px, conn, trans);

                foreach (var item in model.ChiTiet)
                {
                    _khoDao.InsertChiTietPhieuXuat(new ChiTietPhieuXuat
                    {
                        MaPX = maPX,
                        MaLo = item.MaLo,
                        SoLuong = item.SoLuong,
                        GhiChu = item.GhiChu
                    }, conn, trans);
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "TAO_PHIEU_XUAT_HUY",
                    DoiTuong = "PhieuXuat",
                    MaDoiTuong = maPX.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Lập phiếu xuất hủy {px.SoPhieuXuat} (chờ Quản lý phê duyệt). Lý do: {model.LyDo}"
                }, conn, trans);

                trans.Commit();
                return maPX;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void DuyetPhieuXuatHuy(int maPX, int maNVDuyet, int maTK, bool dongY, string ip, string userAgent)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                string sqlGet = "SELECT MaPX, LoaiXuat, KhuVuc, TrangThai FROM dbo.PhieuXuat WHERE MaPX = @MaPX";
                string loaiXuat = "", khuVuc = "", trangThai = "";
                using (var cmdGet = new SqlCommand(sqlGet, conn, trans))
                {
                    cmdGet.Parameters.AddWithValue("@MaPX", maPX);
                    using var reader = cmdGet.ExecuteReader();
                    if (!reader.Read()) throw new InvalidOperationException("Phiếu xuất không tồn tại.");
                    loaiXuat = reader.GetString(reader.GetOrdinal("LoaiXuat"));
                    khuVuc = reader.GetString(reader.GetOrdinal("KhuVuc"));
                    trangThai = reader.GetString(reader.GetOrdinal("TrangThai"));
                }

                if (trangThai != "CHO_DUYET")
                    throw new InvalidOperationException("Phiếu này đã được duyệt hoặc từ chối trước đó.");

                string newStatus = dongY ? "HOAN_TAT" : "TU_CHOI";

                const string sqlUpdate = "UPDATE dbo.PhieuXuat SET TrangThai = @TrangThai, MaNVDuyet = @MaNVDuyet WHERE MaPX = @MaPX";
                using (var cmdUpdate = new SqlCommand(sqlUpdate, conn, trans))
                {
                    cmdUpdate.Parameters.AddWithValue("@TrangThai", newStatus);
                    cmdUpdate.Parameters.AddWithValue("@MaNVDuyet", maNVDuyet);
                    cmdUpdate.Parameters.AddWithValue("@MaPX", maPX);
                    cmdUpdate.ExecuteNonQuery();
                }

                if (dongY)
                {
                    // Lấy chi tiết xuất hủy và trừ tồn kho hoặc quầy tương ứng
                    const string sqlItems = "SELECT ct.MaLo, ct.SoLuong, l.MaHang, l.SlTonKho, l.SlTonQuay FROM dbo.ChiTietPhieuXuat ct INNER JOIN dbo.LoHang l ON ct.MaLo = l.MaLo WHERE ct.MaPX = @MaPX";
                    var items = new List<(int MaLo, int SoLuong, int MaHang)>();
                    using (var cmdItems = new SqlCommand(sqlItems, conn, trans))
                    {
                        cmdItems.Parameters.AddWithValue("@MaPX", maPX);
                        using var reader = cmdItems.ExecuteReader();
                        while (reader.Read())
                        {
                            items.Add((reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2)));
                        }
                    }

                    foreach (var item in items)
                    {
                        string updateStockSql = khuVuc == "QUAY"
                            ? "UPDATE dbo.LoHang SET SlTonQuay = SlTonQuay - @Qty WHERE MaLo = @MaLo; SELECT SlTonQuay FROM dbo.LoHang WHERE MaLo = @MaLo;"
                            : "UPDATE dbo.LoHang SET SlTonKho = SlTonKho - @Qty WHERE MaLo = @MaLo; SELECT SlTonKho FROM dbo.LoHang WHERE MaLo = @MaLo;";

                        int tonSau = 0;
                        using (var cmdStock = new SqlCommand(updateStockSql, conn, trans))
                        {
                            cmdStock.Parameters.AddWithValue("@Qty", item.SoLuong);
                            cmdStock.Parameters.AddWithValue("@MaLo", item.MaLo);
                            tonSau = Convert.ToInt32(cmdStock.ExecuteScalar());
                        }

                        // Ghi nhận Thẻ kho
                        _khoDao.InsertTheKho(new TheKho
                        {
                            MaHang = item.MaHang,
                            MaLo = item.MaLo,
                            KhuVuc = khuVuc,
                            LoaiGiaoDich = "XUAT_HUY",
                            LoaiChungTu = "PX",
                            MaChungTu = maPX,
                            SlNhap = 0,
                            SlXuat = item.SoLuong,
                            TonSau = tonSau
                        }, conn, trans);
                    }
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = dongY ? "DUYET_XUAT_HUY" : "TU_CHOI_XUAT_HUY",
                    DoiTuong = "PhieuXuat",
                    MaDoiTuong = maPX.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Quản lý {(dongY ? "đã DUYỆT" : "TỪ CHỐI")} phiếu xuất hủy {maPX}"
                }, conn, trans);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public int TaoPhieuKiemKe(KiemKeViewModel model, int maNVLap, int maTK, string ip, string userAgent)
        {
            if (model.ChiTiet == null || model.ChiTiet.Count == 0)
                throw new InvalidOperationException("Danh sách kiểm kê không được để trống.");

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                var kk = new PhieuKiemKe
                {
                    KhuVuc = model.KhuVuc,
                    TrangThaiDuyet = "CHO_DUYET",
                    MaNVLap = maNVLap
                };

                int maKK = _khoDao.InsertPhieuKiemKe(kk, conn, trans);

                foreach (var item in model.ChiTiet)
                {
                    _khoDao.InsertChiTietKiemKe(new ChiTietKiemKe
                    {
                        MaKK = maKK,
                        MaLo = item.MaLo,
                        SlHeThong = item.SlHeThong,
                        SlThucTe = item.SlThucTe
                    }, conn, trans);
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = "TAO_PHIEU_KIEM_KE",
                    DoiTuong = "PhieuKiemKe",
                    MaDoiTuong = maKK.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Lập phiếu kiểm kê {model.KhuVuc} gồm {model.ChiTiet.Count} lô hàng (Chờ quản lý duyệt điều chỉnh)"
                }, conn, trans);

                trans.Commit();
                return maKK;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void DuyetPhieuKiemKe(int maKK, int maNVDuyet, int maTK, bool dongY, string ip, string userAgent)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                string newStatus = dongY ? "DA_DUYET" : "TU_CHOI";

                const string sqlUpdate = "UPDATE dbo.PhieuKiemKe SET TrangThaiDuyet = @TrangThai, MaNVDuyet = @MaNVDuyet WHERE MaKK = @MaKK AND TrangThaiDuyet = 'CHO_DUYET'";
                using (var cmd = new SqlCommand(sqlUpdate, conn, trans))
                {
                    cmd.Parameters.AddWithValue("@TrangThai", newStatus);
                    cmd.Parameters.AddWithValue("@MaNVDuyet", maNVDuyet);
                    cmd.Parameters.AddWithValue("@MaKK", maKK);
                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new InvalidOperationException("Phiếu kiểm kê không tồn tại hoặc đã được xử lý.");
                }

                if (dongY)
                {
                    var items = _khoDao.GetChiTietKiemKe(maKK);
                    const string sqlKhuVuc = "SELECT KhuVuc FROM dbo.PhieuKiemKe WHERE MaKK = @MaKK";
                    string khuVuc = "";
                    using (var cmdKV = new SqlCommand(sqlKhuVuc, conn, trans))
                    {
                        cmdKV.Parameters.AddWithValue("@MaKK", maKK);
                        khuVuc = Convert.ToString(cmdKV.ExecuteScalar()) ?? "QUAY";
                    }

                    foreach (var item in items)
                    {
                        // Cập nhật tồn kho theo số lượng thực tế kiểm kê
                        string sqlAdjust = khuVuc == "QUAY"
                            ? "UPDATE dbo.LoHang SET SlTonQuay = @SlThucTe WHERE MaLo = @MaLo"
                            : "UPDATE dbo.LoHang SET SlTonKho = @SlThucTe WHERE MaLo = @MaLo";

                        using (var cmdAdj = new SqlCommand(sqlAdjust, conn, trans))
                        {
                            cmdAdj.Parameters.AddWithValue("@SlThucTe", item.SlThucTe);
                            cmdAdj.Parameters.AddWithValue("@MaLo", item.MaLo);
                            cmdAdj.ExecuteNonQuery();
                        }

                        // Ghi Thẻ kho
                        int diff = item.ChenhLech;
                        int slNhap = diff > 0 ? diff : 0;
                        int slXuat = diff < 0 ? -diff : 0;

                        var lot = _khoDao.GetLotById(item.MaLo, conn, trans);
                        _khoDao.InsertTheKho(new TheKho
                        {
                            MaHang = lot?.MaHang ?? 0,
                            MaLo = item.MaLo,
                            KhuVuc = khuVuc,
                            LoaiGiaoDich = "KIEM_KE",
                            LoaiChungTu = "KK",
                            MaChungTu = maKK,
                            SlNhap = slNhap,
                            SlXuat = slXuat,
                            TonSau = item.SlThucTe
                        }, conn, trans);
                    }
                }

                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = dongY ? "DUYET_KIEM_KE" : "TU_CHOI_KIEM_KE",
                    DoiTuong = "PhieuKiemKe",
                    MaDoiTuong = maKK.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Quản lý {(dongY ? "đã DUYỆT điều chỉnh tồn kho" : "TỪ CHỐI")} phiếu kiểm kê {maKK}"
                }, conn, trans);

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public List<TheKho> GetTheKhoHistory(int? maHang, int? maLo, DateTime? fromDate, DateTime? toDate)
        {
            return _khoDao.GetTheKhoHistory(maHang, maLo, fromDate, toDate);
        }

        public List<LoHang> GetNearExpiryLots(int days)
        {
            return _khoDao.GetNearExpiryLots(days);
        }

        public List<LoHang> GetAllLots(int? maHang)
        {
            return _khoDao.GetAllLots(maHang);
        }
    }
}
