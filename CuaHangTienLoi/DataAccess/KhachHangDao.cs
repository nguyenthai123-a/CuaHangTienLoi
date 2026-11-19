using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class KhachHangDao : BaseDao
    {
        public KhachHangDao(IConfiguration configuration) : base(configuration) { }
        public KhachHangDao(string connectionString) : base(connectionString) { }

        public KhachHang? GetByPhone(string phone)
        {
            const string sql = "SELECT MaKH, HoTen, SoDienThoai, DiemTichLuy FROM dbo.KhachHang WHERE SoDienThoai = @SoDienThoai";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@SoDienThoai", phone.Trim());

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new KhachHang
            {
                MaKH = DbHelper.GetSafeInt(reader, "MaKH"),
                HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                DiemTichLuy = DbHelper.GetSafeInt(reader, "DiemTichLuy")
            };
        }

        public KhachHang? GetById(int maKH)
        {
            const string sql = "SELECT MaKH, HoTen, SoDienThoai, DiemTichLuy FROM dbo.KhachHang WHERE MaKH = @MaKH";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaKH", maKH);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new KhachHang
            {
                MaKH = DbHelper.GetSafeInt(reader, "MaKH"),
                HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                DiemTichLuy = DbHelper.GetSafeInt(reader, "DiemTichLuy")
            };
        }

        public int Insert(KhachHang kh)
        {
            const string sql = @"
                INSERT INTO dbo.KhachHang (HoTen, SoDienThoai, DiemTichLuy)
                VALUES (@HoTen, @SoDienThoai, @DiemTichLuy);
                SELECT SCOPE_IDENTITY();";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HoTen", kh.HoTen.Trim());
            cmd.Parameters.AddWithValue("@SoDienThoai", kh.SoDienThoai.Trim());
            cmd.Parameters.AddWithValue("@DiemTichLuy", kh.DiemTichLuy);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void UpdatePoints(int maKH, int pointsChange, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                UPDATE dbo.KhachHang WITH (UPDLOCK, ROWLOCK)
                SET DiemTichLuy = DiemTichLuy + @Points
                WHERE MaKH = @MaKH AND (DiemTichLuy + @Points) >= 0";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaKH", maKH);
            cmd.Parameters.AddWithValue("@Points", pointsChange);

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0)
                throw new InvalidOperationException($"Không thể cập nhật điểm cho khách hàng {maKH} (số dư điểm không đủ hoặc khách hàng không tồn tại).");
        }

        public void RecordPointTransaction(GiaoDichDiem gd, SqlConnection conn, SqlTransaction trans)
        {
            const string sql = @"
                INSERT INTO dbo.GiaoDichDiem (MaKH, Loai, SoDiem, Ngay, MaHoaDon, MaPhieuTra)
                VALUES (@MaKH, @Loai, @SoDiem, SYSDATETIME(), @MaHoaDon, @MaPhieuTra)";

            using var cmd = new SqlCommand(sql, conn, trans);
            cmd.Parameters.AddWithValue("@MaKH", gd.MaKH);
            cmd.Parameters.AddWithValue("@Loai", gd.Loai);
            cmd.Parameters.AddWithValue("@SoDiem", gd.SoDiem);
            cmd.Parameters.AddWithValue("@MaHoaDon", DbHelper.ToDbValue(gd.MaHoaDon));
            cmd.Parameters.AddWithValue("@MaPhieuTra", DbHelper.ToDbValue(gd.MaPhieuTra));
            cmd.ExecuteNonQuery();
        }

        public List<KhachHang> GetAll()
        {
            const string sql = "SELECT MaKH, HoTen, SoDienThoai, DiemTichLuy FROM dbo.KhachHang ORDER BY DiemTichLuy DESC";
            var list = new List<KhachHang>();

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new KhachHang
                {
                    MaKH = DbHelper.GetSafeInt(reader, "MaKH"),
                    HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                    SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                    DiemTichLuy = DbHelper.GetSafeInt(reader, "DiemTichLuy")
                });
            }
            return list;
        }
    }
}
