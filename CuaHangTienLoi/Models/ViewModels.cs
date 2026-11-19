using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CuaHangTienLoi.Models
{
    // ===== AUTH VIEW MODELS =====

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; } = string.Empty;

        public string? ReturnUrl { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại")]
        [DataType(DataType.Password)]
        public string MatKhauCu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string MatKhauMoi { get; set; } = string.Empty;

        [Compare("MatKhauMoi", ErrorMessage = "Xác nhận mật khẩu mới không khớp")]
        [DataType(DataType.Password)]
        public string XacNhanMatKhauMoi { get; set; } = string.Empty;
    }

    public class UserSessionModel
    {
        public int MaTK { get; set; }
        public int MaNV { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string MaVaiTro { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public List<string> QuyenList { get; set; } = new();
        public int? CurrentMaCa { get; set; }
        public int? CurrentMaQuay { get; set; }
        public string? CurrentTenQuay { get; set; }
    }

    // ===== POS VIEW MODELS =====

    public class PosCartItem
    {
        public int MaHang { get; set; }
        public int MaNhom { get; set; }
        public string? TenNhom { get; set; }
        public string MaVach { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; } = 1;
        public int GioiHanDoTuoi { get; set; } = 0;
        public int? MaKM { get; set; }
        public string? TenKM { get; set; }
        public decimal GiamGiaDonVi { get; set; } = 0;
        public decimal TongTienGiam => GiamGiaDonVi * SoLuong;
        public decimal ThanhTien => (DonGia * SoLuong) - TongTienGiam;
        public int TonQuayHienTai { get; set; }
    }

    public class FefoAllocationItem
    {
        public int MaLo { get; set; }
        public string SoLo { get; set; } = string.Empty;
        public DateTime HanSuDung { get; set; }
        public int SoLuongXuat { get; set; }
        public decimal GiaNhap { get; set; }
    }

    public class PosCheckoutRequest
    {
        public int MaCa { get; set; }
        public int? MaKH { get; set; }
        public bool DaXacNhanDoTuoi { get; set; }
        public int DiemDoi { get; set; } = 0; // Số điểm muốn đổi trừ vào tiền (1 điểm = 1000đ)
        public List<PosCartItem> Items { get; set; } = new();

        // Thanh toán
        public string PhuongThuc { get; set; } = "TIEN_MAT"; // TIEN_MAT, THE, QR, KET_HOP
        public decimal TienKhachDua { get; set; }
        public decimal SoTienTienMat { get; set; }
        public decimal SoTienThe { get; set; }
        public decimal SoTienQR { get; set; }

        // Thông tin thẻ
        public string? SoTheCuoi { get; set; }
        public string? MaChuanChi { get; set; }

        // Thông tin QR
        public string? MaGiaoDichQR { get; set; }
        public string? LoaiVi { get; set; }
    }

    public class PosCheckoutResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int MaHoaDon { get; set; }
        public string SoHoaDon { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
        public decimal TienKhachDua { get; set; }
        public decimal TienThoi { get; set; }
        public int DiemTichLuyMoi { get; set; }
    }

    // ===== CA LÀM VIỆC VIEW MODELS =====

    public class MoCaViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn quầy")]
        public int MaQuay { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số tiền đầu ca")]
        [Range(0, 100000000, ErrorMessage = "Tiền đầu ca từ 0 đến 100.000.000 VNĐ")]
        public decimal TienDauCa { get; set; }

        public List<Quay> DanhSachQuay { get; set; } = new();
    }

    public class DongCaViewModel
    {
        public int MaCa { get; set; }
        public string TenQuay { get; set; } = string.Empty;
        public string TenThuNgan { get; set; } = string.Empty;
        public DateTime GioMo { get; set; }
        public decimal TienDauCa { get; set; }
        public decimal DoanhThuTienMat { get; set; }
        public decimal DoanhThuThe { get; set; }
        public decimal DoanhThuQR { get; set; }
        public decimal TienMatHeThong => TienDauCa + DoanhThuTienMat;

        [Required(ErrorMessage = "Vui lòng đếm và nhập số tiền mặt thực tế")]
        [Range(0, 1000000000, ErrorMessage = "Số tiền mặt thực tế không hợp lệ")]
        public decimal TienMatThucTe { get; set; }

        public string? LyDoChenhLech { get; set; }
        public bool LaChotThay { get; set; } = false;
    }

    // ===== KHO & FEFO VIEW MODELS =====

    public class NhapKhoViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn Nhà cung cấp")]
        public int MaNCC { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số hóa đơn / phiếu giao của NCC")]
        public string SoPhieuGiao { get; set; } = string.Empty;

        public List<ChiTietNhapItem> ChiTiet { get; set; } = new();
    }

    public class ChiTietNhapItem
    {
        public int MaHang { get; set; }
        public string SoLo { get; set; } = string.Empty;
        public DateTime HanSuDung { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaNhap { get; set; }
    }

    public class XuatKhoViewModel
    {
        [Required]
        public string LoaiXuat { get; set; } = "LEN_QUAY"; // LEN_QUAY, HUY, TRA_NCC
        public string? KhuVuc { get; set; } // KHO, QUAY (chỉ cho HUY)
        public int? MaNCC { get; set; } // Chỉ cho TRA_NCC
        public string? LyDo { get; set; }
        public List<ChiTietXuatItem> ChiTiet { get; set; } = new();
    }

    public class ChiTietXuatItem
    {
        public int MaHang { get; set; }
        public int MaLo { get; set; }
        public int SoLuong { get; set; }
        public string? GhiChu { get; set; }
    }

    public class KiemKeViewModel
    {
        public string KhuVuc { get; set; } = "QUAY"; // KHO, QUAY
        public List<ChiTietKiemKeItem> ChiTiet { get; set; } = new();
    }

    public class ChiTietKiemKeItem
    {
        public int MaLo { get; set; }
        public int MaHang { get; set; }
        public string TenHang { get; set; } = string.Empty;
        public string SoLo { get; set; } = string.Empty;
        public DateTime HanSuDung { get; set; }
        public int SlHeThong { get; set; }
        public int SlThucTe { get; set; }
    }

    // ===== DASHBOARD STATS =====

    public class DashboardStatsViewModel
    {
        public decimal DoanhThuHomNay { get; set; }
        public int SoDonHangHomNay { get; set; }
        public int SoCaDangMo { get; set; }
        public int SoHangSapHetHan { get; set; }
        public int SoHangDuoiDinhMuc { get; set; }
        public int SoPhieuDoiTraChoDuyet { get; set; }
        public int SoPhieuHuyChoDuyet { get; set; }
        public List<LoHang> DanhSachSapHetHan { get; set; } = new();
        public List<HoaDon> HoaDonGanDay { get; set; } = new();
        public List<BaoCaoDoanhThuNgay> DoanhThu7Ngay { get; set; } = new();
    }

    public class BaoCaoDoanhThuNgay
    {
        public string Ngay { get; set; } = string.Empty;
        public decimal DoanhThu { get; set; }
    }
}
