using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class KhuyenMaiDao : BaseDao
    {
        public KhuyenMaiDao(IConfiguration configuration) : base(configuration) { }
        public KhuyenMaiDao(string connectionString) : base(connectionString) { }

        public List<KhuyenMai> GetActivePromotions()
        {
            const string sql = @"
                SELECT km.MaKM, km.TenKM, km.LoaiGiam, km.GiaTri, km.NgayBatDau, km.NgayKetThuc, km.TrangThai
                FROM dbo.KhuyenMai km
                WHERE km.TrangThai = 'DANG_CHAY'
                  AND SYSDATETIME() BETWEEN km.NgayBatDau AND km.NgayKetThuc";

            var list = new List<KhuyenMai>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KhuyenMai
                {
                    MaKM = DbHelper.GetSafeInt(reader, "MaKM"),
                    TenKM = DbHelper.GetSafeString(reader, "TenKM"),
                    LoaiGiam = DbHelper.GetSafeString(reader, "LoaiGiam"),
                    GiaTri = DbHelper.GetSafeDecimal(reader, "GiaTri"),
                    NgayBatDau = DbHelper.GetSafeDateTime(reader, "NgayBatDau"),
                    NgayKetThuc = DbHelper.GetSafeDateTime(reader, "NgayKetThuc"),
                    TrangThai = DbHelper.GetSafeString(reader, "TrangThai")
                });
            }
            return list;
        }

        public KhuyenMai? GetPromotionForProduct(int maHang)
        {
            const string sql = @"
                SELECT TOP 1 km.MaKM, km.TenKM, km.LoaiGiam, km.GiaTri, km.NgayBatDau, km.NgayKetThuc, km.TrangThai
                FROM dbo.KhuyenMai km
                INNER JOIN dbo.KhuyenMai_HangHoa kmh ON km.MaKM = kmh.MaKM
                WHERE kmh.MaHang = @MaHang
                  AND km.TrangThai = 'DANG_CHAY'
                  AND SYSDATETIME() BETWEEN km.NgayBatDau AND km.NgayKetThuc
                ORDER BY km.GiaTri DESC";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaHang", maHang);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new KhuyenMai
            {
                MaKM = DbHelper.GetSafeInt(reader, "MaKM"),
                TenKM = DbHelper.GetSafeString(reader, "TenKM"),
                LoaiGiam = DbHelper.GetSafeString(reader, "LoaiGiam"),
                GiaTri = DbHelper.GetSafeDecimal(reader, "GiaTri"),
                NgayBatDau = DbHelper.GetSafeDateTime(reader, "NgayBatDau"),
                NgayKetThuc = DbHelper.GetSafeDateTime(reader, "NgayKetThuc"),
                TrangThai = DbHelper.GetSafeString(reader, "TrangThai")
            };
        }

        public int Insert(KhuyenMai km)
        {
            const string sql = @"
                INSERT INTO dbo.KhuyenMai (TenKM, LoaiGiam, GiaTri, NgayBatDau, NgayKetThuc, TrangThai)
                VALUES (@TenKM, @LoaiGiam, @GiaTri, @NgayBatDau, @NgayKetThuc, 'DANG_CHAY');
                SELECT SCOPE_IDENTITY();";

            using var conn = OpenConnection();
            using var trans = conn.BeginTransaction();
            try
            {
                using var cmd = new SqlCommand(sql, conn, trans);
                cmd.Parameters.AddWithValue("@TenKM", km.TenKM.Trim());
                cmd.Parameters.AddWithValue("@LoaiGiam", km.LoaiGiam);
                cmd.Parameters.AddWithValue("@GiaTri", km.GiaTri);
                cmd.Parameters.AddWithValue("@NgayBatDau", km.NgayBatDau);
                cmd.Parameters.AddWithValue("@NgayKetThuc", km.NgayKetThuc);

                int maKM = Convert.ToInt32(cmd.ExecuteScalar());

                if (km.DanhSachMaHang != null && km.DanhSachMaHang.Count > 0)
                {
                    const string sqlItem = "INSERT INTO dbo.KhuyenMai_HangHoa (MaKM, MaHang) VALUES (@MaKM, @MaHang)";
                    foreach (var maHang in km.DanhSachMaHang)
                    {
                        using var cmdItem = new SqlCommand(sqlItem, conn, trans);
                        cmdItem.Parameters.AddWithValue("@MaKM", maKM);
                        cmdItem.Parameters.AddWithValue("@MaHang", maHang);
                        cmdItem.ExecuteNonQuery();
                    }
                }

                trans.Commit();
                return maKM;
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public void EndPromotionEarly(int maKM)
        {
            const string sql = @"
                UPDATE dbo.KhuyenMai
                SET TrangThai = 'DA_KET_THUC',
                    NgayKetThuc = SYSDATETIME()
                WHERE MaKM = @MaKM";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaKM", maKM);
            cmd.ExecuteNonQuery();
        }

        public List<KhuyenMai> GetAll()
        {
            const string sql = @"
                SELECT MaKM, TenKM, LoaiGiam, GiaTri, NgayBatDau, NgayKetThuc, TrangThai
                FROM dbo.KhuyenMai
                ORDER BY MaKM DESC";

            var list = new List<KhuyenMai>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KhuyenMai
                {
                    MaKM = DbHelper.GetSafeInt(reader, "MaKM"),
                    TenKM = DbHelper.GetSafeString(reader, "TenKM"),
                    LoaiGiam = DbHelper.GetSafeString(reader, "LoaiGiam"),
                    GiaTri = DbHelper.GetSafeDecimal(reader, "GiaTri"),
                    NgayBatDau = DbHelper.GetSafeDateTime(reader, "NgayBatDau"),
                    NgayKetThuc = DbHelper.GetSafeDateTime(reader, "NgayKetThuc"),
                    TrangThai = DbHelper.GetSafeString(reader, "TrangThai")
                });
            }
            return list;
        }
    }
}
