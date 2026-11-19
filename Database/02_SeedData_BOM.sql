USE CuaHangTienLoi;
GO

DISABLE TRIGGER dbo.TR_NhatKy_ChiThem ON dbo.NhatKyKiemToan;
DELETE FROM dbo.NhatKyKiemToan;
ENABLE TRIGGER dbo.TR_NhatKy_ChiThem ON dbo.NhatKyKiemToan;

DELETE FROM dbo.TheKho;
DELETE FROM dbo.ChiTietHoaDon;
DELETE FROM dbo.ThanhToan;
DELETE FROM dbo.ChiTietPhieuTra;
DELETE FROM dbo.PhieuTraHang;
DELETE FROM dbo.GiaoDichDiem;
DELETE FROM dbo.HoaDon;
DELETE FROM dbo.BaoCaoCa;
DELETE FROM dbo.Ca;
DELETE FROM dbo.ChiTietKiemKe;
DELETE FROM dbo.PhieuKiemKe;
DELETE FROM dbo.ChiTietPhieuXuat;
DELETE FROM dbo.PhieuXuat;
DELETE FROM dbo.ChiTietPhieuNhap;
DELETE FROM dbo.PhieuNhap;
DELETE FROM dbo.KhuyenMai_HangHoa;
DELETE FROM dbo.KhuyenMai;
DELETE FROM dbo.KhachHang;
DELETE FROM dbo.LoHang;
DELETE FROM dbo.HangHoa;
DELETE FROM dbo.NhomHang;
DELETE FROM dbo.NhaCungCap;
DELETE FROM dbo.PhanCongQuay;
DELETE FROM dbo.Quay;
DELETE FROM dbo.TaiKhoan;
DELETE FROM dbo.NhanVien;

DBCC CHECKIDENT ('dbo.NhanVien', RESEED, 0);
DBCC CHECKIDENT ('dbo.TaiKhoan', RESEED, 0);
DBCC CHECKIDENT ('dbo.Quay', RESEED, 0);
DBCC CHECKIDENT ('dbo.NhaCungCap', RESEED, 0);
DBCC CHECKIDENT ('dbo.NhomHang', RESEED, 0);
DBCC CHECKIDENT ('dbo.HangHoa', RESEED, 0);
DBCC CHECKIDENT ('dbo.LoHang', RESEED, 0);
DBCC CHECKIDENT ('dbo.KhachHang', RESEED, 0);
DBCC CHECKIDENT ('dbo.KhuyenMai', RESEED, 0);
DBCC CHECKIDENT ('dbo.TheKho', RESEED, 0);
DBCC CHECKIDENT ('dbo.NhatKyKiemToan', RESEED, 0);
GO
-- =========================================================================================
-- HỆ THỐNG QUẢN LÝ KHO VÀ CỬA HÀNG TIỆN LỢI (CuaHangTienLoi)
-- File: 02_SeedData.sql
-- =========================================================================================

USE CuaHangTienLoi;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

-- 1. THIẾT LẬP NHÂN VIÊN VÀ TÀI KHOẢN (MẬT KHẨU BĂM BCRYPT)
IF NOT EXISTS (SELECT 1 FROM dbo.NhanVien)
BEGIN
    INSERT INTO dbo.NhanVien (HoTen, SoDienThoai, TrangThai) VALUES
    (N'Nguyễn Văn Quản Lý', '0909000001', 1),   -- MaNV = 1
    (N'Trần Minh Kho',     '0909000002', 1),   -- MaNV = 2
    (N'Lê Thị Thu Ngân 1', '0909000003', 1),   -- MaNV = 3
    (N'Phạm Văn Thu Ngân 2','0909000004', 1);  -- MaNV = 4
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.TaiKhoan)
BEGIN
    INSERT INTO dbo.TaiKhoan (MaNV, TenDangNhap, MatKhauHash, TrangThai, SoLanNhapSai, MaVaiTro) VALUES
    (1, 'admin',     '$2y$10$HYzVmPhcoPoU2M1HrO3tA.PN37AUyeM9HD4uuQSkX6pvzV/5mKcLO', 1, 0, 'QUAN_LY'),
    (2, 'kho01',     '$2y$10$Ft5rOCDNeQ8IUXcYvTflLuEAXp8HcHMyzMkMZ1QKxSRTjcdp08oru', 1, 0, 'NHAN_VIEN_KHO'),
    (3, 'thungan01', '$2y$10$C6404bd2we9MeRcnOSKLR.8L9EenPUBvkVzEdN3fEmeS4E9FSvHwy', 1, 0, 'THU_NGAN'),
    (4, 'thungan02', '$2y$10$C6404bd2we9MeRcnOSKLR.8L9EenPUBvkVzEdN3fEmeS4E9FSvHwy', 1, 0, 'THU_NGAN');
