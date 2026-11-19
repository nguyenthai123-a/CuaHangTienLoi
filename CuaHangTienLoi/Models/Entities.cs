using System;
using System.Collections.Generic;

namespace CuaHangTienLoi.Models
{
    // ===== M1. TÀI KHOẢN & PHÂN QUYỀN =====

    public class VaiTro
    {
        public string MaVaiTro { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public string? MoTa { get; set; }
    }

    public class Quyen
    {
        public string MaQuyen { get; set; } = string.Empty;
        public string TenQuyen { get; set; } = string.Empty;
        public bool LaNhayCam { get; set; }
    }

    public class NhanVien
    {
        public int MaNV { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public bool TrangThai { get; set; } = true;
    }

    public class TaiKhoan
    {
        public int MaTK { get; set; }
        public int MaNV { get; set; }
        public string TenDangNhap { get; set; } = string.Empty;
        public string MatKhauHash { get; set; } = string.Empty;
        public bool TrangThai { get; set; } = true;
        public int SoLanNhapSai { get; set; } = 0;
        public string MaVaiTro { get; set; } = string.Empty;

        // Navigation
        public NhanVien? NhanVien { get; set; }
        public VaiTro? VaiTro { get; set; }
    }

    public class Quay
    {
        public int MaQuay { get; set; }
        public string TenQuay { get; set; } = string.Empty;
        public string TrangThai { get; set; } = "HOAT_DONG"; // HOAT_DONG, NGUNG
    }

    public class PhanCongQuay
    {
        public int MaNV { get; set; }
        public int MaQuay { get; set; }
    }

    public class NhatKyKiemToan
    {
        public long MaNK { get; set; }
        public int MaTK { get; set; }
        public DateTime ThoiGian { get; set; } = DateTime.Now;
        public string HanhDong { get; set; } = string.Empty;
        public string? DoiTuong { get; set; }
        public string? MaDoiTuong { get; set; }
        public string? DiaChiIP { get; set; }
        public string? ThietBi { get; set; }
        public string? GiaTriCu { get; set; }
        public string? GiaTriMoi { get; set; }

        // Hiển thị mở rộng
        public string? TenDangNhap { get; set; }
        public string? HoTenNhanVien { get; set; }
    }

    // ===== M2. DANH MỤC =====

    public class NhomHang
    {
        public int MaNhom { get; set; }
        public string TenNhom { get; set; } = string.Empty;
    }

    public class HangHoa
    {
        public int MaHang { get; set; }
        public string MaVach { get; set; } = string.Empty;
        public string TenHang { get; set; } = string.Empty;
        public int MaNhom { get; set; }
        public string DonViTinh { get; set; } = string.Empty;
        public decimal GiaBan { get; set; }
        public decimal? GiaNhap { get; set; }
        public int DinhMucTon { get; set; } = 0;
        public int GioiHanDoTuoi { get; set; } = 0; // 0: không giới hạn; 18: cần xác nhận độ tuổi
        public int? SoNgayBaoQuanMacDinh { get; set; }
        public bool TrangThaiKinhDoanh { get; set; } = true;

        // Thuộc tính hiển thị
        public string? TenNhom { get; set; }
        public int TongTonKho { get; set; }
        public int TongTonQuay { get; set; }
    }

    public class NhaCungCap
    {
        public int MaNCC { get; set; }
        public string TenNCC { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? DiaChi { get; set; }
        public string? NguoiLienHe { get; set; }
    }

    // ===== M3. KHO HÀNG & FEFO =====

    public class LoHang
    {
        public int MaLo { get; set; }
        public int MaHang { get; set; }
        public string SoLo { get; set; } = string.Empty;
        public DateTime HanSuDung { get; set; }
        public DateTime NgayNhap { get; set; }
        public int SlTonKho { get; set; }
        public int SlTonQuay { get; set; }

        // Navigation / Extension
        public string? TenHang { get; set; }
        public string? MaVach { get; set; }
        public string? DonViTinh { get; set; }
        public decimal GiaBan { get; set; }
        public int SoNgayConLai => (HanSuDung.Date - DateTime.Today).Days;
        public bool SapHetHan => SoNgayConLai <= 30;
    }

    public class PhieuNhap
    {
        public int MaPN { get; set; }
        public string? SoPhieuNhap { get; set; }
        public DateTime NgayNhap { get; set; }
        public string SoPhieuGiao { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
        public int MaNCC { get; set; }
        public int MaNVLap { get; set; }

        // Navigation
        public string? TenNCC { get; set; }
        public string? TenNVLap { get; set; }
        public List<ChiTietPhieuNhap> ChiTiet { get; set; } = new();
    }

    public class ChiTietPhieuNhap
    {
        public int MaCTPN { get; set; }
        public int MaPN { get; set; }
        public int MaLo { get; set; }
        public int SoLuong { get; set; }
        public decimal GiaNhap { get; set; }
        public DateTime HanSuDung { get; set; }

