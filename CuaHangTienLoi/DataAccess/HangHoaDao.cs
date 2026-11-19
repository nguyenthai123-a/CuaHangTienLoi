using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class HangHoaDao : BaseDao
    {
        public HangHoaDao(IConfiguration configuration) : base(configuration) { }
        public HangHoaDao(string connectionString) : base(connectionString) { }

        public List<HangHoa> GetAll(int? maNhom = null, string? search = null, bool includeInactive = false)
        {
            string sql = @"
                SELECT h.MaHang, h.MaVach, h.TenHang, h.MaNhom, h.DonViTinh, h.GiaBan, h.GiaNhap, 
                       h.DinhMucTon, h.GioiHanDoTuoi, h.SoNgayBaoQuanMacDinh, h.TrangThaiKinhDoanh,
                       nh.TenNhom,
                       ISNULL((SELECT SUM(SlTonKho) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) AS TongTonKho,
                       ISNULL((SELECT SUM(SlTonQuay) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) AS TongTonQuay
                FROM dbo.HangHoa h
                INNER JOIN dbo.NhomHang nh ON h.MaNhom = nh.MaNhom
                WHERE 1=1";

            if (!includeInactive)
                sql += " AND h.TrangThaiKinhDoanh = 1";

            if (maNhom.HasValue && maNhom.Value > 0)
                sql += " AND h.MaNhom = @MaNhom";

            if (!string.IsNullOrWhiteSpace(search))
                sql += " AND (h.TenHang LIKE @Search OR h.MaVach LIKE @Search)";

            sql += " ORDER BY h.MaHang ASC";

            var list = new List<HangHoa>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);

            if (maNhom.HasValue && maNhom.Value > 0)
                cmd.Parameters.AddWithValue("@MaNhom", maNhom.Value);

            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@Search", $"%{search.Trim()}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new HangHoa
                {
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    MaNhom = DbHelper.GetSafeInt(reader, "MaNhom"),
                    DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                    GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan"),
                    GiaNhap = DbHelper.GetSafeNullableDecimal(reader, "GiaNhap"),
                    DinhMucTon = DbHelper.GetSafeInt(reader, "DinhMucTon"),
                    GioiHanDoTuoi = DbHelper.GetSafeInt(reader, "GioiHanDoTuoi"),
                    SoNgayBaoQuanMacDinh = DbHelper.GetSafeNullableInt(reader, "SoNgayBaoQuanMacDinh"),
                    TrangThaiKinhDoanh = DbHelper.GetSafeBool(reader, "TrangThaiKinhDoanh"),
                    TenNhom = DbHelper.GetSafeString(reader, "TenNhom"),
                    TongTonKho = DbHelper.GetSafeInt(reader, "TongTonKho"),
                    TongTonQuay = DbHelper.GetSafeInt(reader, "TongTonQuay")
                });
            }
            return list;
        }

        public HangHoa? GetById(int maHang)
        {
            const string sql = @"
                SELECT h.MaHang, h.MaVach, h.TenHang, h.MaNhom, h.DonViTinh, h.GiaBan, h.GiaNhap, 
                       h.DinhMucTon, h.GioiHanDoTuoi, h.SoNgayBaoQuanMacDinh, h.TrangThaiKinhDoanh,
                       nh.TenNhom,
                       ISNULL((SELECT SUM(SlTonKho) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) AS TongTonKho,
                       ISNULL((SELECT SUM(SlTonQuay) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) AS TongTonQuay
                FROM dbo.HangHoa h
                INNER JOIN dbo.NhomHang nh ON h.MaNhom = nh.MaNhom
                WHERE h.MaHang = @MaHang";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHang", maHang);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new HangHoa
            {
                MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                MaNhom = DbHelper.GetSafeInt(reader, "MaNhom"),
                DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan"),
                GiaNhap = DbHelper.GetSafeNullableDecimal(reader, "GiaNhap"),
                DinhMucTon = DbHelper.GetSafeInt(reader, "DinhMucTon"),
                GioiHanDoTuoi = DbHelper.GetSafeInt(reader, "GioiHanDoTuoi"),
                SoNgayBaoQuanMacDinh = DbHelper.GetSafeNullableInt(reader, "SoNgayBaoQuanMacDinh"),
                TrangThaiKinhDoanh = DbHelper.GetSafeBool(reader, "TrangThaiKinhDoanh"),
                TenNhom = DbHelper.GetSafeString(reader, "TenNhom"),
                TongTonKho = DbHelper.GetSafeInt(reader, "TongTonKho"),
                TongTonQuay = DbHelper.GetSafeInt(reader, "TongTonQuay")
            };
        }

        public HangHoa? GetByBarcode(string maVach)
        {
            const string sql = @"
                SELECT h.MaHang, h.MaVach, h.TenHang, h.MaNhom, h.DonViTinh, h.GiaBan, h.GiaNhap, 
                       h.DinhMucTon, h.GioiHanDoTuoi, h.SoNgayBaoQuanMacDinh, h.TrangThaiKinhDoanh,
                       nh.TenNhom,
                       ISNULL((SELECT SUM(SlTonKho) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) AS TongTonKho,
                       ISNULL((SELECT SUM(SlTonQuay) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) AS TongTonQuay
                FROM dbo.HangHoa h
                INNER JOIN dbo.NhomHang nh ON h.MaNhom = nh.MaNhom
                WHERE h.MaVach = @MaVach AND h.TrangThaiKinhDoanh = 1";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaVach", maVach.Trim());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new HangHoa
            {
                MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                MaVach = DbHelper.GetSafeString(reader, "MaVach"),
                TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                MaNhom = DbHelper.GetSafeInt(reader, "MaNhom"),
                DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                GiaBan = DbHelper.GetSafeDecimal(reader, "GiaBan"),
                GiaNhap = DbHelper.GetSafeNullableDecimal(reader, "GiaNhap"),
                DinhMucTon = DbHelper.GetSafeInt(reader, "DinhMucTon"),
                GioiHanDoTuoi = DbHelper.GetSafeInt(reader, "GioiHanDoTuoi"),
                SoNgayBaoQuanMacDinh = DbHelper.GetSafeNullableInt(reader, "SoNgayBaoQuanMacDinh"),
                TrangThaiKinhDoanh = DbHelper.GetSafeBool(reader, "TrangThaiKinhDoanh"),
                TenNhom = DbHelper.GetSafeString(reader, "TenNhom"),
                TongTonKho = DbHelper.GetSafeInt(reader, "TongTonKho"),
                TongTonQuay = DbHelper.GetSafeInt(reader, "TongTonQuay")
            };
        }

        public int Insert(HangHoa h)
        {
            const string sql = @"
                INSERT INTO dbo.HangHoa (MaVach, TenHang, MaNhom, DonViTinh, GiaBan, GiaNhap, DinhMucTon, GioiHanDoTuoi, SoNgayBaoQuanMacDinh, TrangThaiKinhDoanh)
                VALUES (@MaVach, @TenHang, @MaNhom, @DonViTinh, @GiaBan, @GiaNhap, @DinhMucTon, @GioiHanDoTuoi, @SoNgayBaoQuanMacDinh, @TrangThaiKinhDoanh);
                SELECT SCOPE_IDENTITY();";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaVach", h.MaVach.Trim());
            cmd.Parameters.AddWithValue("@TenHang", h.TenHang.Trim());
            cmd.Parameters.AddWithValue("@MaNhom", h.MaNhom);
            cmd.Parameters.AddWithValue("@DonViTinh", h.DonViTinh.Trim());
            cmd.Parameters.AddWithValue("@GiaBan", h.GiaBan);
            cmd.Parameters.AddWithValue("@GiaNhap", DbHelper.ToDbValue(h.GiaNhap));
            cmd.Parameters.AddWithValue("@DinhMucTon", h.DinhMucTon);
            cmd.Parameters.AddWithValue("@GioiHanDoTuoi", h.GioiHanDoTuoi);
            cmd.Parameters.AddWithValue("@SoNgayBaoQuanMacDinh", DbHelper.ToDbValue(h.SoNgayBaoQuanMacDinh));
            cmd.Parameters.AddWithValue("@TrangThaiKinhDoanh", h.TrangThaiKinhDoanh);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Update(HangHoa h)
        {
            const string sql = @"
                UPDATE dbo.HangHoa
                SET MaVach = @MaVach,
                    TenHang = @TenHang,
                    MaNhom = @MaNhom,
                    DonViTinh = @DonViTinh,
                    GiaBan = @GiaBan,
                    GiaNhap = @GiaNhap,
                    DinhMucTon = @DinhMucTon,
                    GioiHanDoTuoi = @GioiHanDoTuoi,
                    SoNgayBaoQuanMacDinh = @SoNgayBaoQuanMacDinh,
                    TrangThaiKinhDoanh = @TrangThaiKinhDoanh
                WHERE MaHang = @MaHang";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHang", h.MaHang);
            cmd.Parameters.AddWithValue("@MaVach", h.MaVach.Trim());
            cmd.Parameters.AddWithValue("@TenHang", h.TenHang.Trim());
            cmd.Parameters.AddWithValue("@MaNhom", h.MaNhom);
            cmd.Parameters.AddWithValue("@DonViTinh", h.DonViTinh.Trim());
            cmd.Parameters.AddWithValue("@GiaBan", h.GiaBan);
            cmd.Parameters.AddWithValue("@GiaNhap", DbHelper.ToDbValue(h.GiaNhap));
            cmd.Parameters.AddWithValue("@DinhMucTon", h.DinhMucTon);
            cmd.Parameters.AddWithValue("@GioiHanDoTuoi", h.GioiHanDoTuoi);
            cmd.Parameters.AddWithValue("@SoNgayBaoQuanMacDinh", DbHelper.ToDbValue(h.SoNgayBaoQuanMacDinh));
            cmd.Parameters.AddWithValue("@TrangThaiKinhDoanh", h.TrangThaiKinhDoanh);
            cmd.ExecuteNonQuery();
        }

        public void UpdatePrice(int maHang, decimal newPrice)
        {
            const string sql = "UPDATE dbo.HangHoa SET GiaBan = @GiaBan WHERE MaHang = @MaHang";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHang", maHang);
            cmd.Parameters.AddWithValue("@GiaBan", newPrice);
            cmd.ExecuteNonQuery();
        }

        public List<NhomHang> GetAllCategories()
        {
            const string sql = "SELECT MaNhom, TenNhom FROM dbo.NhomHang ORDER BY MaNhom";
            var list = new List<NhomHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new NhomHang
                {
                    MaNhom = DbHelper.GetSafeInt(reader, "MaNhom"),
                    TenNhom = DbHelper.GetSafeString(reader, "TenNhom")
                });
            }
            return list;
        }

        public List<NhaCungCap> GetAllSuppliers()
        {
            const string sql = "SELECT MaNCC, TenNCC, SoDienThoai, DiaChi, NguoiLienHe FROM dbo.NhaCungCap ORDER BY TenNCC";
            var list = new List<NhaCungCap>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new NhaCungCap
                {
                    MaNCC = DbHelper.GetSafeInt(reader, "MaNCC"),
                    TenNCC = DbHelper.GetSafeString(reader, "TenNCC"),
                    SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                    DiaChi = DbHelper.GetSafeString(reader, "DiaChi"),
                    NguoiLienHe = DbHelper.GetSafeString(reader, "NguoiLienHe")
                });
            }
            return list;
        }
    }
}