END
GO

-- 2. THIẾT LẬP QUẦY VÀ PHÂN CÔNG QUẦY
IF NOT EXISTS (SELECT 1 FROM dbo.Quay)
BEGIN
    INSERT INTO dbo.Quay (TenQuay, TrangThai) VALUES
    (N'Quầy 01 - Tầng Trệt', 'HOAT_DONG'),
    (N'Quầy 02 - Tầng Lửng', 'HOAT_DONG');

    INSERT INTO dbo.PhanCongQuay (MaNV, MaQuay) VALUES
    (3, 1), -- Lê Thị Thu Ngân 1 -> Quầy 01
    (4, 2); -- Phạm Văn Thu Ngân 2 -> Quầy 02
END
GO

-- 3. NHÀ CUNG CẤP
IF NOT EXISTS (SELECT 1 FROM dbo.NhaCungCap)
BEGIN
    INSERT INTO dbo.NhaCungCap (TenNCC, SoDienThoai, DiaChi, NguoiLienHe) VALUES
    (N'Công ty CP Sữa Việt Nam (Vinamilk)',        '02854155555', N'10 Tân Trào, P. Tân Phú, Quận 7, TP.HCM', N'Nguyễn Hoàng Mai'),
    (N'Công ty TNHH NGK Coca-Cola Việt Nam',       '02838961000', N'Xa Lộ Hà Nội, Linh Trung, TP. Thủ Đức, TP.HCM', N'Trần Tuấn Anh'),
    (N'Công ty CP Acecook Việt Nam',               '02838154064', N'Lô II-3, KCN Tân Bình, P. Tây Thạnh, Tân Phú, TP.HCM', N'Lê Minh Trí'),
    (N'Tổng Công ty CP Bia - Rượu - NGK Sài Gòn',  '02838294083', N'187 Nguyễn Chí Thanh, Phường 12, Quận 5, TP.HCM', N'Võ Quốc Hưng'),
    (N'Công ty TNHH Unilever Việt Nam',            '02854135686', N'A2-3 KCN Tây Bắc Củ Chi, Củ Chi, TP.HCM', N'Đặng Thị Thảo');
END
GO

-- 4. NHÓM HÀNG HÓA
IF NOT EXISTS (SELECT 1 FROM dbo.NhomHang)
BEGIN
    INSERT INTO dbo.NhomHang (TenNhom) VALUES
    (N'Đồ uống & Giải khát'),            -- MaNhom = 1
    (N'Thực phẩm & Đồ ăn nhanh'),        -- MaNhom = 2
    (N'Sữa & Sản phẩm từ sữa'),          -- MaNhom = 3
    (N'Bánh kẹo & Snack ăn vặt'),        -- MaNhom = 4
    (N'Bia, Rượu & Thuốc lá (18+)'),      -- MaNhom = 5
    (N'Hóa mỹ phẩm & Đồ tiện ích');      -- MaNhom = 6
END
GO