        // Navigation
        public string? TenHang { get; set; }
        public string? SoLo { get; set; }
        public decimal ThanhTien => SoLuong * GiaNhap;
    }

    public class PhieuXuat
    {
        public int MaPX { get; set; }
        public string? SoPhieuXuat { get; set; }
        public string LoaiXuat { get; set; } = "LEN_QUAY"; // LEN_QUAY, HUY, TRA_NCC
        public DateTime NgayXuat { get; set; }
        public string? LyDo { get; set; }
        public string TrangThai { get; set; } = "HOAN_TAT"; // CHO_DUYET, HOAN_TAT, TU_CHOI
        public string? KhuVuc { get; set; } // KHO, QUAY (chỉ dành cho xuất hủy)
        public int? MaNCC { get; set; }
        public int MaNVLap { get; set; }
        public int? MaNVDuyet { get; set; }

        // Navigation
        public string? TenNVLap { get; set; }
        public string? TenNVDuyet { get; set; }
        public string? TenNCC { get; set; }
        public List<ChiTietPhieuXuat> ChiTiet { get; set; } = new();
    }

    public class ChiTietPhieuXuat
    {
        public int MaCTPX { get; set; }
        public int MaPX { get; set; }
        public int MaLo { get; set; }
        public int SoLuong { get; set; }
        public string? GhiChu { get; set; }

        // Navigation
        public string? TenHang { get; set; }
        public string? SoLo { get; set; }
        public DateTime HanSuDung { get; set; }
    }

    public class PhieuKiemKe
    {
        public int MaKK { get; set; }
        public string? SoPhieuKiemKe { get; set; }
        public DateTime NgayKiemKe { get; set; }
        public string KhuVuc { get; set; } = "QUAY"; // KHO, QUAY
        public string TrangThaiDuyet { get; set; } = "CHO_DUYET"; // CHO_DUYET, DA_DUYET, TU_CHOI
        public int MaNVLap { get; set; }
        public int? MaNVDuyet { get; set; }

        // Navigation
        public string? TenNVLap { get; set; }
        public string? TenNVDuyet { get; set; }
        public List<ChiTietKiemKe> ChiTiet { get; set; } = new();
    }

    public class ChiTietKiemKe
    {
        public int MaCTKK { get; set; }
        public int MaKK { get; set; }
        public int MaLo { get; set; }
        public int SlHeThong { get; set; }
        public int SlThucTe { get; set; }
        public int ChenhLech => SlThucTe - SlHeThong;

        // Navigation
        public string? TenHang { get; set; }
        public string? SoLo { get; set; }
        public DateTime HanSuDung { get; set; }
    }

    public class TheKho
    {
        public long MaTheKho { get; set; }
        public int MaHang { get; set; }
        public int MaLo { get; set; }
        public string KhuVuc { get; set; } = "QUAY"; // KHO, QUAY
        public DateTime Ngay { get; set; }
        public string LoaiGiaoDich { get; set; } = string.Empty; // NHAP, XUAT_LEN_QUAY, XUAT_HUY, TRA_NCC, BAN, TRA_HANG, HUY_HOA_DON, KIEM_KE
        public string LoaiChungTu { get; set; } = string.Empty; // PN, PX, KK, HD, PT
        public int MaChungTu { get; set; }
        public int SlNhap { get; set; }
        public int SlXuat { get; set; }
        public int TonSau { get; set; }

        // Navigation
        public string? TenHang { get; set; }
        public string? SoLo { get; set; }
    }

    // ===== M5. CA LÀM VIỆC & TIỀN MẶT =====

    public class Ca
    {
        public int MaCa { get; set; }
        public int MaQuay { get; set; }
        public int MaThuNgan { get; set; }
        public int? MaNguoiChot { get; set; }
        public DateTime GioMo { get; set; }
        public DateTime? GioDong { get; set; }
        public decimal TienDauCa { get; set; }
        public decimal? TienMatHeThong { get; set; }
        public decimal? TienMatThucTe { get; set; }
        public decimal? ChenhLech => (TienMatThucTe.HasValue && TienMatHeThong.HasValue) ? TienMatThucTe.Value - TienMatHeThong.Value : null;
        public string? LyDoChenhLech { get; set; }
        public string TrangThai { get; set; } = "MO"; // MO, DONG

        // Navigation
        public string? TenQuay { get; set; }
        public string? TenThuNgan { get; set; }
        public string? TenNguoiChot { get; set; }
    }

    public class BaoCaoCa
    {
        public int MaBC { get; set; }
        public int MaCa { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal DoanhThuTienMat { get; set; }
        public decimal DoanhThuThe { get; set; }
        public decimal DoanhThuQR { get; set; }
        public decimal TongDoanhThu => DoanhThuTienMat + DoanhThuThe + DoanhThuQR;

        // Navigation
        public Ca? Ca { get; set; }
    }

    // ===== M6. KHÁCH HÀNG & KHUYẾN MÃI =====

    public class KhachHang
    {
        public int MaKH { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public int DiemTichLuy { get; set; } = 0;
    }

