using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class DoiTraDao : BaseDao
    {
        public DoiTraDao(IConfiguration configuration) : base(configuration) { }
        public DoiTraDao(string connectionString) : base(connectionString) { }

        public int CreatePhieuTra(PhieuTraHang pt, List<ChiTietPhieuTra> chiTiet, SqlConnection conn, SqlTransaction trans)
        {
            const string sqlPhieu = @"
                INSERT INTO dbo.PhieuTraHang (MaHoaDon, MaCa, NgayTra, LyDo, TrangThaiDuyet, MaNVLap)
                VALUES (@MaHoaDon, @MaCa, CONVERT(date, SYSDATETIME()), @LyDo, 'CHO_DUYET', @MaNVLap);
                SELECT SCOPE_IDENTITY();";

            using var cmd = new SqlCommand(sqlPhieu, conn, trans);
            cmd.Parameters.AddWithValue("@MaHoaDon", pt.MaHoaDon);
            cmd.Parameters.AddWithValue("@MaCa", DbHelper.ToDbValue(pt.MaCa));
            cmd.Parameters.AddWithValue("@LyDo", pt.LyDo.Trim());
            cmd.Parameters.AddWithValue("@MaNVLap", pt.MaNVLap);

            int maPhieuTra = Convert.ToInt32(cmd.ExecuteScalar());

            const string sqlCT = @"
                INSERT INTO dbo.ChiTietPhieuTra (MaPhieuTra, MaCT, SoLuong, TinhTrangHang)
                VALUES (@MaPhieuTra, @MaCT, @SoLuong, @TinhTrangHang)";

            foreach (var item in chiTiet)
            {
                using var cmdCT = new SqlCommand(sqlCT, conn, trans);
                cmdCT.Parameters.AddWithValue("@MaPhieuTra", maPhieuTra);
                cmdCT.Parameters.AddWithValue("@MaCT", item.MaCT);
                cmdCT.Parameters.AddWithValue("@SoLuong", item.SoLuong);
                cmdCT.Parameters.AddWithValue("@TinhTrangHang", item.TinhTrangHang);
                cmdCT.ExecuteNonQuery();
            }

            return maPhieuTra;
        }

        /// <summary>
        /// Phê duyệt phiếu trả hàng: Cấp số phiếu trả từ Sequence dbo.seq_PhieuTra, 
        /// cập nhật số tiền hoàn, phương thức hoàn và mã người duyệt (đáp ứng ràng buộc CK_PTH_Duyet)
        /// </summary>
        public void ApprovePhieuTra(int maPhieuTra, int maNVDuyet, decimal soTienHoan, string phuongThucHoan, string? maGiaoDichHoan, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.PhieuTraHang
                SET SoPT = NEXT VALUE FOR dbo.seq_PhieuTra,
                    SoTienHoan = @SoTienHoan,
                    PhuongThucHoan = @PhuongThucHoan,
                    MaGiaoDichHoan = @MaGiaoDichHoan,
                    TrangThaiDuyet = 'DA_DUYET',
                    MaNVDuyet = @MaNVDuyet
                WHERE MaPhieuTra = @MaPhieuTra AND TrangThaiDuyet = 'CHO_DUYET'";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaPhieuTra", maPhieuTra);
            cmd.Parameters.AddWithValue("@MaNVDuyet", maNVDuyet);
            cmd.Parameters.AddWithValue("@SoTienHoan", soTienHoan);
            cmd.Parameters.AddWithValue("@PhuongThucHoan", phuongThucHoan);
            cmd.Parameters.AddWithValue("@MaGiaoDichHoan", DbHelper.ToDbValue(maGiaoDichHoan));

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new InvalidOperationException("Phiếu trả hàng không tồn tại hoặc đã được xử lý trước đó.");
        }

        public void RejectPhieuTra(int maPhieuTra, int maNVDuyet, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.PhieuTraHang
                SET TrangThaiDuyet = 'TU_CHOI',
                    MaNVDuyet = @MaNVDuyet
                WHERE MaPhieuTra = @MaPhieuTra AND TrangThaiDuyet = 'CHO_DUYET'";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaPhieuTra", maPhieuTra);
            cmd.Parameters.AddWithValue("@MaNVDuyet", maNVDuyet);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new InvalidOperationException("Phiếu trả hàng không tồn tại hoặc đã được xử lý trước đó.");
        }

        public PhieuTraHang? GetById(int maPhieuTra)
        {
            const string sql = @"
                SELECT pt.MaPhieuTra, pt.SoPT, pt.SoPhieuTra, pt.MaHoaDon, pt.MaCa, pt.NgayTra, pt.LyDo,
                       pt.SoTienHoan, pt.PhuongThucHoan, pt.MaGiaoDichHoan, pt.TrangThaiDuyet,
                       pt.MaNVLap, pt.MaNVDuyet,
                       hd.SoHoaDon,
                       nvLap.HoTen AS TenNVLap, nvDuyet.HoTen AS TenNVDuyet
                FROM dbo.PhieuTraHang pt
                INNER JOIN dbo.HoaDon hd ON pt.MaHoaDon = hd.MaHoaDon
                INNER JOIN dbo.NhanVien nvLap ON pt.MaNVLap = nvLap.MaNV
                LEFT JOIN dbo.NhanVien nvDuyet ON pt.MaNVDuyet = nvDuyet.MaNV
                WHERE pt.MaPhieuTra = @MaPhieuTra";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaPhieuTra", maPhieuTra);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var pt = MapPhieuTra(reader);
            reader.Close();

            pt.ChiTiet = GetChiTietByPhieuTra(maPhieuTra, conn);
            return pt;
        }

        public List<PhieuTraHang> GetPendingReturns()
        {
            const string sql = @"
                SELECT pt.MaPhieuTra, pt.SoPT, pt.SoPhieuTra, pt.MaHoaDon, pt.MaCa, pt.NgayTra, pt.LyDo,
                       pt.SoTienHoan, pt.PhuongThucHoan, pt.MaGiaoDichHoan, pt.TrangThaiDuyet,
                       pt.MaNVLap, pt.MaNVDuyet,
                       hd.SoHoaDon,
                       nvLap.HoTen AS TenNVLap, nvDuyet.HoTen AS TenNVDuyet
                FROM dbo.PhieuTraHang pt
                INNER JOIN dbo.HoaDon hd ON pt.MaHoaDon = hd.MaHoaDon
                INNER JOIN dbo.NhanVien nvLap ON pt.MaNVLap = nvLap.MaNV
                LEFT JOIN dbo.NhanVien nvDuyet ON pt.MaNVDuyet = nvDuyet.MaNV
                WHERE pt.TrangThaiDuyet = 'CHO_DUYET'
                ORDER BY pt.MaPhieuTra ASC";

            var list = new List<PhieuTraHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapPhieuTra(reader));
            }
            return list;
        }

        public List<PhieuTraHang> GetAllReturns()
        {
            const string sql = @"
                SELECT pt.MaPhieuTra, pt.SoPT, pt.SoPhieuTra, pt.MaHoaDon, pt.MaCa, pt.NgayTra, pt.LyDo,
                       pt.SoTienHoan, pt.PhuongThucHoan, pt.MaGiaoDichHoan, pt.TrangThaiDuyet,
                       pt.MaNVLap, pt.MaNVDuyet,
                       hd.SoHoaDon,
                       nvLap.HoTen AS TenNVLap, nvDuyet.HoTen AS TenNVDuyet
                FROM dbo.PhieuTraHang pt
                INNER JOIN dbo.HoaDon hd ON pt.MaHoaDon = hd.MaHoaDon
                INNER JOIN dbo.NhanVien nvLap ON pt.MaNVLap = nvLap.MaNV
                LEFT JOIN dbo.NhanVien nvDuyet ON pt.MaNVDuyet = nvDuyet.MaNV
                ORDER BY pt.MaPhieuTra DESC";

            var list = new List<PhieuTraHang>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapPhieuTra(reader));
            }
            return list;
        }

        public List<ChiTietPhieuTra> GetChiTietByPhieuTra(int maPhieuTra, SqlConnection conn)
        {
            const string sql = @"
                SELECT ctpt.MaCTPT, ctpt.MaPhieuTra, ctpt.MaCT, ctpt.SoLuong, ctpt.TinhTrangHang,
                       cthd.MaHang, cthd.MaLo, cthd.DonGia,
                       h.TenHang
                FROM dbo.ChiTietPhieuTra ctpt
                INNER JOIN dbo.ChiTietHoaDon cthd ON ctpt.MaCT = cthd.MaCT
                INNER JOIN dbo.HangHoa h ON cthd.MaHang = h.MaHang
                WHERE ctpt.MaPhieuTra = @MaPhieuTra";

            var list = new List<ChiTietPhieuTra>();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaPhieuTra", maPhieuTra);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ChiTietPhieuTra
                {
                    MaCTPT = DbHelper.GetSafeInt(reader, "MaCTPT"),
                    MaPhieuTra = DbHelper.GetSafeInt(reader, "MaPhieuTra"),
                    MaCT = DbHelper.GetSafeInt(reader, "MaCT"),
                    SoLuong = DbHelper.GetSafeInt(reader, "SoLuong"),
                    TinhTrangHang = DbHelper.GetSafeString(reader, "TinhTrangHang"),
                    MaHang = DbHelper.GetSafeInt(reader, "MaHang"),
                    MaLo = DbHelper.GetSafeInt(reader, "MaLo"),
                    DonGia = DbHelper.GetSafeDecimal(reader, "DonGia"),
                    TenHang = DbHelper.GetSafeString(reader, "TenHang")
                });
            }
            return list;
        }

        private static PhieuTraHang MapPhieuTra(SqlDataReader reader)
        {
            return new PhieuTraHang
            {
                MaPhieuTra = DbHelper.GetSafeInt(reader, "MaPhieuTra"),
                SoPT = DbHelper.GetSafeNullableInt(reader, "SoPT"),
                SoPhieuTra = DbHelper.GetSafeString(reader, "SoPhieuTra"),
                MaHoaDon = DbHelper.GetSafeInt(reader, "MaHoaDon"),
                MaCa = DbHelper.GetSafeNullableInt(reader, "MaCa"),
                NgayTra = DbHelper.GetSafeDateTime(reader, "NgayTra"),
                LyDo = DbHelper.GetSafeString(reader, "LyDo"),
                SoTienHoan = DbHelper.GetSafeNullableDecimal(reader, "SoTienHoan"),
                PhuongThucHoan = DbHelper.GetSafeString(reader, "PhuongThucHoan"),
                MaGiaoDichHoan = DbHelper.GetSafeString(reader, "MaGiaoDichHoan"),
                TrangThaiDuyet = DbHelper.GetSafeString(reader, "TrangThaiDuyet"),
                MaNVLap = DbHelper.GetSafeInt(reader, "MaNVLap"),
                MaNVDuyet = DbHelper.GetSafeNullableInt(reader, "MaNVDuyet"),
                SoHoaDon = DbHelper.GetSafeString(reader, "SoHoaDon"),
                TenNVLap = DbHelper.GetSafeString(reader, "TenNVLap"),
                TenNVDuyet = DbHelper.GetSafeString(reader, "TenNVDuyet")
            };
        }
    }
}