-- 5. DANH MỤC HÀNG HÓA CHI TIẾT
IF NOT EXISTS (SELECT 1 FROM dbo.HangHoa)
BEGIN
    INSERT INTO dbo.HangHoa (MaVach, TenHang, MaNhom, DonViTinh, GiaBan, GiaNhap, DinhMucTon, GioiHanDoTuoi, SoNgayBaoQuanMacDinh, TrangThaiKinhDoanh) VALUES
    ('893456789001', N'Nước ngọt Coca-Cola Sleek lon 320ml',       1, N'Lon',   11000,  8000, 24,  0, 365, 1),
    ('893456789002', N'Trà xanh Không Độ chai 455ml',              1, N'Chai',  10000,  7000, 24,  0, 365, 1),
    ('893456789003', N'Nước khoáng thiên nhiên LaVie 500ml',       1, N'Chai',   6000,  3800, 24,  0, 730, 1),
    ('893456789004', N'Mì Hảo Hảo tôm chua cay 75g',               2, N'Gói',    4500,  3200, 30,  0, 180, 1),
    ('893456789005', N'Xúc xích tiệt trùng Vissan Heo 4 cây 100g',  2, N'Gói',   16000, 11500, 15,  0, 120, 1),
    ('893456789006', N'Bánh mì sandwich bơ sữa Kinh Đô 250g',      2, N'Ổ',     18000, 13000, 10,  0,  15, 1),
    ('893456789007', N'Sữa tươi tiệt trùng Vinamilk ít đường 180ml',3, N'Hộp',    9000,  6500, 48,  0, 180, 1),
    ('893456789008', N'Sữa chua Vinamilk có đường lốc 4 hộp',      3, N'Lốc',   28000, 21000, 12,  0,  45, 1),
    ('893456789009', N'Snack khoai tây O''Star vị kim chi 63g',     4, N'Gói',   13500,  9500, 20,  0, 240, 1),
    ('893456789010', N'Bia Tiger lon 330ml (18+)',                 5, N'Lon',   19000, 15000, 48, 18, 365, 1),
    ('893456789011', N'Bia Heineken lon Sleek 330ml (18+)',        5, N'Lon',   23000, 18500, 48, 18, 365, 1),
    ('893456789012', N'Thuốc lá Craven "A" đỏ (18+)',              5, N'Bao',   34000, 28000, 20, 18, 730, 1),
    ('893456789013', N'Khăn ướt không mùi kháng khuẩn Puri 80 tờ', 6, N'Gói',   26000, 18000, 15,  0, 730, 1);
END
GO

-- 6. THIẾT LẬP LÔ HÀNG VỚI CÁC MỐC HSD ĐỂ PHỤC VỤ NGUYÊN TẮC FEFO
IF NOT EXISTS (SELECT 1 FROM dbo.LoHang)
BEGIN
    INSERT INTO dbo.LoHang (MaHang, SoLo, HanSuDung, NgayNhap, SlTonKho, SlTonQuay) VALUES
    (7, 'LO-VNM-2401', DATEADD(DAY, 25, CAST(GETDATE() AS DATE)), DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), 30, 15),
    (7, 'LO-VNM-2402', DATEADD(DAY, 90, CAST(GETDATE() AS DATE)), DATEADD(DAY, -5, CAST(GETDATE() AS DATE)),  80, 40),
    (4, 'LO-HH-05A',  DATEADD(DAY, 35, CAST(GETDATE() AS DATE)), DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 50, 20),
    (4, 'LO-HH-05B',  DATEADD(DAY, 120, CAST(GETDATE() AS DATE)), DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), 100, 60),
    (1, 'LO-CC-101',  DATEADD(DAY, 180, CAST(GETDATE() AS DATE)), DATEADD(DAY, -30, CAST(GETDATE() AS DATE)), 60, 36),
    (2, 'LO-TX-201',  DATEADD(DAY, 150, CAST(GETDATE() AS DATE)), DATEADD(DAY, -25, CAST(GETDATE() AS DATE)), 40, 24),
    (3, 'LO-LV-301',  DATEADD(DAY, 300, CAST(GETDATE() AS DATE)), DATEADD(DAY, -40, CAST(GETDATE() AS DATE)), 70, 30),
    (5, 'LO-VS-401',  DATEADD(DAY, 18, CAST(GETDATE() AS DATE)), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 20, 10),
    (6, 'LO-BM-501',  DATEADD(DAY, 5, CAST(GETDATE() AS DATE)), DATEADD(DAY, -2, CAST(GETDATE() AS DATE)),  15, 8),
    (8, 'LO-SC-601',  DATEADD(DAY, 20, CAST(GETDATE() AS DATE)), DATEADD(DAY, -5, CAST(GETDATE() AS DATE)),  25, 12),
    (9, 'LO-OS-701',  DATEADD(DAY, 110, CAST(GETDATE() AS DATE)), DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 35, 18),
    (10, 'LO-TG-801', DATEADD(DAY, 60, CAST(GETDATE() AS DATE)), DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), 50, 24),
    (10, 'LO-TG-802', DATEADD(DAY, 210, CAST(GETDATE() AS DATE)), DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), 80, 48),
    (11, 'LO-HN-901', DATEADD(DAY, 120, CAST(GETDATE() AS DATE)), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 60, 24),
    (12, 'LO-CR-001', DATEADD(DAY, 400, CAST(GETDATE() AS DATE)), DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 40, 20),
    (13, 'LO-PR-101', DATEADD(DAY, 500, CAST(GETDATE() AS DATE)), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 30, 15);