    public class KhuyenMai
    {
        public int MaKM { get; set; }
        public string TenKM { get; set; } = string.Empty;
        public string LoaiGiam { get; set; } = "PHAN_TRAM"; // PHAN_TRAM, SO_TIEN
        public decimal GiaTri { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string TrangThai { get; set; } = "DANG_CHAY"; // DANG_CHAY, DA_KET_THUC

        public List<int> DanhSachMaHang { get; set; } = new();
    }

    // ===== M4. BÁN HÀNG TẠI QUẦY (POS) =====

    public class HoaDon
    {
        public int MaHoaDon { get; set; }
        public int SoHD { get; set; }
        public string? SoHoaDon { get; set; }
        public DateTime NgayGio { get; set; }
        public decimal TongTien { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien => TongTien - GiamGia;
        public string TrangThai { get; set; } = "HOAN_TAT"; // HOAN_TAT, DA_HUY
        public bool DaXacNhanDoTuoi { get; set; }
        public int MaCa { get; set; }
        public int? MaKH { get; set; }
        public int? NguoiHuy { get; set; }
        public string? LyDoHuy { get; set; }
        public DateTime? NgayHuy { get; set; }

        // Navigation
        public string? TenKhachHang { get; set; }
        public string? SoDienThoaiKH { get; set; }
        public string? TenThuNgan { get; set; }
        public string? TenQuay { get; set; }
        public List<ChiTietHoaDon> ChiTiet { get; set; } = new();
        public List<ThanhToan> ThanhToanList { get; set; } = new();
    }

    public class ChiTietHoaDon
    {
        public int MaCT { get; set; }
        public int MaHoaDon { get; set; }
        public int MaHang { get; set; }
        public int MaLo { get; set; }
        public int? MaKM { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal SoTienGiam { get; set; }
        public decimal GiaVon { get; set; }
        public decimal ThanhTien => (SoLuong * DonGia) - SoTienGiam;

        // Navigation
        public string? TenHang { get; set; }
        public string? DonViTinh { get; set; }
        public string? SoLo { get; set; }
        public DateTime HanSuDung { get; set; }
    }

    public class ThanhToan
    {
        public int MaTT { get; set; }
        public int MaHoaDon { get; set; }
        public string LoaiThanhToan { get; set; } = "TIEN_MAT"; // TIEN_MAT, THE, QR
        public decimal SoTien { get; set; }
        public string TrangThai { get; set; } = "THANH_CONG"; // THANH_CONG, DA_HOAN
        public decimal? TienKhachDua { get; set; }
        public decimal? TienThoi { get; set; }
        public string? SoTheCuoi { get; set; }
        public string? MaChuanChi { get; set; }
        public string? MaGiaoDich { get; set; }
        public string? LoaiVi { get; set; }
    }

    public class PhieuTraHang
    {
        public int MaPhieuTra { get; set; }
        public int? SoPT { get; set; }
        public string? SoPhieuTra { get; set; }
        public int MaHoaDon { get; set; }
        public int? MaCa { get; set; }
        public DateTime NgayTra { get; set; }
        public string LyDo { get; set; } = string.Empty;
        public decimal? SoTienHoan { get; set; }
        public string? PhuongThucHoan { get; set; } // TIEN_MAT, CONG_THANH_TOAN
        public string? MaGiaoDichHoan { get; set; }
        public string TrangThaiDuyet { get; set; } = "CHO_DUYET"; // CHO_DUYET, DA_DUYET, TU_CHOI
        public int MaNVLap { get; set; }
        public int? MaNVDuyet { get; set; }

        // Navigation
        public string? SoHoaDon { get; set; }
        public string? TenNVLap { get; set; }
        public string? TenNVDuyet { get; set; }
        public List<ChiTietPhieuTra> ChiTiet { get; set; } = new();
    }

    public class ChiTietPhieuTra
    {
        public int MaCTPT { get; set; }
        public int MaPhieuTra { get; set; }
        public int MaCT { get; set; }
        public int SoLuong { get; set; }
        public string TinhTrangHang { get; set; } = "CON_DUNG_DUOC"; // CON_DUNG_DUOC, KHONG_DUNG_DUOC

        // Navigation
        public string? TenHang { get; set; }
        public decimal DonGia { get; set; }
        public int MaHang { get; set; }
        public int MaLo { get; set; }
    }

    public class GiaoDichDiem
    {
        public int MaGD { get; set; }
        public int MaKH { get; set; }
        public string Loai { get; set; } = "TICH"; // TICH, DOI, TRU_TRA_HANG, TRU_HUY_HD
        public int SoDiem { get; set; }
        public DateTime Ngay { get; set; }
        public int? MaHoaDon { get; set; }
        public int? MaPhieuTra { get; set; }
    }

    public class CauHinhHeThong
    {
        public string Khoa { get; set; } = string.Empty;
        public string GiaTri { get; set; } = string.Empty;
        public string? MoTa { get; set; }
    }
}
