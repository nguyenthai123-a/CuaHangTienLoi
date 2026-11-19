using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class NhanVienDao : BaseDao
    {
        public NhanVienDao(IConfiguration configuration) : base(configuration) { }
        public NhanVienDao(string connectionString) : base(connectionString) { }

        public List<NhanVien> GetAll()
        {
            const string sql = "SELECT MaNV, HoTen, SoDienThoai, TrangThai FROM dbo.NhanVien ORDER BY MaNV ASC";
            var list = new List<NhanVien>();

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new NhanVien
                {
                    MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                    HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                    SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                    TrangThai = DbHelper.GetSafeBool(reader, "TrangThai")
                });
            }
            return list;
        }

        public NhanVien? GetById(int maNV)
        {
            const string sql = "SELECT MaNV, HoTen, SoDienThoai, TrangThai FROM dbo.NhanVien WHERE MaNV = @MaNV";
            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaNV", maNV);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new NhanVien
            {
                MaNV = DbHelper.GetSafeInt(reader, "MaNV"),
                HoTen = DbHelper.GetSafeString(reader, "HoTen"),
                SoDienThoai = DbHelper.GetSafeString(reader, "SoDienThoai"),
                TrangThai = DbHelper.GetSafeBool(reader, "TrangThai")
            };
        }

        public int Insert(NhanVien nv)
        {
            const string sql = @"
                INSERT INTO dbo.NhanVien (HoTen, SoDienThoai, TrangThai)
                VALUES (@HoTen, @SoDienThoai, @TrangThai);
                SELECT SCOPE_IDENTITY();";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HoTen", nv.HoTen);
            cmd.Parameters.AddWithValue("@SoDienThoai", DbHelper.ToDbValue(nv.SoDienThoai));
            cmd.Parameters.AddWithValue("@TrangThai", nv.TrangThai);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void Update(NhanVien nv)
        {
            const string sql = @"
                UPDATE dbo.NhanVien 
                SET HoTen = @HoTen, SoDienThoai = @SoDienThoai, TrangThai = @TrangThai
                WHERE MaNV = @MaNV";

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@MaNV", nv.MaNV);
            cmd.Parameters.AddWithValue("@HoTen", nv.HoTen);
            cmd.Parameters.AddWithValue("@SoDienThoai", DbHelper.ToDbValue(nv.SoDienThoai));
            cmd.Parameters.AddWithValue("@TrangThai", nv.TrangThai);
            cmd.ExecuteNonQuery();
        }

        public List<Quay> GetAllQuay()
        {
            const string sql = "SELECT MaQuay, TenQuay, TrangThai FROM dbo.Quay WHERE TrangThai = 'HOAT_DONG' ORDER BY MaQuay";
            var list = new List<Quay>();

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Quay
                {
                    MaQuay = DbHelper.GetSafeInt(reader, "MaQuay"),
                    TenQuay = DbHelper.GetSafeString(reader, "TenQuay"),
                    TrangThai = DbHelper.GetSafeString(reader, "TrangThai")
                });
            }
            return list;
        }

        public List<VaiTro> GetAllVaiTro()
        {
            const string sql = "SELECT MaVaiTro, TenVaiTro, MoTa FROM dbo.VaiTro";
            var list = new List<VaiTro>();

            using var conn = OpenConnection();
            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new VaiTro
                {
                    MaVaiTro = DbHelper.GetSafeString(reader, "MaVaiTro"),
                    TenVaiTro = DbHelper.GetSafeString(reader, "TenVaiTro"),
                    MoTa = DbHelper.GetSafeString(reader, "MoTa")
                });
            }
            return list;
        }
    }
}
