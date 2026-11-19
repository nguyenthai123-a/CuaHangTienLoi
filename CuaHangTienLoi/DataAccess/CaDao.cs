using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class CaDao : BaseDao
    {
        public CaDao(IConfiguration configuration) : base(configuration) { }
        public CaDao(string connectionString) : base(connectionString) { }

        public Ca? GetOpenShiftByQuay(int maQuay)
        {
            const string sql = @"
                SELECT c.MaCa, c.MaQuay, c.MaThuNgan, c.MaNguoiChot, c.GioMo, c.GioDong,
                       c.TienDauCa, c.TienMatHeThong, c.TienMatThucTe, c.LyDoChenhLech, c.TrangThai,
                       q.TenQuay, nv.HoTen AS TenThuNgan
                FROM dbo.Ca c
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                WHERE c.MaQuay = @MaQuay AND c.TrangThai = 'MO'";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaQuay", maQuay);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return MapCa(reader);
        }

        public Ca? GetOpenShiftByThuNgan(int maThuNgan)
        {
            const string sql = @"
                SELECT c.MaCa, c.MaQuay, c.MaThuNgan, c.MaNguoiChot, c.GioMo, c.GioDong,
                       c.TienDauCa, c.TienMatHeThong, c.TienMatThucTe, c.LyDoChenhLech, c.TrangThai,
                       q.TenQuay, nv.HoTen AS TenThuNgan
                FROM dbo.Ca c
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                WHERE c.MaThuNgan = @MaThuNgan AND c.TrangThai = 'MO'";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaThuNgan", maThuNgan);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return MapCa(reader);
        }

        public Ca? GetById(int maCa)
        {
            const string sql = @"
                SELECT c.MaCa, c.MaQuay, c.MaThuNgan, c.MaNguoiChot, c.GioMo, c.GioDong,
                       c.TienDauCa, c.TienMatHeThong, c.TienMatThucTe, c.LyDoChenhLech, c.TrangThai,
                       q.TenQuay, nv.HoTen AS TenThuNgan, nvChot.HoTen AS TenNguoiChot
                FROM dbo.Ca c
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.NhanVien nvChot ON c.MaNguoiChot = nvChot.MaNV
                WHERE c.MaCa = @MaCa";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaCa", maCa);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var ca = MapCa(reader);
            ca.TenNguoiChot = DbHelper.GetSafeString(reader, "TenNguoiChot");
            return ca;
        }

        public int OpenShift(int maQuay, int maThuNgan, decimal tienDauCa)
        {
            // Kiểm tra quy tắc nghiệp vụ: Mỗi quầy chỉ có DUY NHẤT 1 ca đang mở (UX_Ca_QuayDangMo)
            const string checkSql = "SELECT COUNT(1) FROM dbo.Ca WHERE MaQuay = @MaQuay AND TrangThai = 'MO'";
            const string insertSql = @"
                INSERT INTO dbo.Ca (MaQuay, MaThuNgan, GioMo, TienDauCa, TrangThai)
                VALUES (@MaQuay, @MaThuNgan, SYSDATETIME(), @TienDauCa, 'MO');
                SELECT SCOPE_IDENTITY();";

            using var conn = OpenConnection();
            using var checkCmd = new SqlCommand(checkSql, conn);
            checkCmd.Parameters.AddWithValue("@MaQuay", maQuay);
            int openCount = Convert.ToInt32(checkCmd.ExecuteScalar());
            if (openCount > 0)
            {
                throw new InvalidOperationException($"Quầy {maQuay} hiện đang có một ca làm việc chưa đóng. Mỗi quầy chỉ được mở 1 ca tại một thời điểm!");
            }

            using var cmd = new SqlCommand(insertSql, conn);
            cmd.Parameters.AddWithValue("@MaQuay", maQuay);
            cmd.Parameters.AddWithValue("@MaThuNgan", maThuNgan);
            cmd.Parameters.AddWithValue("@TienDauCa", tienDauCa);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        /// <summary>
        /// Tính toán tổng doanh thu theo từng phương thức (Tiền mặt, Thẻ, QR) trong ca
        /// </summary>
        public (decimal TienMat, decimal The, decimal QR) CalculateShiftRevenue(int maCa)
        {
            const string sql = @"
                SELECT 
                    ISNULL(SUM(CASE WHEN tt.LoaiThanhToan = 'TIEN_MAT' THEN tt.SoTien ELSE 0 END), 0) AS DoanhThuTienMat,
                    ISNULL(SUM(CASE WHEN tt.LoaiThanhToan = 'THE' THEN tt.SoTien ELSE 0 END), 0) AS DoanhThuThe,
                    ISNULL(SUM(CASE WHEN tt.LoaiThanhToan = 'QR' THEN tt.SoTien ELSE 0 END), 0) AS DoanhThuQR
                FROM dbo.ThanhToan tt
                INNER JOIN dbo.HoaDon hd ON tt.MaHoaDon = hd.MaHoaDon
                WHERE hd.MaCa = @MaCa AND hd.TrangThai = 'HOAN_TAT' AND tt.TrangThai = 'THANH_CONG'";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaCa", maCa);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                decimal tm = DbHelper.GetSafeDecimal(reader, "DoanhThuTienMat");
                decimal the = DbHelper.GetSafeDecimal(reader, "DoanhThuThe");
                decimal qr = DbHelper.GetSafeDecimal(reader, "DoanhThuQR");
                return (tm, the, qr);
            }
            return (0m, 0m, 0m);
        }

        public void CloseShift(int maCa, int maNguoiChot, decimal tienMatHeThong, decimal tienMatThucTe, string? lyDoChenhLech, decimal doanhThuTienMat, decimal doanhThuThe, decimal doanhThuQR)
        {
            const string sqlUpdateCa = @"
                UPDATE dbo.Ca
                SET TrangThai = 'DONG',
                    GioDong = SYSDATETIME(),
                    MaNguoiChot = @MaNguoiChot,
                    TienMatHeThong = @TienMatHeThong,
                    TienMatThucTe = @TienMatThucTe,
                    LyDoChenhLech = @LyDoChenhLech
                WHERE MaCa = @MaCa AND TrangThai = 'MO'";

            const string sqlInsertBC = @"
                INSERT INTO dbo.BaoCaoCa (MaCa, NgayLap, DoanhThuTienMat, DoanhThuThe, DoanhThuQR)
                VALUES (@MaCa, SYSDATETIME(), @DoanhThuTienMat, @DoanhThuThe, @DoanhThuQR)";

            using var conn = OpenConnection();
            using var trans = conn.BeginTransaction();
            try
            {
                using var cmd1 = new SqlCommand(sqlUpdateCa, conn, trans);
                cmd1.Parameters.AddWithValue("@MaCa", maCa);
                cmd1.Parameters.AddWithValue("@MaNguoiChot", maNguoiChot);
                cmd1.Parameters.AddWithValue("@TienMatHeThong", tienMatHeThong);
                cmd1.Parameters.AddWithValue("@TienMatThucTe", tienMatThucTe);
                cmd1.Parameters.AddWithValue("@LyDoChenhLech", DbHelper.ToDbValue(lyDoChenhLech));
                int affected = cmd1.ExecuteNonQuery();
                if (affected == 0)
                    throw new InvalidOperationException("Ca làm việc không tồn tại hoặc đã được đóng trước đó.");

                using var cmd2 = new SqlCommand(sqlInsertBC, conn, trans);
                cmd2.Parameters.AddWithValue("@MaCa", maCa);
                cmd2.Parameters.AddWithValue("@DoanhThuTienMat", doanhThuTienMat);
                cmd2.Parameters.AddWithValue("@DoanhThuThe", doanhThuThe);
                cmd2.Parameters.AddWithValue("@DoanhThuQR", doanhThuQR);
                cmd2.ExecuteNonQuery();

                trans.Commit();
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public BaoCaoCa? GetBaoCaoCa(int maCa)
        {
            const string sql = @"
                SELECT bc.MaBC, bc.MaCa, bc.NgayLap, bc.DoanhThuTienMat, bc.DoanhThuThe, bc.DoanhThuQR, bc.TongDoanhThu,
                       c.MaQuay, c.MaThuNgan, c.GioMo, c.GioDong, c.TienDauCa, c.TienMatHeThong, c.TienMatThucTe, c.LyDoChenhLech,
                       q.TenQuay, nv.HoTen AS TenThuNgan, nvChot.HoTen AS TenNguoiChot
                FROM dbo.BaoCaoCa bc
                INNER JOIN dbo.Ca c ON bc.MaCa = c.MaCa
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.NhanVien nvChot ON c.MaNguoiChot = nvChot.MaNV
                WHERE bc.MaCa = @MaCa";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaCa", maCa);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new BaoCaoCa
            {
                MaBC = DbHelper.GetSafeInt(reader, "MaBC"),
                MaCa = DbHelper.GetSafeInt(reader, "MaCa"),
                NgayLap = DbHelper.GetSafeDateTime(reader, "NgayLap"),
                DoanhThuTienMat = DbHelper.GetSafeDecimal(reader, "DoanhThuTienMat"),
                DoanhThuThe = DbHelper.GetSafeDecimal(reader, "DoanhThuThe"),
                DoanhThuQR = DbHelper.GetSafeDecimal(reader, "DoanhThuQR"),
                Ca = new Ca
                {
                    MaCa = DbHelper.GetSafeInt(reader, "MaCa"),
                    MaQuay = DbHelper.GetSafeInt(reader, "MaQuay"),
                    MaThuNgan = DbHelper.GetSafeInt(reader, "MaThuNgan"),
                    GioMo = DbHelper.GetSafeDateTime(reader, "GioMo"),
                    GioDong = DbHelper.GetSafeNullableDateTime(reader, "GioDong"),
                    TienDauCa = DbHelper.GetSafeDecimal(reader, "TienDauCa"),
                    TienMatHeThong = DbHelper.GetSafeNullableDecimal(reader, "TienMatHeThong"),
                    TienMatThucTe = DbHelper.GetSafeNullableDecimal(reader, "TienMatThucTe"),
                    LyDoChenhLech = DbHelper.GetSafeString(reader, "LyDoChenhLech"),
                    TenQuay = DbHelper.GetSafeString(reader, "TenQuay"),
                    TenThuNgan = DbHelper.GetSafeString(reader, "TenThuNgan"),
                    TenNguoiChot = DbHelper.GetSafeString(reader, "TenNguoiChot")
                }
            };
        }

        public List<Ca> GetAllShifts(int limit = 50)
        {
            string sql = $@"
                SELECT TOP {limit} c.MaCa, c.MaQuay, c.MaThuNgan, c.MaNguoiChot, c.GioMo, c.GioDong,
                       c.TienDauCa, c.TienMatHeThong, c.TienMatThucTe, c.LyDoChenhLech, c.TrangThai,
                       q.TenQuay, nv.HoTen AS TenThuNgan, nvChot.HoTen AS TenNguoiChot
                FROM dbo.Ca c
                INNER JOIN dbo.Quay q ON c.MaQuay = q.MaQuay
                INNER JOIN dbo.NhanVien nv ON c.MaThuNgan = nv.MaNV
                LEFT JOIN dbo.NhanVien nvChot ON c.MaNguoiChot = nvChot.MaNV
                ORDER BY c.MaCa DESC";

            var list = new List<Ca>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var ca = MapCa(reader);
                ca.TenNguoiChot = DbHelper.GetSafeString(reader, "TenNguoiChot");
                list.Add(ca);
            }
            return list;
        }

        private static Ca MapCa(SqlDataReader reader)
        {
            return new Ca
            {
                MaCa = DbHelper.GetSafeInt(reader, "MaCa"),
                MaQuay = DbHelper.GetSafeInt(reader, "MaQuay"),
                MaThuNgan = DbHelper.GetSafeInt(reader, "MaThuNgan"),
                MaNguoiChot = DbHelper.GetSafeNullableInt(reader, "MaNguoiChot"),
                GioMo = DbHelper.GetSafeDateTime(reader, "GioMo"),
                GioDong = DbHelper.GetSafeNullableDateTime(reader, "GioDong"),
                TienDauCa = DbHelper.GetSafeDecimal(reader, "TienDauCa"),
                TienMatHeThong = DbHelper.GetSafeNullableDecimal(reader, "TienMatHeThong"),
                TienMatThucTe = DbHelper.GetSafeNullableDecimal(reader, "TienMatThucTe"),
                LyDoChenhLech = DbHelper.GetSafeString(reader, "LyDoChenhLech"),
                TrangThai = DbHelper.GetSafeString(reader, "TrangThai"),
                TenQuay = DbHelper.GetSafeString(reader, "TenQuay"),
                TenThuNgan = DbHelper.GetSafeString(reader, "TenThuNgan")
            };
        }
    }
}
