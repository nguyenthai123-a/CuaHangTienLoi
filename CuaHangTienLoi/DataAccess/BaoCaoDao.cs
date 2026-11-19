using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.DataAccess
{
    public class BaoCaoDao : BaseDao
    {
        public BaoCaoDao(IConfiguration configuration) : base(configuration) { }
        public BaoCaoDao(string connectionString) : base(connectionString) { }

        public DashboardStatsViewModel GetDashboardStats()
        {
            var stats = new DashboardStatsViewModel();

            using var conn = OpenConnection();

            // 1. Doanh thu và số đơn hôm nay
            const string sqlToday = @"
                SELECT 
                    ISNULL(SUM(TongTien - GiamGia), 0) AS DoanhThuHomNay,
                    COUNT(1) AS SoDonHangHomNay
                FROM dbo.HoaDon
                WHERE CONVERT(date, NgayGio) = CONVERT(date, SYSDATETIME())
                  AND TrangThai = 'HOAN_TAT'";

            using (var cmd = new SqlCommand(sqlToday, conn))
            using (var reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    stats.DoanhThuHomNay = DbHelper.GetSafeDecimal(reader, "DoanhThuHomNay");
                    stats.SoDonHangHomNay = DbHelper.GetSafeInt(reader, "SoDonHangHomNay");
                }
            }

            // 2. Ca đang mở
            const string sqlCa = "SELECT COUNT(1) FROM dbo.Ca WHERE TrangThai = 'MO'";
            using (var cmd = new SqlCommand(sqlCa, conn))
            {
                stats.SoCaDangMo = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 3. Hàng sắp hết hạn (< 30 ngày)
            const string sqlExp = @"
                SELECT COUNT(DISTINCT l.MaLo)
                FROM dbo.LoHang l
                WHERE (l.SlTonKho > 0 OR l.SlTonQuay > 0)
                  AND DATEDIFF(DAY, GETDATE(), l.HanSuDung) <= 30";
            using (var cmd = new SqlCommand(sqlExp, conn))
            {
                stats.SoHangSapHetHan = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 4. Hàng dưới định mức tồn
            const string sqlLowStock = @"
                SELECT COUNT(1)
                FROM dbo.HangHoa h
                WHERE h.TrangThaiKinhDoanh = 1
                  AND h.DinhMucTon > 0
                  AND ISNULL((SELECT SUM(SlTonQuay) FROM dbo.LoHang WHERE MaHang = h.MaHang), 0) < h.DinhMucTon";
            using (var cmd = new SqlCommand(sqlLowStock, conn))
            {
                stats.SoHangDuoiDinhMuc = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 5. Phiếu đổi trả chờ duyệt
            const string sqlDoiTra = "SELECT COUNT(1) FROM dbo.PhieuTraHang WHERE TrangThaiDuyet = 'CHO_DUYET'";
            using (var cmd = new SqlCommand(sqlDoiTra, conn))
            {
                stats.SoPhieuDoiTraChoDuyet = Convert.ToInt32(cmd.ExecuteScalar());
            }

            // 6. Doanh thu 7 ngày gần nhất
            const string sql7Days = @"
                SELECT CONVERT(VARCHAR(10), NgayGio, 103) AS Ngay,
                       ISNULL(SUM(TongTien - GiamGia), 0) AS DoanhThu
                FROM dbo.HoaDon
                WHERE NgayGio >= DATEADD(DAY, -6, CONVERT(date, SYSDATETIME()))
                  AND TrangThai = 'HOAN_TAT'
                GROUP BY CONVERT(VARCHAR(10), NgayGio, 103), CONVERT(date, NgayGio)
                ORDER BY CONVERT(date, NgayGio) ASC";

            using (var cmd = new SqlCommand(sql7Days, conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    stats.DoanhThu7Ngay.Add(new BaoCaoDoanhThuNgay
                    {
                        Ngay = DbHelper.GetSafeString(reader, "Ngay"),
                        DoanhThu = DbHelper.GetSafeDecimal(reader, "DoanhThu")
                    });
                }
            }

            return stats;
        }
    }
}