END
GO

-- 7. THẺ KHO BAN ĐẦU
IF NOT EXISTS (SELECT 1 FROM dbo.TheKho)
BEGIN
    INSERT INTO dbo.TheKho (MaHang, MaLo, KhuVuc, Ngay, LoaiGiaoDich, LoaiChungTu, MaChungTu, SlNhap, SlXuat, TonSau) VALUES
    (7, 1, 'KHO',  DATEADD(DAY, -20, SYSDATETIME()), 'NHAP', 'PN', 1, 45, 0, 45),
    (7, 1, 'QUAY', DATEADD(DAY, -19, SYSDATETIME()), 'XUAT_LEN_QUAY', 'PX', 1, 15, 0, 15),
    (7, 2, 'KHO',  DATEADD(DAY, -5, SYSDATETIME()),  'NHAP', 'PN', 2, 120, 0, 120),
    (7, 2, 'QUAY', DATEADD(DAY, -4, SYSDATETIME()),  'XUAT_LEN_QUAY', 'PX', 2, 40, 0, 40),
    (4, 3, 'KHO',  DATEADD(DAY, -15, SYSDATETIME()), 'NHAP', 'PN', 3, 70, 0, 70),
    (4, 3, 'QUAY', DATEADD(DAY, -14, SYSDATETIME()), 'XUAT_LEN_QUAY', 'PX', 3, 20, 0, 20),
    (10, 12, 'KHO', DATEADD(DAY, -20, SYSDATETIME()), 'NHAP', 'PN', 4, 74, 0, 74),
    (10, 12, 'QUAY', DATEADD(DAY, -19, SYSDATETIME()), 'XUAT_LEN_QUAY', 'PX', 4, 24, 0, 24);
END
GO

-- 8. KHÁCH HÀNG THÂN THIẾT
IF NOT EXISTS (SELECT 1 FROM dbo.KhachHang)
BEGIN
    INSERT INTO dbo.KhachHang (HoTen, SoDienThoai, DiemTichLuy) VALUES
    (N'Nguyễn Văn An',   '0901234567', 150),
    (N'Trần Thị Bích',   '0912345678',  65),
    (N'Lê Hoàng Tuấn',   '0987654321',  20),
    (N'Võ Ngọc Hà',      '0933445566',   0);
END
GO

-- 9. CHƯƠNG TRÌNH KHUYẾN MÃI
IF NOT EXISTS (SELECT 1 FROM dbo.KhuyenMai)
BEGIN
    INSERT INTO dbo.KhuyenMai (TenKM, LoaiGiam, GiaTri, NgayBatDau, NgayKetThuc, TrangThai) VALUES
    (N'Mùa hè sảng khoái - Giảm 10% Coca-Cola & Trà xanh', 'PHAN_TRAM', 10.00, DATEADD(DAY, -7, SYSDATETIME()), DATEADD(DAY, 30, SYSDATETIME()), 'DANG_CHAY'),
    (N'Tiết kiệm mỗi ngày - Giảm 500đ mỗi gói Mì Hảo Hảo', 'SO_TIEN', 500.00, DATEADD(DAY, -7, SYSDATETIME()), DATEADD(DAY, 30, SYSDATETIME()), 'DANG_CHAY');

    INSERT INTO dbo.KhuyenMai_HangHoa (MaKM, MaHang) VALUES
    (1, 1),
    (1, 2),
    (2, 4);
END
GO

-- 10. GHI NHẬT KÝ KIỂM TOÁN BAN ĐẦU
IF NOT EXISTS (SELECT 1 FROM dbo.NhatKyKiemToan)
BEGIN
    INSERT INTO dbo.NhatKyKiemToan (MaTK, ThoiGian, HanhDong, DoiTuong, MaDoiTuong, DiaChiIP, ThietBi, GiaTriMoi) VALUES
    (1, SYSDATETIME(), N'KHOI_TAO_HE_THONG', N'HeThong', N'SETUP', '127.0.0.1', N'Windows Server / Setup Script', N'Khởi tạo dữ liệu mẫu ban đầu cho hệ thống Cửa Hàng Tiện Lợi');
END
GO

PRINT N'Hoan tat nap du lieu mau (Seed Data) cho he thong CuaHangTienLoi!';
GO
