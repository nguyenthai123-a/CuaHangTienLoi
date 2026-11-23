using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class BanHangDao : BaseDao
    {
        public BanHangDao(IConfiguration configuration) : base(configuration) { }
        public BanHangDao(string connectionString) : base(connectionString) { }

        public int CreateHoaDon(HoaDon hd, SqlConnection conn, SqlTransaction trans)
        {
            // Tận dụng Sequence dbo.seq_HoaDon tự động sinh số HD
            const string sql = @"
                INSERT INTO dbo.HoaDon (SoHD, NgayGio, TongTien, GiamGia, TrangThai, DaXacNhanDoTuoi, MaCa, MaKH)
                VALUES (NEXT VALUE FOR dbo.seq_HoaDon, SYSDATETIME(), @TongTien, @GiamGia, 'HOAN_TAT', @DaXacNhanDoTuoi, @MaCa, @MaKH);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@TongTien", hd.TongTien);
            cmd.Parameters.AddWithValue("@GiamGia", hd.GiamGia);
            cmd.Parameters.AddWithValue("@DaXacNhanDoTuoi", hd.DaXacNhanDoTuoi);
            cmd.Parameters.AddWithValue("@MaCa", hd.MaCa);
            cmd.Parameters.AddWithValue("@MaKH", DbHelper.ToDbValue(hd.MaKH));

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void AddChiTietHoaDon(ChiTietHoaDon ct, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.ChiTietHoaDon (MaHoaDon, MaHang, MaLo, MaKM, SoLuong, DonGia, SoTienGiam, GiaVon)
                VALUES (@MaHoaDon, @MaHang, @MaLo, @MaKM, @SoLuong, @DonGia, @SoTienGiam, @GiaVon)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaHoaDon", ct.MaHoaDon);
            cmd.Parameters.AddWithValue("@MaHang", ct.MaHang);
            cmd.Parameters.AddWithValue("@MaLo", ct.MaLo);
            cmd.Parameters.AddWithValue("@MaKM", DbHelper.ToDbValue(ct.MaKM));
            cmd.Parameters.AddWithValue("@SoLuong", ct.SoLuong);
            cmd.Parameters.AddWithValue("@DonGia", ct.DonGia);
            cmd.Parameters.AddWithValue("@SoTienGiam", ct.SoTienGiam);
            cmd.Parameters.AddWithValue("@GiaVon", ct.GiaVon);
            cmd.ExecuteNonQuery();
        }

        public void AddThanhToan(ThanhToan tt, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.ThanhToan (MaHoaDon, LoaiThanhToan, SoTien, TrangThai, TienKhachDua, TienThoi, SoTheCuoi, MaChuanChi, MaGiaoDich, LoaiVi)
                VALUES (@MaHoaDon, @LoaiThanhToan, @SoTien, @TrangThai, @TienKhachDua, @TienThoi, @SoTheCuoi, @MaChuanChi, @MaGiaoDich, @LoaiVi)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaHoaDon", tt.MaHoaDon);
            cmd.Parameters.AddWithValue("@LoaiThanhToan", tt.LoaiThanhToan);
            cmd.Parameters.AddWithValue("@SoTien", tt.SoTien);
            cmd.Parameters.AddWithValue("@TrangThai", tt.TrangThai);
            cmd.Parameters.AddWithValue("@TienKhachDua", DbHelper.ToDbValue(tt.TienKhachDua));
            cmd.Parameters.AddWithValue("@TienThoi", DbHelper.ToDbValue(tt.TienThoi));
            cmd.Parameters.AddWithValue("@SoTheCuoi", DbHelper.ToDbValue(tt.SoTheCuoi));
            cmd.Parameters.AddWithValue("@MaChuanChi", DbHelper.ToDbValue(tt.MaChuanChi));
            cmd.Parameters.AddWithValue("@MaGiaoDich", DbHelper.ToDbValue(tt.MaGiaoDich));
            cmd.Parameters.AddWithValue("@LoaiVi", DbHelper.ToDbValue(tt.LoaiVi));
            cmd.ExecuteNonQuery();
        }

        public HoaDon? GetHoaDonById(int maHoaDon)
        {
            const string sql = @"
                SELECT hd.MaHoaDon, hd.SoHD, hd.SoHoaDon, hd.NgayGio, hd.TongTien, hd.GiamGia, hd.TrangThai, 
                       hd.DaXacNhanDoTuoi, hd.MaCa, hd.MaKH, hd.NguoiHuy, hd.LyDoHuy, hd.NgayHuy,
                       kh.HoTen AS TenKhachHang, kh.SoDienThoai AS SoDienThoaiKH,
                       nv.HoTen AS TenThuNgan, q.TenQuay
                FROM dbo.HoaDon hd
                INNER JOIN dbo.Ca c ON hd.MaCa = c.MaCa
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.KhachHang kh ON hd.MaKH = kh.MaKH
                WHERE hd.MaHoaDon = @MaHoaDon";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHoaDon", maHoaDon);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var hd = MapHoaDon(reader);
            reader.Close();

            // Load chi tiết hóa đơn
            hd.ChiTiet = GetChiTietByHoaDon(maHoaDon, conn);
            // Load thanh toán
            hd.ThanhToanList = GetThanhToanByHoaDon(maHoaDon, conn);

            return hd;
        }

        public HoaDon? GetHoaDonBySo(string soHoaDon)
        {
            const string sql = @"
                SELECT hd.MaHoaDon, hd.SoHD, hd.SoHoaDon, hd.NgayGio, hd.TongTien, hd.GiamGia, hd.TrangThai, 
                       hd.DaXacNhanDoTuoi, hd.MaCa, hd.MaKH, hd.NguoiHuy, hd.LyDoHuy, hd.NgayHuy,
                       kh.HoTen AS TenKhachHang, kh.SoDienThoai AS SoDienThoaiKH,
                       nv.HoTen AS TenThuNgan, q.TenQuay
                FROM dbo.HoaDon hd
                INNER JOIN dbo.Ca c ON hd.MaCa = c.MaCa
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.KhachHang kh ON hd.MaKH = kh.MaKH
                WHERE hd.SoHoaDon = @SoHoaDon";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SoHoaDon", soHoaDon.Trim());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var hd = MapHoaDon(reader);
            reader.Close();

            hd.ChiTiet = GetChiTietByHoaDon(hd.MaHoaDon, conn);
            hd.ThanhToanList = GetThanhToanByHoaDon(hd.MaHoaDon, conn);

            return hd;
        }

        public List<ChiTietHoaDon> GetChiTietByHoaDon(int maHoaDon, SqlConnection conn)
        {
            const string sql = @"
                SELECT ct.MaCT, ct.MaHoaDon, ct.MaHang, ct.MaLo, ct.MaKM, ct.SoLuong, ct.DonGia, 
                       ct.SoTienGiam, ct.GiaVon,
                       h.TenHang, h.DonViTinh, l.SoLo, l.HanSuDung
                FROM dbo.ChiTietHoaDon ct
                INNER JOIN dbo.HangHoa h ON ct.MaHang = h.MaHang
                INNER JOIN dbo.LoHang l ON ct.MaLo = l.MaLo
                WHERE ct.MaHoaDon = @MaHoaDon";

            var list = new List<ChiTietHoaDon>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHoaDon", maHoaDon);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChiTietHoaDon
                {
                    MaCT = DbHelper.GetSafeInt(reader, "MaCT"),
                    MaHoaDon = DbHelper.GetSafeInt(reader, "MaHoaDon"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    MaKM = DbHelper.GetSafeNullableInt(reader, "MaKM"),
                    SoLuong = DbHelper.GetSafeInt(reader, "SoLuong"),
                    DonGia = DbHelper.GetSafeDecimal(reader, "DonGia"),
                    SoTienGiam = DbHelper.GetSafeDecimal(reader, "SoTienGiam"),
                    GiaVon = DbHelper.GetSafeDecimal(reader, "GiaVon"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang"),
                    DonViTinh = DbHelper.GetSafeString(reader, "DonViTinh"),
                    SoLo = DbHelper.GetSafeString(reader, "SoLo"),
                    HanSuDung = DbHelper.GetSafeDateTime(reader, "HanSuDung")
                });
            }
            return list;
        }

        public List<ThanhToan> GetThanhToanByHoaDon(int maHoaDon, SqlConnection conn)
        {
            const string sql = @"
                SELECT MaTT, MaHoaDon, LoaiThanhToan, SoTien, TrangThai, TienKhachDua, TienThoi, 
                       SoTheCuoi, MaChuanChi, MaGiaoDich, LoaiVi
                FROM dbo.ThanhToan
                WHERE MaHoaDon = @MaHoaDon";

            var list = new List<ThanhToan>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHoaDon", maHoaDon);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ThanhToan
                {
                    MaTT = DbHelper.GetSafeInt(reader, "MaTT"),
                    MaHoaDon = DbHelper.GetSafeInt(reader, "MaHoaDon"),
                    LoaiThanhToan = DbHelper.GetSafeString(reader, "LoaiThanhToan"),
                    SoTien = DbHelper.GetSafeDecimal(reader, "SoTien"),
                    TrangThai = DbHelper.GetSafeString(reader, "TrangThai"),
                    TienKhachDua = DbHelper.GetSafeNullableDecimal(reader, "TienKhachDua"),
                    TienThoi = DbHelper.GetSafeNullableDecimal(reader, "TienThoi"),
                    SoTheCuoi = DbHelper.GetSafeString(reader, "SoTheCuoi"),
                    MaChuanChi = DbHelper.GetSafeString(reader, "MaChuanChi"),
                    MaGiaoDich = DbHelper.GetSafeString(reader, "MaGiaoDich"),
                    LoaiVi = DbHelper.GetSafeString(reader, "LoaiVi")
                });
            }
            return list;
        }

        public List<HoaDon> GetRecentInvoices(int limit = 50)
        {
            string sql = $@"
                SELECT TOP {limit} hd.MaHoaDon, hd.SoHD, hd.SoHoaDon, hd.NgayGio, hd.TongTien, hd.GiamGia, hd.TrangThai, 
                       hd.DaXacNhanDoTuoi, hd.MaCa, hd.MaKH, hd.NguoiHuy, hd.LyDoHuy, hd.NgayHuy,
                       kh.HoTen AS TenKhachHang, kh.SoDienThoai AS SoDienThoaiKH,
                       nv.HoTen AS TenThuNgan, q.TenQuay
                FROM dbo.HoaDon hd
                INNER JOIN dbo.Ca c ON hd.MaCa = c.MaCa
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.KhachHang kh ON hd.MaKH = kh.MaKH
                ORDER BY hd.MaHoaDon DESC";

            var list = new List<HoaDon>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapHoaDon(reader));
            }
            return list;
        }

        public List<HoaDon> GetInvoicesByCa(int maCa)
        {
            const string sql = @"
                SELECT hd.MaHoaDon, hd.SoHD, hd.SoHoaDon, hd.NgayGio, hd.TongTien, hd.GiamGia, hd.TrangThai, 
                       hd.DaXacNhanDoTuoi, hd.MaCa, hd.MaKH, hd.NguoiHuy, hd.LyDoHuy, hd.NgayHuy,
                       kh.HoTen AS TenKhachHang, kh.SoDienThoai AS SoDienThoaiKH,
                       nv.HoTen AS TenThuNgan, q.TenQuay
                FROM dbo.HoaDon hd
                INNER JOIN dbo.Ca c ON hd.MaCa = c.MaCa
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.KhachHang kh ON hd.MaKH = kh.MaKH
                WHERE hd.MaCa = @MaCa
                ORDER BY hd.MaHoaDon DESC";

            var list = new List<HoaDon>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaCa", maCa);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapHoaDon(reader));
            }
            return list;
        }

        public void CancelHoaDon(int maHoaDon, int nguoiHuy, string lyDoHuy, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.HoaDon
                SET TrangThai = 'DA_HUY',
                    NguoiHuy = @NguoiHuy,
                    LyDoHuy = @LyDoHuy,
                    NgayHuy = SYSDATETIME()
                WHERE MaHoaDon = @MaHoaDon AND TrangThai = 'HOAN_TAT'";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaHoaDon", maHoaDon);
            cmd.Parameters.AddWithValue("@NguoiHuy", nguoiHuy);
            cmd.Parameters.AddWithValue("@LyDoHuy", lyDoHuy.Trim());

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new InvalidOperationException("Hóa đơn không tồn tại hoặc đã bị hủy trước đó.");
        }

        private static HoaDon MapHoaDon(SqlDataReader reader)
        {
            return new HoaDon
            {
                MaHoaDon = DbHelper.GetSafeInt(reader, "MaHoaDon"),
                SoHD = DbHelper.GetSafeInt(reader, "SoHD"),
                SoHoaDon = DbHelper.GetSafeString(reader, "SoHoaDon"),
                NgayGio = DbHelper.GetSafeDateTime(reader, "NgayGio"),
                TongTien = DbHelper.GetSafeDecimal(reader, "TongTien"),
                GiamGia = DbHelper.GetSafeDecimal(reader, "GiamGia"),
                TrangThai = DbHelper.GetSafeString(reader, "TrangThai"),
                DaXacNhanDoTuoi = DbHelper.GetSafeBool(reader, "DaXacNhanDoTuoi"),
                MaCa = DbHelper.GetSafeInt(reader, "MaCa"),
                MaKH = DbHelper.GetSafeNullableInt(reader, "MaKH"),
                NguoiHuy = DbHelper.GetSafeNullableInt(reader, "NguoiHuy"),
                LyDoHuy = DbHelper.GetSafeString(reader, "LyDoHuy"),
                NgayHuy = DbHelper.GetSafeNullableDateTime(reader, "NgayHuy"),
                TenKhachHang = DbHelper.GetSafeString(reader, "TenKhachHang"),
                SoDienThoaiKH = DbHelper.GetSafeString(reader, "SoDienThoaiKH"),
                TenThuNgan = DbHelper.GetSafeString(reader, "TenThuNgan"),
                TenQuay = DbHelper.GetSafeString(reader, "TenQuay")
            };
        }
    }
}
