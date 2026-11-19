using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class NhatKyDao : BaseDao
    {
        public NhatKyDao(IConfiguration configuration) : base(configuration) { }
        public NhatKyDao(string connectionString) : base(connectionString) { }

        public void InsertLog(NhatKyKiemToan nk, SqlConnection? conn = null, SqlTransaction? trans = null)
        {
            const string sql = @"
                INSERT INTO dbo.NhatKyKiemToan (MaTK, ThoiGian, HanhDong, DoiTuong, MaDoiTuong, DiaChiIP, ThietBi, GiaTriCu, GiaTriMoi)
                VALUES (@MaTK, SYSDATETIME(), @HanhDong, @DoiTuong, @MaDoiTuong, @DiaChiIP, @ThietBi, @GiaTriCu, @GiaTriMoi)";

            bool shouldClose = false;
            if (conn == null)
            {
                conn = OpenConnection();
                shouldClose = true;
            }

            try
            {
                using var cmd = new SqlCommand(sql, conn, trans);
                cmd.Parameters.AddWithValue("@MaTK", nk.MaTK);
                cmd.Parameters.AddWithValue("@HanhDong", nk.HanhDong.Trim());
                cmd.Parameters.AddWithValue("@DoiTuong", DbHelper.ToDbValue(nk.DoiTuong));
                cmd.Parameters.AddWithValue("@MaDoiTuong", DbHelper.ToDbValue(nk.MaDoiTuong));
                cmd.Parameters.AddWithValue("@DiaChiIP", DbHelper.ToDbValue(nk.DiaChiIP));
                cmd.Parameters.AddWithValue("@ThietBi", DbHelper.ToDbValue(nk.ThietBi));
                cmd.Parameters.AddWithValue("@GiaTriCu", DbHelper.ToDbValue(nk.GiaTriCu));
                cmd.Parameters.AddWithValue("@GiaTriMoi", DbHelper.ToDbValue(nk.GiaTriMoi));
                cmd.ExecuteNonQuery();
            }
            finally
            {
                if (shouldClose) conn.Dispose();
            }
        }

        public List<NhatKyKiemToan> GetRecentLogs(int limit = 100, string? search = null)
        {
            string sql = $@"
                SELECT TOP {limit} nk.MaNK, nk.MaTK, nk.ThoiGian, nk.HanhDong, nk.DoiTuong, nk.MaDoiTuong,
                       nk.DiaChiIP, nk.ThietBi, nk.GiaTriCu, nk.GiaTriMoi,
                       tk.TenDangNhap, nv.HoTen AS HoTenNhanVien
                FROM dbo.NhatKyKiemToan nk
                INNER JOIN dbo.TaiKhoan tk ON nk.MaTK = tk.MaTK
                INNER JOIN dbo.NhanVien nv ON tk.MaNV = nv.MaNV
                WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(search))
            {
                sql += " AND (nk.HanhDong LIKE @Search OR nk.DoiTuong LIKE @Search OR nk.MaDoiTuong LIKE @Search OR tk.TenDangNhap LIKE @Search)";
            }

            sql += " ORDER BY nk.MaNK DESC";

            var list = new List<NhatKyKiemToan>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);

            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@Search", $"%{search.Trim()}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new NhatKyKiemToan
                {
                    MaNK = DbHelper.GetSafeLong(reader, "MaNK"),
                    MaTK = DbHelper.GetSafeInt(reader, "MaTK"),
                    ThoiGian = DbHelper.GetSafeDateTime(reader, "ThoiGian"),
                    HanhDong = DbHelper.GetSafeString(reader, "HanhDong"),
                    DoiTuong = DbHelper.GetSafeString(reader, "DoiTuong"),
                    MaDoiTuong = DbHelper.GetSafeString(reader, "MaDoiTuong"),
                    DiaChiIP = DbHelper.GetSafeString(reader, "DiaChiIP"),
                    ThietBi = DbHelper.GetSafeString(reader, "ThietBi"),
                    GiaTriCu = DbHelper.GetSafeString(reader, "GiaTriCu"),
                    GiaTriMoi = DbHelper.GetSafeString(reader, "GiaTriMoi"),
                    TenDangNhap = DbHelper.GetSafeString(reader, "TenDangNhap"),
                    HoTenNhanVien = DbHelper.GetSafeString(reader, "HoTenNhanVien")
                });
            }
            return list;
        }
    }
}
