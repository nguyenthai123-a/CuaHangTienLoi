using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public class ExcelExportService : IExcelExportService
    {
        public ExcelExportService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        public byte[] ExportInventory(List<LoHang> lots)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("TonKho_FEFO");

            // Header tiêu đề
            ws.Cells["A1:J1"].Merge = true;
            ws.Cells["A1"].Value = "BÁO CÁO TỒN KHO VÀ THEO DÕI HẠN SỬ DỤNG THEO NGUYÊN TẮC FEFO";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(41, 128, 185));
            ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

            ws.Cells["A2:J2"].Merge = true;
            ws.Cells["A2"].Value = $"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            ws.Cells["A2"].Style.Font.Italic = true;
            ws.Cells["A2"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            // Bảng cột
            string[] headers = new[]
            {
                "STT", "Mã Vạch", "Tên Hàng Hóa", "ĐVT", "Số Lô", 
                "Hạn Sử Dụng", "Số Ngày Còn Lại", "Tồn Kho", "Tồn Quầy", "Đơn Giá Bán"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[4, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(52, 73, 94));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 5;
            int stt = 1;
            foreach (var lot in lots)
            {
                ws.Cells[row, 1].Value = stt++;
                ws.Cells[row, 2].Value = lot.MaVach;
                ws.Cells[row, 3].Value = lot.TenHang;
                ws.Cells[row, 4].Value = lot.DonViTinh;
                ws.Cells[row, 5].Value = lot.SoLo;
                ws.Cells[row, 6].Value = lot.HanSuDung.ToString("dd/MM/yyyy");
                ws.Cells[row, 7].Value = lot.SoNgayConLai;
                ws.Cells[row, 8].Value = lot.SlTonKho;
                ws.Cells[row, 9].Value = lot.SlTonQuay;
                ws.Cells[row, 10].Value = lot.GiaBan;

                // Cảnh báo nếu sắp hết hạn (< 30 ngày)
                if (lot.SoNgayConLai <= 30)
                {
                    ws.Cells[row, 6, row, 7].Style.Font.Bold = true;
                    ws.Cells[row, 6, row, 7].Style.Font.Color.SetColor(Color.FromArgb(192, 57, 43));
                    ws.Cells[row, 6, row, 7].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    ws.Cells[row, 6, row, 7].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(250, 219, 216));
                }

                ws.Cells[row, 10].Style.Numberformat.Format = "#,##0";
                row++;
            }

            // Kẻ viền bảng
            using (var range = ws.Cells[4, 1, row - 1, headers.Length])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public byte[] ExportSalesReport(List<HoaDon> invoices)
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("BaoCaoDoanhThu");

            ws.Cells["A1:H1"].Merge = true;
            ws.Cells["A1"].Value = "BÁO CÁO DANH SÁCH HÓA ĐƠN VÀ DOANH THU BÁN HÀNG";
            ws.Cells["A1"].Style.Font.Bold = true;
            ws.Cells["A1"].Style.Font.Size = 16;
            ws.Cells["A1"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            ws.Cells["A1"].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells["A1"].Style.Fill.BackgroundColor.SetColor(Color.FromArgb(39, 174, 96));
            ws.Cells["A1"].Style.Font.Color.SetColor(Color.White);

            string[] headers = new[]
            {
                "STT", "Số Hóa Đơn", "Ngày Giờ", "Thu Ngân", "Quầy", 
                "Tổng Tiền Hàng", "Giảm Giá", "Thành Tiền"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cells[3, i + 1];
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                cell.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(44, 62, 80));
                cell.Style.Font.Color.SetColor(Color.White);
                cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 4;
            int stt = 1;
            foreach (var hd in invoices)
            {
                ws.Cells[row, 1].Value = stt++;
                ws.Cells[row, 2].Value = hd.SoHoaDon;
                ws.Cells[row, 3].Value = hd.NgayGio.ToString("dd/MM/yyyy HH:mm");
                ws.Cells[row, 4].Value = hd.TenThuNgan;
                ws.Cells[row, 5].Value = hd.TenQuay;
                ws.Cells[row, 6].Value = hd.TongTien;
                ws.Cells[row, 7].Value = hd.GiamGia;
                ws.Cells[row, 8].Value = hd.ThanhTien;

                ws.Cells[row, 6].Style.Numberformat.Format = "#,##0";
                ws.Cells[row, 7].Style.Numberformat.Format = "#,##0";
                ws.Cells[row, 8].Style.Numberformat.Format = "#,##0";
                row++;
            }

            // Dòng tổng cộng
            ws.Cells[row, 1, row, 5].Merge = true;
            ws.Cells[row, 1].Value = "TỔNG CỘNG";
            ws.Cells[row, 1].Style.Font.Bold = true;
            ws.Cells[row, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

            ws.Cells[row, 6].Formula = $"SUM(F4:F{row - 1})";
            ws.Cells[row, 7].Formula = $"SUM(G4:G{row - 1})";
            ws.Cells[row, 8].Formula = $"SUM(H4:H{row - 1})";
            ws.Cells[row, 6, row, 8].Style.Font.Bold = true;
            ws.Cells[row, 6, row, 8].Style.Numberformat.Format = "#,##0";

            using (var range = ws.Cells[3, 1, row, headers.Length])
            {
                range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            }

            ws.Cells.AutoFitColumns();
            return package.GetAsByteArray();
        }

        public List<HangHoa> ImportProductsFromExcel(Stream stream)
        {
            var list = new List<HangHoa>();
            using var package = new ExcelPackage(stream);
            var ws = package.Workbook.Worksheets[0];
            int rowCount = ws.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                string? maVach = ws.Cells[row, 1].Value?.ToString();
                string? tenHang = ws.Cells[row, 2].Value?.ToString();
                if (string.IsNullOrWhiteSpace(maVach) || string.IsNullOrWhiteSpace(tenHang)) continue;

                int.TryParse(ws.Cells[row, 3].Value?.ToString(), out int maNhom);
                string? donViTinh = ws.Cells[row, 4].Value?.ToString() ?? "Cái";
                decimal.TryParse(ws.Cells[row, 5].Value?.ToString(), out decimal giaBan);
                decimal.TryParse(ws.Cells[row, 6].Value?.ToString(), out decimal giaNhap);
                int.TryParse(ws.Cells[row, 7].Value?.ToString(), out int dinhMucTon);
                int.TryParse(ws.Cells[row, 8].Value?.ToString(), out int gioiHanDoTuoi);

                list.Add(new HangHoa
                {
                    MaVach = maVach.Trim(),
                    TenHang = tenHang.Trim(),
                    MaNhom = maNhom > 0 ? maNhom : 1,
                    DonViTinh = donViTinh.Trim(),
                    GiaBan = giaBan,
                    GiaNhap = giaNhap,
                    DinhMucTon = dinhMucTon,
                    GioiHanDoTuoi = gioiHanDoTuoi,
                    TrangThaiKinhDoanh = true
                });
            }
            return list;
        }
    }
}
