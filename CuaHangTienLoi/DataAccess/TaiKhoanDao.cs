using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class TaiKhoanDao : BaseDao
    {
        public TaiKhoanDao(IConfiguration configuration) : base(configuration) { }
        public TaiKhoanDao(string connectionString) : base(connectionString) { }

        public TaiKhoan? GetByUsername(string username)
        {
            const string sql = @"
                SELECT tk.MaTK, tk.MaNV, tk.TenDangNhap, tk.MatKhauHash, tk.TrangThai, tk.SoLanNhapSai, tk.MaVaiTro,
                       nv.HoTen, nv.SoDienThoai, nv.TrangThai AS NVTrangThai,
                       vt.TenVaiTro, vt.MoTa AS VTMoTa
                FROM dbo.TaiKhoan tk
                INNER JOIN dbo.NhanVien nv ON tk.MaNV = nv.MaNV
                INNER JOIN dbo.VaiTro vt ON tk.MaVaiTro = vt.MaVaiTro
                WHERE tk.TenDangNhap = @TenDangNhap";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TenDangNhap", username);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new TaiKhoan
            {
                MaTK = DbHelper.GetSafeInt(reader, "MaTK"),
                MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                TenDangNhap = DbHelper.GetSafeString(reader, "TenDangNhap"),
                MatKhauHash = DbHelper.GetSafeString(reader, "MatKhauHash"),
                TrangThai = DbHelper.GetSafeBool(reader, "TrangThai"),
                SoLanNhapSai = DbHelper.GetSafeInt(reader, "SoLanNhapSai"),
                MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                NhanVien = new NhanVien
                {
                    MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                    HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                    SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                    TrangThai = DbHelper.GetSafeBool(reader, "NVTrangThai")
                },
                VaiTro = new VaiTro
                {
                    MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                    TenVaiTro = DbHelper.GetSafeString(reader, "TenVaiTro"),
                    MoTa = DbHelper.GetSafeString(reader, "VTMoTa")
                }
            };
        }

        public TaiKhoan? GetById(int maTK)
        {
            const string sql = @"
                SELECT tk.MaTK, tk.MaNV, tk.TenDangNhap, tk.MatKhauHash, tk.TrangThai, tk.SoLanNhapSai, tk.MaVaiTro,
                       nv.HoTen, nv.SoDienThoai, nv.TrangThai AS NVTrangThai,
                       vt.TenVaiTro, vt.MoTa AS VTMoTa
                FROM dbo.TaiKhoan tk
                INNER JOIN dbo.NhanVien nv ON tk.MaNV = nv.MaNV
                INNER JOIN dbo.VaiTro vt ON tk.MaVaiTro = vt.MaVaiTro
                WHERE tk.MaTK = @MaTK";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaTK", maTK);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new TaiKhoan
            {
                MaTK = DbHelper.GetSafeInt(reader, "MaTK"),
                MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                TenDangNhap = DbHelper.GetSafeString(reader, "TenDangNhap"),
                MatKhauHash = DbHelper.GetSafeString(reader, "MatKhauHash"),
                TrangThai = DbHelper.GetSafeBool(reader, "TrangThai"),
                SoLanNhapSai = DbHelper.GetSafeInt(reader, "SoLanNhapSai"),
                MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                NhanVien = new NhanVien
                {
                    MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                    HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                    SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                    TrangThai = DbHelper.GetSafeBool(reader, "NVTrangThai")
                },
                VaiTro = new VaiTro
                {
                    MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                    TenVaiTro = DbHelper.GetSafeString(reader, "TenVaiTro"),
                    MoTa = DbHelper.GetSafeString(reader, "VTMoTa")
                }
            };
        }

        /// <summary>
        /// Tăng số lần nhập sai và tự động khóa tài khoản khi đạt ngưỡng tối đa (5 lần)
        /// </summary>
        public int IncrementFailedLogin(int maTK, int maxAllowed = 5)
        {
            const string sql = @"
                UPDATE dbo.TaiKhoan
                SET SoLanNhapSai = SoLanNhapSai + 1,
                    TrangThai = CASE WHEN SoLanNhapSai + 1 >= @MaxAllowed THEN 0 ELSE TrangThai END
                WHERE MaTK = @MaTK;
                SELECT SoLanNhapSai, TrangThai FROM dbo.TaiKhoan WHERE MaTK = @MaTK;";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaTK", maTK);
            cmd.Parameters.AddWithValue("@MaxAllowed", maxAllowed);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return DbHelper.GetSafeInt(reader, "SoLanNhapSai");
            }
            return 0;
        }

        public void ResetFailedLogin(int maTK)
        {
            const string sql = "UPDATE dbo.TaiKhoan SET SoLanNhapSai = 0 WHERE MaTK = @MaTK";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaTK", maTK);
            cmd.ExecuteNonQuery();
        }

        public void UpdatePassword(int maTK, string passwordHash)
        {
            const string sql = "UPDATE dbo.TaiKhoan SET MatKhauHash = @Hash, SoLanNhapSai = 0 WHERE MaTK = @MaTK";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaTK", maTK);
            cmd.Parameters.AddWithValue("@Hash", passwordHash);
            cmd.ExecuteNonQuery();
        }

        public void SetLockStatus(int maTK, bool isActive)
        {
            const string sql = "UPDATE dbo.TaiKhoan SET TrangThai = @TrangThai, SoLanNhapSai = CASE WHEN @TrangThai = 1 THEN 0 ELSE SoLanNhapSai END WHERE MaTK = @MaTK";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaTK", maTK);
            cmd.Parameters.AddWithValue("@TrangThai", isActive);
            cmd.ExecuteNonQuery();
        }

        public List<string> GetUserPermissions(string maVaiTro)
        {
            const string sql = @"
                SELECT MaQuyen FROM dbo.VaiTro_Quyen WHERE MaVaiTro = @MaVaiTro";

            var list = new List<string>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaVaiTro", maVaiTro);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(DbHelper.GetSafeString(reader, "MaQuyen"));
            }
            return list;
        }

        public List<TaiKhoan> GetAll()
        {
            const string sql = @"
                SELECT tk.MaTK, tk.MaNV, tk.TenDangNhap, tk.TrangThai, tk.SoLanNhapSai, tk.MaVaiTro,
                       nv.HoTen, nv.SoDienThoai, vt.TenVaiTro
                FROM dbo.TaiKhoan tk
                INNER JOIN dbo.NhanVien nv ON tk.MaNV = nv.MaNV
                INNER JOIN dbo.VaiTro vt ON tk.MaVaiTro = vt.MaVaiTro
                ORDER BY tk.MaTK ASC";

            var list = new List<TaiKhoan>();
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TaiKhoan
                {
                    MaTK = DbHelper.GetSafeInt(reader, "MaTK"),
                    MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                    TenDangNhap = DbHelper.GetSafeString(reader, "TenDangNhap"),
                    TrangThai = DbHelper.GetSafeBool(reader, "TrangThai"),
                    SoLanNhapSai = DbHelper.GetSafeInt(reader, "SoLanNhapSai"),
                    MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                    NhanVien = new NhanVien
                    {
                        MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                        HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                        SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai")
                    },
                    VaiTro = new VaiTro
                    {
                        MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                        TenVaiTro = DbHelper.GetSafeString(reader, "TenVaiTro")
                    }
                });
            }
            return list;
        }

        public int CreateAccount(int maNV, string tenDangNhap, string matKhauHash, string maVaiTro)
        {
            const string sql = @"
                INSERT INTO dbo.TaiKhoan (MaNV, TenDangNhap, MatKhauHash, TrangThai, SoLanNhapSai, MaVaiTro)
                VALUES (@MaNV, @TenDangNhap, @MatKhauHash, 1, 0, @MaVaiTro);
                SELECT SCOPE_IDENTITY();";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaNV", maNV);
            cmd.Parameters.AddWithValue("@TenDangNhap", tenDangNhap);
            cmd.Parameters.AddWithValue("@MatKhauHash", matKhauHash);
            cmd.Parameters.AddWithValue("@MaVaiTro", maVaiTro);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
    }
}
