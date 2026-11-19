using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class KhoDao : BaseDao
    {
        public KhoDao(IConfiguration configuration) : base(configuration) { }
        public KhoDao(string connectionString) : base(connectionString) { }

        /// <summary>
        /// Lấy danh sách lô hàng theo thứ tự FEFO (Hết hạn trước, xếp trước - HanSuDung ASC)
        /// Tận dụng chỉ mục IX_LoHang_FEFO
        /// </summary>
        public List<LoHang> GetFefoLotsForShelf(int maHang)
        {
            const string sql = @"
                SELECT l.MaLo, l.MaHang, l.SoLo, l.HanSuDung, l.NgayNhap, l.SlTonKho, l.SlTonQuay,
                       h.TenHang, h.MaVach, h.DonViTinh, h.GiaBan
                FROM dbo.LoHang l WITH (INDEX(IX_LoHang_FEFO))
                INNER JOIN dbo.HangHoa h ON l.MaHang = h.MaHang
                WHERE l.MaHang = @MaHang AND l.SlTonQuay > 0
                ORDER BY l.HanSuDung ASC, l.MaLo ASC";

            var list = new List<LoHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHang", maHang);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new LoHang
                {
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                    HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung"),
                    NgayNhap = DbHelper.GetSafeDateTime(reader, "NgayNhap"),
                    SlTonKho = DbHelper.GetSafeInt(reader, "SlTonKho"),
                    SlTonQuay = DbHelper.GetSafeInt(reader, "SlTonQuay"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                    DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                    GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan")
                });
            }
            return list;
        }

        /// <summary>
        /// Lấy danh sách lô hàng trong kho phục vụ xuất lên quầy theo FEFO
        /// </summary>
        public List<LoHang> GetFefoLotsForWarehouse(int maHang)
        {
            const string sql = @"
                SELECT l.MaLo, l.MaHang, l.SoLo, l.HanSuDung, l.NgayNhap, l.SlTonKho, l.SlTonQuay,
                       h.TenHang, h.MaVach, h.DonViTinh, h.GiaBan
                FROM dbo.LoHang l
                INNER JOIN dbo.HangHoa h ON l.MaHang = h.MaHang
                WHERE l.MaHang = @MaHang AND l.SlTonKho > 0
                ORDER BY l.HanSuDung ASC, l.MaLo ASC";

            var list = new List<LoHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHang", maHang);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new LoHang
                {
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                    HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung"),
                    NgayNhap = DbHelper.GetSafeDateTime(reader, "NgayNhap"),
                    SlTonKho = DbHelper.GetSafeInt(reader, "SlTonKho"),
                    SlTonQuay = DbHelper.GetSafeInt(reader, "SlTonQuay"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                    DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                    GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan")
                });
            }
            return list;
        }

        public LoHang? GetLotById(int maLo, SqlConnection conn, SqlTransaction? trans = null)
        {
            const string sql = @"
                SELECT l.MaLo, l.MaHang, l.SoLo, l.HanSuDung, l.NgayNhap, l.SlTonKho, l.SlTonQuay,
                       h.TenHang, h.MaVach, h.DonViTinh, h.GiaBan, h.GiaNhap
                FROM dbo.LoHang l WITH (UPDLOCK)
                INNER JOIN dbo.HangHoa h ON l.MaHang = h.MaHang
                WHERE l.MaLo = @MaLo";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaLo", maLo);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new LoHang
            {
                MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung"),
                NgayNhap = DbHelper.GetSafeDateTime(reader, "NgayNhap"),
                SlTonKho = DbHelper.GetSafeInt(reader, "SlTonKho"),
                SlTonQuay = DbHelper.GetSafeInt(reader, "SlTonQuay"),
                TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan")
            };
        }

        public List<LoHang> GetAllLots(int? maHang = null)
        {
            string sql = @"
                SELECT l.MaLo, l.MaHang, l.SoLo, l.HanSuDung, l.NgayNhap, l.SlTonKho, l.SlTonQuay,
                       h.TenHang, h.MaVach, h.DonViTinh, h.GiaBan
                FROM dbo.LoHang l
                INNER JOIN dbo.HangHoa h ON l.MaHang = h.MaHang
                WHERE 1=1";

            if (maHang.HasValue && maHang.Value > 0)
                sql += " AND l.MaHang = @MaHang";

            sql += " ORDER BY l.HanSuDung ASC";

            var list = new List<LoHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            if (maHang.HasValue && maHang.Value > 0)
                cmd.Parameters.AddWithValue("@MaHang", maHang.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new LoHang
                {
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                    HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung"),
                    NgayNhap = DbHelper.GetSafeDateTime(reader, "NgayNhap"),
                    SlTonKho = DbHelper.GetSafeInt(reader, "SlTonKho"),
                    SlTonQuay = DbHelper.GetSafeInt(reader, "SlTonQuay"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                    DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                    GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan")
                });
            }
            return list;
        }

        public List<LoHang> GetNearExpiryLots(int daysThreshold = 30)
        {
            const string sql = @"
                SELECT l.MaLo, l.MaHang, l.SoLo, l.HanSuDung, l.NgayNhap, l.SlTonKho, l.SlTonQuay,
                       h.TenHang, h.MaVach, h.DonViTinh, h.GiaBan
                FROM dbo.LoHang l
                INNER JOIN dbo.HangHoa h ON l.MaHang = h.MaHang
                WHERE (l.SlTonKho > 0 OR l.SlTonQuay > 0)
                  AND DATEDIFF(DAY, GETDATE(), l.HanSuDung) <= @Days
                ORDER BY l.HanSuDung ASC";

            var list = new List<LoHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@Days", daysThreshold);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new LoHang
                {
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                    HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung"),
                    NgayNhap = DbHelper.GetSafeDateTime(reader, "NgayNhap"),
                    SlTonKho = DbHelper.GetSafeInt(reader, "SlTonKho"),
                    SlTonQuay = DbHelper.GetSafeInt(reader, "SlTonQuay"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                    DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                    GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan")
                });
            }
            return list;
        }

        public int DeductShelfStock(int maLo, int qty, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.LoHang WITH (UPDLOCK, ROWLOCK)
                SET SlTonQuay = SlTonQuay - @Qty
                WHERE MaLo = @MaLo AND SlTonQuay >= @Qty;
                SELECT SlTonQuay FROM dbo.LoHang WHERE MaLo = @MaLo;";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaLo", maLo);
            cmd.Parameters.AddWithValue("@Qty", qty);

            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"Không đủ tồn quầy cho lô {maLo} để trừ {qty} sản phẩm.");

            return Convert.ToInt32(result);
        }

        public int AddShelfStock(int maLo, int qty, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.LoHang WITH (UPDLOCK, ROWLOCK)
                SET SlTonQuay = SlTonQuay + @Qty
                WHERE MaLo = @MaLo;
                SELECT SlTonQuay FROM dbo.LoHang WHERE MaLo = @MaLo;";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaLo", maLo);
            cmd.Parameters.AddWithValue("@Qty", qty);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int TransferWarehouseToShelf(int maLo, int qty, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.LoHang WITH (UPDLOCK, ROWLOCK)
                SET SlTonKho = SlTonKho - @Qty,
                    SlTonQuay = SlTonQuay + @Qty
                WHERE MaLo = @MaLo AND SlTonKho >= @Qty;
                SELECT SlTonQuay FROM dbo.LoHang WHERE MaLo = @MaLo;";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaLo", maLo);
            cmd.Parameters.AddWithValue("@Qty", qty);

            var result = cmd.ExecuteScalar();
            if (result == null || result == DBNull.Value)
                throw new InvalidOperationException($"Lô {maLo} không đủ tồn kho để chuyển {qty} lên quầy.");

            return Convert.ToInt32(result);
        }

        public int CreateLoHang(int maHang, string soLo, DateTime hanSuDung, int slTonKho, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.LoHang (MaHang, SoLo, HanSuDung, NgayNhap, SlTonKho, SlTonQuay)
                VALUES (@MaHang, @SoLo, @HanSuDung, CONVERT(date, SYSDATETIME()), @SlTonKho, 0);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaHang", maHang);
            cmd.Parameters.AddWithValue("@SoLo", soLo.Trim());
            cmd.Parameters.AddWithValue("@HanSuDung", hanSuDung.Date);
            cmd.Parameters.AddWithValue("@SlTonKho", slTonKho);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int InsertPhieuNhap(PhieuNhap pn, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.PhieuNhap (NgayNhap, SoPhieuGiao, TongTien, MaNCC, MaNVLap)
                VALUES (CONVERT(date, SYSDATETIME()), @SoPhieuGiao, @TongTien, @MaNCC, @MaNVLap);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@SoPhieuGiao", pn.SoPhieuGiao.Trim());
            cmd.Parameters.AddWithValue("@TongTien", pn.TongTien);
            cmd.Parameters.AddWithValue("@MaNCC", pn.MaNCC);
            cmd.Parameters.AddWithValue("@MaNVLap", pn.MaNVLap);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void InsertChiTietPhieuNhap(ChiTietPhieuNhap ctpn, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.ChiTietPhieuNhap (MaPN, MaLo, SoLuong, GiaNhap, HanSuDung)
                VALUES (@MaPN, @MaLo, @SoLuong, @GiaNhap, @HanSuDung)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaPN", ctpn.MaPN);
            cmd.Parameters.AddWithValue("@MaLo", ctpn.MaLo);
            cmd.Parameters.AddWithValue("@SoLuong", ctpn.SoLuong);
            cmd.Parameters.AddWithValue("@GiaNhap", ctpn.GiaNhap);
            cmd.Parameters.AddWithValue("@HanSuDung", ctpn.HanSuDung.Date);
            cmd.ExecuteNonQuery();
        }

        public int InsertPhieuXuat(PhieuXuat px, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.PhieuXuat (LoaiXuat, NgayXuat, LyDo, TrangThai, KhuVuc, MaNCC, MaNVLap, MaNVDuyet)
                VALUES (@LoaiXuat, CONVERT(date, SYSDATETIME()), @LyDo, @TrangThai, @KhuVuc, @MaNCC, @MaNVLap, @MaNVDuyet);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@LoaiXuat", px.LoaiXuat);
            cmd.Parameters.AddWithValue("@LyDo", DbHelper.ToDbValue(px.LyDo));
            cmd.Parameters.AddWithValue("@TrangThai", px.TrangThai);
            cmd.Parameters.AddWithValue("@KhuVuc", DbHelper.ToDbValue(px.KhuVuc));
            cmd.Parameters.AddWithValue("@MaNCC", DbHelper.ToDbValue(px.MaNCC));
            cmd.Parameters.AddWithValue("@MaNVLap", px.MaNVLap);
            cmd.Parameters.AddWithValue("@MaNVDuyet", DbHelper.ToDbValue(px.MaNVDuyet));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void InsertChiTietPhieuXuat(ChiTietPhieuXuat ctpx, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.ChiTietPhieuXuat (MaPX, MaLo, SoLuong, GhiChu)
                VALUES (@MaPX, @MaLo, @SoLuong, @GhiChu)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaPX", ctpx.MaPX);
            cmd.Parameters.AddWithValue("@MaLo", ctpx.MaLo);
            cmd.Parameters.AddWithValue("@SoLuong", ctpx.SoLuong);
            cmd.Parameters.AddWithValue("@GhiChu", DbHelper.ToDbValue(ctpx.GhiChu));
            cmd.ExecuteNonQuery();
        }

        public void InsertTheKho(TheKho tk, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.TheKho (MaHang, MaLo, KhuVuc, Ngay, LoaiGiaoDich, LoaiChungTu, MaChungTu, SlNhap, SlXuat, TonSau)
                VALUES (@MaHang, @MaLo, @KhuVuc, SYSDATETIME(), @LoaiGiaoDich, @LoaiChungTu, @MaChungTu, @SlNhap, @SlXuat, @TonSau)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaHang", tk.MaHang);
            cmd.Parameters.AddWithValue("@MaLo", tk.MaLo);
            cmd.Parameters.AddWithValue("@KhuVuc", tk.KhuVuc);
            cmd.Parameters.AddWithValue("@LoaiGiaoDich", tk.LoaiGiaoDich);
            cmd.Parameters.AddWithValue("@LoaiChungTu", tk.LoaiChungTu);
            cmd.Parameters.AddWithValue("@MaChungTu", tk.MaChungTu);
            cmd.Parameters.AddWithValue("@SlNhap", tk.SlNhap);
            cmd.Parameters.AddWithValue("@SlXuat", tk.SlXuat);
            cmd.Parameters.AddWithValue("@TonSau", tk.TonSau);
            cmd.ExecuteNonQuery();
        }

        public List<TheKho> GetTheKhoHistory(int? maHang = null, int? maLo = null, DateTime? fromDate = null, DateTime? toDate = null)
        {
            string sql = @"
                SELECT tk.MaTheKho, tk.MaHang, tk.MaLo, tk.KhuVuc, tk.Ngay, tk.LoaiGiaoDich, 
                       tk.LoaiChungTu, tk.MaChungTu, tk.SlNhap, tk.SlXuat, tk.TonSau,
                       h.TenHang, l.SoLo
                FROM dbo.TheKho tk
                INNER JOIN dbo.HangHoa h ON tk.MaHang = h.MaHang
                INNER JOIN dbo.LoHang l ON tk.MaLo = l.MaLo
                WHERE 1=1";

            if (maHang.HasValue && maHang.Value > 0)
                sql += " AND tk.MaHang = @MaHang";

            if (maLo.HasValue && maLo.Value > 0)
                sql += " AND tk.MaLo = @MaLo";

            if (fromDate.HasValue)
                sql += " AND tk.Ngay >= @FromDate";

            if (toDate.HasValue)
                sql += " AND tk.Ngay <= @ToDate";

            sql += " ORDER BY tk.MaTheKho DESC";

            var list = new List<TheKho>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);

            if (maHang.HasValue && maHang.Value > 0)
                cmd.Parameters.AddWithValue("@MaHang", maHang.Value);

            if (maLo.HasValue && maLo.Value > 0)
                cmd.Parameters.AddWithValue("@MaLo", maLo.Value);

            if (fromDate.HasValue)
                cmd.Parameters.AddWithValue("@FromDate", fromDate.Value);

            if (toDate.HasValue)
                cmd.Parameters.AddWithValue("@ToDate", toDate.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TheKho
                {
                    MaTheKho = DbHelper.GetSafeLong(reader, "MaTheKho"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    KhuVuc = DbHelper.GetSafeString(reader, "KhuVuc"),
                    Ngay = DbHelper.GetSafeDateTime(reader, "Ngay"),
                    LoaiGiaoDich = DbHelper.GetSafeString(reader, "LoaiGiaoDich"),
                    LoaiChungTu = DbHelper.GetSafeString(reader, "LoaiChungTu"),
                    MaChungTu = DbHelper.GetSafeInt(reader, "MaChungTu"),
                    SlNhap = DbHelper.GetSafeInt(reader, "SlNhap"),
                    SlXuat = DbHelper.GetSafeInt(reader, "SlXuat"),
                    TonSau = DbHelper.GetSafeInt(reader, "TonSau"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo")
                });
            }
            return list;
        }

        public int InsertPhieuKiemKe(PhieuKiemKe kk, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.PhieuKiemKe (NgayKiemKe, KhuVuc, TrangThaiDuyet, MaNVLap)
                VALUES (CONVERT(date, SYSDATETIME()), @KhuVuc, @TrangThaiDuyet, @MaNVLap);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@KhuVuc", kk.KhuVuc);
            cmd.Parameters.AddWithValue("@TrangThaiDuyet", kk.TrangThaiDuyet);
            cmd.Parameters.AddWithValue("@MaNVLap", kk.MaNVLap);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void InsertChiTietKiemKe(ChiTietKiemKe ctkk, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.ChiTietKiemKe (MaKK, MaLo, SlHeThong, SlThucTe)
                VALUES (@MaKK, @MaLo, @SlHeThong, @SlThucTe)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaKK", ctkk.MaKK);
            cmd.Parameters.AddWithValue("@MaLo", ctkk.MaLo);
            cmd.Parameters.AddWithValue("@SlHeThong", ctkk.SlHeThong);
            cmd.Parameters.AddWithValue("@SlThucTe", ctkk.SlThucTe);
            cmd.ExecuteNonQuery();
        }

        public List<PhieuKiemKe> GetAllPhieuKiemKe()
        {
            const string sql = @"
                SELECT kk.MaKK, kk.SoPhieuKiemKe, kk.NgayKiemKe, kk.KhuVuc, kk.TrangThaiDuyet,
                       kk.MaNVLap, kk.MaNVDuyet,
                       nvLap.HoTen AS TenNVLap, nvDuyet.HoTen AS TenNVDuyet
                FROM dbo.PhieuKiemKe kk
                INNER JOIN dbo.NhanVien nvLap ON kk.MaNVLap = nvLap.MaNV
                LEFT JOIN dbo.NhanVien nvDuyet ON kk.MaNVDuyet = nvDuyet.MaNV
                ORDER BY kk.MaKK DESC";

            var list = new List<PhieuKiemKe>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new PhieuKiemKe
                {
                    MaKK = DbHelper.GetSafeInt(reader, "MaKK"),
                    SoPhieuKiemKe = DbHelper.GetSafeString(reader, "SoPhieuKiemKe"),
                    NgayKiemKe = DbHelper.GetSafeDateTime(reader, "NgayKiemKe"),
                    KhuVuc = DbHelper.GetSafeString(reader, "KhuVuc"),
                    TrangThaiDuyet = DbHelper.GetSafeString(reader, "TrangThaiDuyet"),
                    MaNVLap = DbHelper.GetSafeInt(reader, "MaNVLap"),
                    MaNVDuyet = DbHelper.GetSafeNullableInt(reader, "MaNVDuyet"),
                    TenNVLap = DbHelper.GetSafeString(reader, "TenNVLap"),
                    TenNVDuyet = DbHelper.GetSafeString(reader, "TenNVDuyet")
                });
            }
            return list;
        }

        public List<ChiTietKiemKe> GetChiTietKiemKe(int maKK)
        {
            const string sql = @"
                SELECT ct.MaCTKK, ct.MaKK, ct.MaLo, ct.SlHeThong, ct.SlThucTe, ct.ChenhLech,
                       h.TenHang, l.SoLo, l.HanSuDung
                FROM dbo.ChiTietKiemKe ct
                INNER JOIN dbo.LoHang l ON ct.MaLo = l.MaLo
                INNER JOIN dbo.HangHoa h ON l.MaHang = h.MaHang
                WHERE ct.MaKK = @MaKK";

            var list = new List<ChiTietKiemKe>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaKK", maKK);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChiTietKiemKe
                {
                    MaCTKK = DbHelper.GetSafeInt(reader, "MaCTKK"),
                    MaKK = DbHelper.GetSafeInt(reader, "MaKK"),
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    SlHeThong = DbHelper.GetSafeInt(reader, "SlHeThong"),
                    SlThucTe = DbHelper.GetSafeInt(reader, "SlThucTe"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                    HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung")
                });
            }
            return list;
        }
    }
}
