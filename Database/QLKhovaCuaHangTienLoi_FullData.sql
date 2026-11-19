-- =========================================================================================
-- HỆ THỐNG QUẢN LÝ KHO VÀ CỬA HÀNG TIỆN LỢI (CuaHangTienLoi)
-- File: QLKhovaCuaHangTienLoi_FullData.sql
-- Mô tả: File tổng hợp hoàn chỉnh từ CSDL, Bảng, Ràng buộc, Triggers, Chỉ mục, Quyền hạn
--        đến DỮ LIỆU MẪU ĐẦY ĐỦ (Nhân viên, Tài khoản Bcrypt, Quầy, Danh mục, Lô hàng FEFO,
--        Khách hàng tích điểm, Khuyến mãi, Thẻ kho ban đầu).
-- Hướng dẫn: Mở file này trong SQL Server Management Studio (SSMS) và nhấn F5 (Execute).
-- =========================================================================================

USE master;
GO

-- Xóa database cũ nếu có để tạo mới đồng bộ
IF EXISTS (SELECT * FROM sys.databases WHERE name = 'CuaHangTienLoi')
BEGIN
    ALTER DATABASE CuaHangTienLoi SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE CuaHangTienLoi;
END
GO

CREATE DATABASE CuaHangTienLoi;
GO

USE CuaHangTienLoi;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

-- SEQUENCE cấp số hóa đơn / phiếu trả hàng
CREATE SEQUENCE dbo.seq_HoaDon   AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE dbo.seq_PhieuTra AS INT START WITH 1 INCREMENT BY 1;
GO

-- ===== M1. Tài khoản & phân quyền =====
CREATE TABLE dbo.VaiTro (
    MaVaiTro  VARCHAR(20)   NOT NULL PRIMARY KEY,
    TenVaiTro NVARCHAR(50)  NOT NULL,
    MoTa      NVARCHAR(200) NULL
);

CREATE TABLE dbo.Quyen (
    MaQuyen   VARCHAR(30)   NOT NULL PRIMARY KEY,
    TenQuyen  NVARCHAR(100) NOT NULL,
    LaNhayCam BIT NOT NULL DEFAULT 0
);

CREATE TABLE dbo.VaiTro_Quyen (
    MaVaiTro VARCHAR(20) NOT NULL REFERENCES dbo.VaiTro(MaVaiTro),
    MaQuyen  VARCHAR(30) NOT NULL REFERENCES dbo.Quyen(MaQuyen),
    PRIMARY KEY (MaVaiTro, MaQuyen)
);

CREATE TABLE dbo.NhanVien (
    MaNV        INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15)   NULL,
    TrangThai   BIT NOT NULL DEFAULT 1              -- 1 = đang làm việc
);

CREATE TABLE dbo.TaiKhoan (
    MaTK         INT IDENTITY(1,1) PRIMARY KEY,
    MaNV         INT NOT NULL UNIQUE REFERENCES dbo.NhanVien(MaNV),    -- 1-1
    TenDangNhap  VARCHAR(50)  NOT NULL UNIQUE,
    MatKhauHash  VARCHAR(255) NOT NULL,                -- bcrypt/argon2
    TrangThai    BIT NOT NULL DEFAULT 1,               -- 1 = hoạt động, 0 = bị khóa
    SoLanNhapSai INT NOT NULL DEFAULT 0 CHECK (SoLanNhapSai >= 0),
    MaVaiTro     VARCHAR(20) NOT NULL REFERENCES dbo.VaiTro(MaVaiTro)
);

CREATE TABLE dbo.Quay (
    MaQuay    INT IDENTITY(1,1) PRIMARY KEY,
    TenQuay   NVARCHAR(50) NOT NULL,
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'HOAT_DONG' CHECK (TrangThai IN ('HOAT_DONG','NGUNG'))
);

CREATE TABLE dbo.PhanCongQuay (
    MaNV   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaQuay INT NOT NULL REFERENCES dbo.Quay(MaQuay),
    PRIMARY KEY (MaNV, MaQuay)
);

CREATE TABLE dbo.NhatKyKiemToan (
    MaNK       BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaTK       INT NOT NULL REFERENCES dbo.TaiKhoan(MaTK),
    ThoiGian   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    HanhDong   NVARCHAR(50)  NOT NULL,
    DoiTuong   NVARCHAR(50)  NULL,                     -- (+) bảng/đối tượng bị tác động
    MaDoiTuong NVARCHAR(50)  NULL,                     -- (+) mã đối tượng (vd HD-0012)
    DiaChiIP   VARCHAR(45)   NULL,
    ThietBi    NVARCHAR(200) NULL,                     -- (+) NFR02: IP/thiết bị
    GiaTriCu   NVARCHAR(MAX) NULL,
    GiaTriMoi  NVARCHAR(MAX) NULL
);
GO

-- Nhật ký chỉ thêm, không sửa, không xóa
CREATE TRIGGER dbo.TR_NhatKy_ChiThem ON dbo.NhatKyKiemToan
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50001, N'Nhật ký kiểm toán chỉ được thêm, không được sửa hoặc xóa.', 1;
END;
GO

-- ===== M2. Danh mục =====
CREATE TABLE dbo.NhomHang (
    MaNhom  INT IDENTITY(1,1) PRIMARY KEY,
    TenNhom NVARCHAR(100) NOT NULL
);

CREATE TABLE dbo.HangHoa (
    MaHang  INT IDENTITY(1,1) PRIMARY KEY,
    MaVach  VARCHAR(30)    NOT NULL UNIQUE,
    TenHang NVARCHAR(200)  NOT NULL,
    MaNhom  INT NOT NULL REFERENCES dbo.NhomHang(MaNhom),
    DonViTinh NVARCHAR(30) NOT NULL,
    GiaBan  DECIMAL(18,0)  NOT NULL CHECK (GiaBan >= 0),
    GiaNhap DECIMAL(18,0)  NULL CHECK (GiaNhap >= 0),
    DinhMucTon INT NOT NULL DEFAULT 0 CHECK (DinhMucTon >= 0),
    GioiHanDoTuoi INT NOT NULL DEFAULT 0 CHECK (GioiHanDoTuoi >= 0),    -- 0 = không giới hạn
    SoNgayBaoQuanMacDinh INT NULL CHECK (SoNgayBaoQuanMacDinh > 0),
    TrangThaiKinhDoanh BIT NOT NULL DEFAULT 1
);

CREATE TABLE dbo.NhaCungCap (
    MaNCC         INT IDENTITY(1,1) PRIMARY KEY,
    TenNCC        NVARCHAR(150) NOT NULL,
    SoDienThoai   VARCHAR(15)   NOT NULL,
    DiaChi        NVARCHAR(255) NULL,
    NguoiLienHe   NVARCHAR(100) NULL
);

-- ===== M3. Kho =====
CREATE TABLE dbo.LoHang (
    MaLo      INT IDENTITY(1,1) PRIMARY KEY,
    MaHang    INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    SoLo      NVARCHAR(50) NOT NULL,
    HanSuDung DATE NOT NULL,
    NgayNhap  DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    SlTonKho  INT NOT NULL DEFAULT 0 CHECK (SlTonKho >= 0),
    SlTonQuay INT NOT NULL DEFAULT 0 CHECK (SlTonQuay >= 0)
);

CREATE TABLE dbo.PhieuNhap (
    MaPN INT IDENTITY(1,1) PRIMARY KEY,
    SoPhieuNhap AS ('PN-' + CASE WHEN MaPN < 10000 THEN RIGHT('0000' + CAST(MaPN AS VARCHAR(10)), 4) ELSE CAST(MaPN AS VARCHAR(10)) END) PERSISTED,
    NgayNhap    DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    SoPhieuGiao NVARCHAR(50) NOT NULL,
    TongTien    DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (TongTien >= 0),
    MaNCC   INT NOT NULL REFERENCES dbo.NhaCungCap(MaNCC),
    MaNVLap INT NOT NULL REFERENCES dbo.NhanVien(MaNV)
);

CREATE TABLE dbo.ChiTietPhieuNhap (
    MaCTPN INT IDENTITY(1,1) PRIMARY KEY,
    MaPN   INT NOT NULL REFERENCES dbo.PhieuNhap(MaPN),
    MaLo   INT NOT NULL UNIQUE REFERENCES dbo.LoHang(MaLo),
    SoLuong   INT NOT NULL CHECK (SoLuong > 0),
    GiaNhap   DECIMAL(18,0) NOT NULL CHECK (GiaNhap >= 0),
    HanSuDung DATE NOT NULL
);

CREATE TABLE dbo.PhieuXuat (
    MaPX INT IDENTITY(1,1) PRIMARY KEY,
    SoPhieuXuat AS ('PX-' + CASE WHEN MaPX < 10000 THEN RIGHT('0000' + CAST(MaPX AS VARCHAR(10)), 4) ELSE CAST(MaPX AS VARCHAR(10)) END) PERSISTED,
    LoaiXuat  VARCHAR(10) NOT NULL CHECK (LoaiXuat IN ('LEN_QUAY','HUY','TRA_NCC')),
    NgayXuat  DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    LyDo      NVARCHAR(255) NULL,
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'HOAN_TAT' CHECK (TrangThai IN ('CHO_DUYET','HOAN_TAT','TU_CHOI')),
    KhuVuc    VARCHAR(5) NULL CHECK (KhuVuc IN ('KHO','QUAY')),
    MaNCC     INT NULL REFERENCES dbo.NhaCungCap(MaNCC),
    MaNVLap   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNVDuyet INT NULL REFERENCES dbo.NhanVien(MaNV),
    CONSTRAINT CK_PX_KhuVuc CHECK ((LoaiXuat = 'HUY' AND KhuVuc IS NOT NULL AND LyDo IS NOT NULL) OR (LoaiXuat <> 'HUY' AND KhuVuc IS NULL)),
    CONSTRAINT CK_PX_NCC    CHECK ((LoaiXuat = 'TRA_NCC' AND MaNCC IS NOT NULL AND LyDo IS NOT NULL) OR (LoaiXuat <> 'TRA_NCC' AND MaNCC IS NULL)),
    CONSTRAINT CK_PX_Duyet  CHECK (LoaiXuat = 'HUY' OR TrangThai = 'HOAN_TAT')
);

CREATE TABLE dbo.ChiTietPhieuXuat (
    MaCTPX INT IDENTITY(1,1) PRIMARY KEY,
    MaPX   INT NOT NULL REFERENCES dbo.PhieuXuat(MaPX),
    MaLo   INT NOT NULL REFERENCES dbo.LoHang(MaLo),
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    GhiChu  NVARCHAR(255) NULL
);

CREATE TABLE dbo.PhieuKiemKe (
    MaKK INT IDENTITY(1,1) PRIMARY KEY,
    SoPhieuKiemKe AS ('KK-' + CASE WHEN MaKK < 10000 THEN RIGHT('0000' + CAST(MaKK AS VARCHAR(10)), 4) ELSE CAST(MaKK AS VARCHAR(10)) END) PERSISTED,
    NgayKiemKe DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    KhuVuc VARCHAR(5) NOT NULL CHECK (KhuVuc IN ('KHO','QUAY')),
    TrangThaiDuyet VARCHAR(10) NOT NULL DEFAULT 'CHO_DUYET' CHECK (TrangThaiDuyet IN ('CHO_DUYET','DA_DUYET','TU_CHOI')),
    MaNVLap   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNVDuyet INT NULL REFERENCES dbo.NhanVien(MaNV)
);

CREATE TABLE dbo.ChiTietKiemKe (
    MaCTKK INT IDENTITY(1,1) PRIMARY KEY,
    MaKK   INT NOT NULL REFERENCES dbo.PhieuKiemKe(MaKK),
    MaLo   INT NOT NULL REFERENCES dbo.LoHang(MaLo),
    SlHeThong INT NOT NULL CHECK (SlHeThong >= 0),
    SlThucTe  INT NOT NULL CHECK (SlThucTe >= 0),
    ChenhLech AS (SlThucTe - SlHeThong) PERSISTED
);

CREATE TABLE dbo.TheKho (
    MaTheKho BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaHang INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    MaLo   INT NOT NULL REFERENCES dbo.LoHang(MaLo),
    KhuVuc VARCHAR(5) NOT NULL CHECK (KhuVuc IN ('KHO','QUAY')),
    Ngay   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    LoaiGiaoDich VARCHAR(15) NOT NULL CHECK (LoaiGiaoDich IN
        ('NHAP','XUAT_LEN_QUAY','XUAT_HUY','TRA_NCC','BAN','TRA_HANG','HUY_HOA_DON','KIEM_KE')),
    LoaiChungTu VARCHAR(3) NOT NULL CHECK (LoaiChungTu IN ('PN','PX','KK','HD','PT')),
    MaChungTu   INT NOT NULL,
    SlNhap INT NOT NULL DEFAULT 0 CHECK (SlNhap >= 0),
    SlXuat INT NOT NULL DEFAULT 0 CHECK (SlXuat >= 0),
    TonSau INT NOT NULL CHECK (TonSau >= 0)
);

-- ===== M5. Ca làm việc =====
CREATE TABLE dbo.Ca (
    MaCa INT IDENTITY(1,1) PRIMARY KEY,
    MaQuay      INT NOT NULL REFERENCES dbo.Quay(MaQuay),
    MaThuNgan   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNguoiChot INT NULL REFERENCES dbo.NhanVien(MaNV),
    GioMo  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    GioDong DATETIME2(0) NULL,
    TienDauCa DECIMAL(18,0) NOT NULL CHECK (TienDauCa >= 0),
    TienMatHeThong DECIMAL(18,0) NULL,
    TienMatThucTe  DECIMAL(18,0) NULL CHECK (TienMatThucTe >= 0),
    ChenhLech AS (TienMatThucTe - TienMatHeThong) PERSISTED,
    LyDoChenhLech NVARCHAR(255) NULL,
    TrangThai VARCHAR(5) NOT NULL DEFAULT 'MO' CHECK (TrangThai IN ('MO','DONG')),
    CONSTRAINT CK_Ca_TrangThai CHECK (
        (TrangThai = 'MO'   AND GioDong IS NULL AND TienMatThucTe IS NULL AND TienMatHeThong IS NULL AND MaNguoiChot IS NULL) OR
        (TrangThai = 'DONG' AND GioDong IS NOT NULL AND TienMatThucTe IS NOT NULL AND TienMatHeThong IS NOT NULL AND MaNguoiChot IS NOT NULL))
);

CREATE TABLE dbo.BaoCaoCa (
    MaBC INT IDENTITY(1,1) PRIMARY KEY,
    MaCa INT NOT NULL UNIQUE REFERENCES dbo.Ca(MaCa),
    NgayLap DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    DoanhThuTienMat DECIMAL(18,0) NOT NULL DEFAULT 0,
    DoanhThuThe     DECIMAL(18,0) NOT NULL DEFAULT 0,
    DoanhThuQR      DECIMAL(18,0) NOT NULL DEFAULT 0,
    TongDoanhThu AS (DoanhThuTienMat + DoanhThuThe + DoanhThuQR) PERSISTED
);

-- ===== M6. Khách hàng thân thiết & khuyến mãi =====
CREATE TABLE dbo.KhachHang (
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL UNIQUE,
    DiemTichLuy INT NOT NULL DEFAULT 0 CHECK (DiemTichLuy >= 0)
);

CREATE TABLE dbo.KhuyenMai (
    MaKM INT IDENTITY(1,1) PRIMARY KEY,
    TenKM NVARCHAR(150) NOT NULL,
    LoaiGiam VARCHAR(10) NOT NULL CHECK (LoaiGiam IN ('PHAN_TRAM','SO_TIEN')),
    GiaTri DECIMAL(18,2) NOT NULL CHECK (GiaTri > 0),
    NgayBatDau  DATETIME2(0) NOT NULL,
    NgayKetThuc DATETIME2(0) NOT NULL,
    TrangThai VARCHAR(12) NOT NULL DEFAULT 'DANG_CHAY' CHECK (TrangThai IN ('DANG_CHAY','DA_KET_THUC')),
    CHECK (NgayKetThuc >= NgayBatDau),
    CHECK (LoaiGiam <> 'PHAN_TRAM' OR GiaTri <= 100)
);

CREATE TABLE dbo.KhuyenMai_HangHoa (
    MaKM   INT NOT NULL REFERENCES dbo.KhuyenMai(MaKM),
    MaHang INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    PRIMARY KEY (MaKM, MaHang)
);

-- ===== M4. Bán hàng =====
CREATE TABLE dbo.HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    SoHD INT NOT NULL UNIQUE DEFAULT (NEXT VALUE FOR dbo.seq_HoaDon),
    SoHoaDon AS ('HD-' + CASE WHEN SoHD < 10000 THEN RIGHT('0000' + CAST(SoHD AS VARCHAR(10)), 4) ELSE CAST(SoHD AS VARCHAR(10)) END) PERSISTED,
    NgayGio  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    TongTien DECIMAL(18,0) NOT NULL CHECK (TongTien >= 0),
    GiamGia  DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (GiamGia >= 0),
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'HOAN_TAT' CHECK (TrangThai IN ('HOAN_TAT','DA_HUY')),
    DaXacNhanDoTuoi BIT NOT NULL DEFAULT 0,
    MaCa INT NOT NULL REFERENCES dbo.Ca(MaCa),
    MaKH INT NULL REFERENCES dbo.KhachHang(MaKH),
    NguoiHuy INT NULL REFERENCES dbo.NhanVien(MaNV),
    LyDoHuy  NVARCHAR(255) NULL,
    NgayHuy  DATETIME2(0) NULL,
    CONSTRAINT CK_HoaDon_Giam CHECK (GiamGia <= TongTien),
    CONSTRAINT CK_HoaDon_Huy CHECK (
        (TrangThai = 'HOAN_TAT' AND NguoiHuy IS NULL AND LyDoHuy IS NULL AND NgayHuy IS NULL) OR
        (TrangThai = 'DA_HUY'   AND NguoiHuy IS NOT NULL AND LyDoHuy IS NOT NULL AND NgayHuy IS NOT NULL))
);

CREATE TABLE dbo.ChiTietHoaDon (
    MaCT INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),
    MaHang   INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    MaLo     INT NOT NULL REFERENCES dbo.LoHang(MaLo),
    MaKM     INT NULL REFERENCES dbo.KhuyenMai(MaKM),
    SoLuong  INT NOT NULL CHECK (SoLuong > 0),
    DonGia   DECIMAL(18,0) NOT NULL CHECK (DonGia >= 0),
    SoTienGiam DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (SoTienGiam >= 0),
    GiaVon   DECIMAL(18,0) NOT NULL CHECK (GiaVon >= 0),
    ThanhTien AS (SoLuong * DonGia) PERSISTED,
    CONSTRAINT CK_CTHD_Giam CHECK (SoTienGiam <= SoLuong * DonGia)
);

CREATE TABLE dbo.ThanhToan (
    MaTT INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),
    LoaiThanhToan VARCHAR(10) NOT NULL CHECK (LoaiThanhToan IN ('TIEN_MAT','THE','QR')),
    SoTien DECIMAL(18,0) NOT NULL CHECK (SoTien > 0),
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'THANH_CONG' CHECK (TrangThai IN ('THANH_CONG','DA_HOAN')),
    TienKhachDua DECIMAL(18,0) NULL, TienThoi DECIMAL(18,0) NULL,
    SoTheCuoi CHAR(4) NULL, MaChuanChi VARCHAR(50) NULL,
    MaGiaoDich VARCHAR(50) NULL, LoaiVi NVARCHAR(30) NULL,
    CONSTRAINT CK_TT_TienMat CHECK (LoaiThanhToan <> 'TIEN_MAT' OR (TienKhachDua IS NOT NULL AND TienThoi IS NOT NULL AND TienKhachDua >= SoTien)),
    CONSTRAINT CK_TT_The     CHECK (LoaiThanhToan <> 'THE' OR (SoTheCuoi IS NOT NULL AND MaChuanChi IS NOT NULL)),
    CONSTRAINT CK_TT_QR      CHECK (LoaiThanhToan <> 'QR'  OR MaGiaoDich IS NOT NULL)
);

CREATE TABLE dbo.PhieuTraHang (
    MaPhieuTra INT IDENTITY(1,1) PRIMARY KEY,
    SoPT INT NULL,
    SoPhieuTra AS (CASE WHEN SoPT IS NULL THEN NULL
        ELSE 'PT-' + CASE WHEN SoPT < 10000 THEN RIGHT('0000' + CAST(SoPT AS VARCHAR(10)), 4) ELSE CAST(SoPT AS VARCHAR(10)) END END) PERSISTED,
    MaHoaDon INT NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),
    MaCa     INT NULL REFERENCES dbo.Ca(MaCa),
    NgayTra  DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    LyDo     NVARCHAR(255) NOT NULL,
    SoTienHoan DECIMAL(18,0) NULL CHECK (SoTienHoan >= 0),
    PhuongThucHoan VARCHAR(15) NULL CHECK (PhuongThucHoan IN ('TIEN_MAT','CONG_THANH_TOAN')),
    MaGiaoDichHoan VARCHAR(50) NULL,
    TrangThaiDuyet VARCHAR(10) NOT NULL DEFAULT 'CHO_DUYET' CHECK (TrangThaiDuyet IN ('CHO_DUYET','DA_DUYET','TU_CHOI')),
    MaNVLap   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNVDuyet INT NULL REFERENCES dbo.NhanVien(MaNV),
    CONSTRAINT CK_PTH_Duyet CHECK (TrangThaiDuyet <> 'DA_DUYET' OR
        (SoPT IS NOT NULL AND SoTienHoan IS NOT NULL AND PhuongThucHoan IS NOT NULL AND MaNVDuyet IS NOT NULL))
);

CREATE TABLE dbo.ChiTietPhieuTra (
    MaCTPT INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuTra INT NOT NULL REFERENCES dbo.PhieuTraHang(MaPhieuTra),
    MaCT   INT NOT NULL REFERENCES dbo.ChiTietHoaDon(MaCT),
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    TinhTrangHang VARCHAR(15) NOT NULL CHECK (TinhTrangHang IN ('CON_DUNG_DUOC','KHONG_DUNG_DUOC'))
);

CREATE TABLE dbo.GiaoDichDiem (
    MaGD INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL REFERENCES dbo.KhachHang(MaKH),
    Loai VARCHAR(15) NOT NULL CHECK (Loai IN ('TICH','DOI','TRU_TRA_HANG','TRU_HUY_HD')),
    SoDiem INT NOT NULL CHECK (SoDiem > 0),
    Ngay DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    MaHoaDon    INT NULL REFERENCES dbo.HoaDon(MaHoaDon),
    MaPhieuTra INT NULL REFERENCES dbo.PhieuTraHang(MaPhieuTra)
);

-- ===== Cấu hình hệ thống =====
CREATE TABLE dbo.CauHinhHeThong (
    Khoa  VARCHAR(50)    NOT NULL PRIMARY KEY,
    GiaTri NVARCHAR(100) NOT NULL,
    MoTa  NVARCHAR(200)  NULL
);
GO

-- ===== Chỉ mục tối ưu & Ràng buộc duy nhất có điều kiện =====
CREATE UNIQUE INDEX UX_Ca_QuayDangMo   ON dbo.Ca(MaQuay) WHERE TrangThai = 'MO';
CREATE UNIQUE INDEX UX_PhieuTra_SoPT   ON dbo.PhieuTraHang(SoPT) WHERE SoPT IS NOT NULL;
CREATE INDEX IX_LoHang_FEFO            ON dbo.LoHang(MaHang, HanSuDung) INCLUDE (SlTonQuay) WHERE SlTonQuay > 0;
CREATE INDEX IX_HoaDon_Ca              ON dbo.HoaDon(MaCa);
CREATE INDEX IX_HoaDon_NgayGio         ON dbo.HoaDon(NgayGio);
CREATE INDEX IX_TheKho_Hang_Ngay       ON dbo.TheKho(MaHang, Ngay);
CREATE INDEX IX_ChiTietHoaDon_MaHang   ON dbo.ChiTietHoaDon(MaHang) INCLUDE (SoLuong, DonGia, SoTienGiam, GiaVon);
CREATE INDEX IX_NhatKy_ThoiGian        ON dbo.NhatKyKiemToan(ThoiGian, MaTK);
GO

-- =========================================================================================
-- PHẦN DỮ LIỆU KHỞI TẠO HỆ THỐNG (ROLES & PERMISSIONS)
-- =========================================================================================

INSERT dbo.VaiTro (MaVaiTro, TenVaiTro, MoTa) VALUES
 ('QUAN_LY',       N'Quản lý',         N'Toàn quyền quản trị, duyệt hủy, điều chỉnh kiểm kê, xem báo cáo'),
 ('NHAN_VIEN_KHO', N'Nhân viên kho',   N'Lập phiếu nhập, xuất lên quầy, đề xuất hủy/trả NCC, kiểm kê'),
 ('THU_NGAN',      N'Thu ngân',        N'Bán hàng tại quầy, mở/đóng ca, lập yêu cầu đổi trả');

INSERT dbo.Quyen (MaQuyen, TenQuyen, LaNhayCam) VALUES
 ('TK_CA_NHAN',     N'Đăng nhập, đổi mật khẩu của mình', 0),
 ('NV_PHAN_QUYEN',  N'Quản lý nhân viên, phân quyền, cấp lại mật khẩu', 0),
 ('DANH_MUC',       N'Quản lý danh mục hàng hóa, nhà cung cấp', 0),
 ('SUA_GIA',        N'Sửa giá bán', 1),
 ('KHO_LAP',        N'Lập phiếu nhập, xuất, kiểm kê', 0),
 ('XUAT_HUY_DUYET', N'Duyệt xuất hủy', 1),
 ('KIEM_KE_DUYET',  N'Duyệt điều chỉnh kiểm kê (điều chỉnh tồn)', 1),
 ('BAN_HANG',       N'Bán hàng, thanh toán, xác nhận độ tuổi', 0),
 ('DOI_TRA_LAP',    N'Lập yêu cầu đổi/trả hàng', 0),
 ('DOI_TRA_DUYET',  N'Duyệt đổi/trả hàng', 1),
 ('HUY_HOA_DON',    N'Hủy hóa đơn', 1),
 ('KHTT',           N'Quản lý khách hàng thân thiết', 0),
 ('KHUYEN_MAI',     N'Quản lý khuyến mãi', 0),
 ('MO_CHOT_CA',     N'Mở ca, chốt ca', 0),
 ('CHOT_CA_THAY',   N'Chốt ca thay thu ngân', 0),
 ('TON_KHO_QUAY',   N'Tra cứu tồn kho và tồn quầy (toàn bộ)', 0),
 ('TON_QUAY',       N'Tra cứu tồn quầy', 0),
 ('CANH_BAO',       N'Xem cảnh báo tồn/hạn dùng', 0),
 ('BAO_CAO',        N'Xem báo cáo thống kê', 0),
 ('NHAT_KY',        N'Xem nhật ký kiểm toán (chỉ đọc)', 0);

INSERT dbo.VaiTro_Quyen (MaVaiTro, MaQuyen) VALUES
 ('QUAN_LY','TK_CA_NHAN'), ('QUAN_LY','NV_PHAN_QUYEN'), ('QUAN_LY','DANH_MUC'), ('QUAN_LY','SUA_GIA'),
 ('QUAN_LY','XUAT_HUY_DUYET'), ('QUAN_LY','KIEM_KE_DUYET'), ('QUAN_LY','DOI_TRA_DUYET'), ('QUAN_LY','HUY_HOA_DON'),
 ('QUAN_LY','KHUYEN_MAI'), ('QUAN_LY','CHOT_CA_THAY'), ('QUAN_LY','TON_KHO_QUAY'), ('QUAN_LY','CANH_BAO'),
 ('QUAN_LY','BAO_CAO'), ('QUAN_LY','NHAT_KY'),
 ('NHAN_VIEN_KHO','TK_CA_NHAN'), ('NHAN_VIEN_KHO','KHO_LAP'), ('NHAN_VIEN_KHO','TON_KHO_QUAY'), ('NHAN_VIEN_KHO','CANH_BAO'),
 ('THU_NGAN','TK_CA_NHAN'), ('THU_NGAN','BAN_HANG'), ('THU_NGAN','DOI_TRA_LAP'), ('THU_NGAN','KHTT'),
 ('THU_NGAN','MO_CHOT_CA'), ('THU_NGAN','TON_QUAY');

INSERT dbo.CauHinhHeThong (Khoa, GiaTri, MoTa) VALUES
 ('SO_LAN_NHAP_SAI_TOI_DA', '5', N'Khóa tài khoản sau số lần nhập sai liên tiếp (FR01)'),
 ('CANH_BAO_HSD_NGAY',     '30', N'Số ngày trước khi hết hạn cần hiển thị cảnh báo (FEFO)'),
 ('TY_LE_DOI_DIEM',      '1000', N'1 điểm tích lũy tương đương 1.000 VNĐ');
GO

-- =========================================================================================
-- PHẦN DỮ LIỆU MẪU ĐẦY ĐỦ (SEED DATA)
-- =========================================================================================

-- 1. NHÂN VIÊN VÀ TÀI KHOẢN (MẬT KHẨU BĂM BCRYPT)
-- Tài khoản đăng nhập mặc định:
--   admin      / Admin@123    (Vai trò: Quản lý)
--   kho01      / Kho@123      (Vai trò: Nhân viên kho)
--   thungan01  / ThuNgan@123  (Vai trò: Thu ngân Quầy 01)
--   thungan02  / ThuNgan@123  (Vai trò: Thu ngân Quầy 02)

INSERT INTO dbo.NhanVien (HoTen, SoDienThoai, TrangThai) VALUES
(N'Nguyễn Văn Quản Lý', '0909000001', 1),   -- MaNV = 1
(N'Trần Minh Kho',     '0909000002', 1),   -- MaNV = 2
(N'Lê Thị Thu Ngân 1', '0909000003', 1),   -- MaNV = 3
(N'Phạm Văn Thu Ngân 2','0909000004', 1);  -- MaNV = 4

INSERT INTO dbo.TaiKhoan (MaNV, TenDangNhap, MatKhauHash, TrangThai, SoLanNhapSai, MaVaiTro) VALUES
(1, 'admin',     '$2y$10$HYzVmPhcoPoU2M1HrO3tA.PN37AUyeM9HD4uuQSkX6pvzV/5mKcLO', 1, 0, 'QUAN_LY'),
(2, 'kho01',     '$2y$10$Ft5rOCDNeQ8IUXcYvTflLuEAXp8HcHMyzMkMZ1QKxSRTjcdp08oru', 1, 0, 'NHAN_VIEN_KHO'),
(3, 'thungan01', '$2y$10$C6404bd2we9MeRcnOSKLR.8L9EenPUBvkVzEdN3fEmeS4E9FSvHwy', 1, 0, 'THU_NGAN'),
(4, 'thungan02', '$2y$10$C6404bd2we9MeRcnOSKLR.8L9EenPUBvkVzEdN3fEmeS4E9FSvHwy', 1, 0, 'THU_NGAN');

-- 2. QUẦY BÁN HÀNG VÀ PHÂN CÔNG QUẦY
INSERT INTO dbo.Quay (TenQuay, TrangThai) VALUES
(N'Quầy 01 - Tầng Trệt', 'HOAT_DONG'),
(N'Quầy 02 - Tầng Lửng', 'HOAT_DONG');

INSERT INTO dbo.PhanCongQuay (MaNV, MaQuay) VALUES
(3, 1), -- Lê Thị Thu Ngân 1 -> Quầy 01
(4, 2); -- Phạm Văn Thu Ngân 2 -> Quầy 02

-- 3. NHÀ CUNG CẤP
INSERT INTO dbo.NhaCungCap (TenNCC, SoDienThoai, DiaChi, NguoiLienHe) VALUES
(N'Công ty CP Sữa Việt Nam (Vinamilk)',        '02854155555', N'10 Tân Trào, P. Tân Phú, Quận 7, TP.HCM', N'Nguyễn Hoàng Mai'),
(N'Công ty TNHH NGK Coca-Cola Việt Nam',       '02838961000', N'Xa Lộ Hà Nội, Linh Trung, TP. Thủ Đức, TP.HCM', N'Trần Tuấn Anh'),
(N'Công ty CP Acecook Việt Nam',               '02838154064', N'Lô II-3, KCN Tân Bình, P. Tây Thạnh, Tân Phú, TP.HCM', N'Lê Minh Trí'),
(N'Tổng Công ty CP Bia - Rượu - NGK Sài Gòn',  '02838294083', N'187 Nguyễn Chí Thanh, Phường 12, Quận 5, TP.HCM', N'Võ Quốc Hưng'),
(N'Công ty TNHH Unilever Việt Nam',            '02854135686', N'A2-3 KCN Tây Bắc Củ Chi, Củ Chi, TP.HCM', N'Đặng Thị Thảo');

-- 4. NHÓM HÀNG HÓA
INSERT INTO dbo.NhomHang (TenNhom) VALUES
(N'Đồ uống & Giải khát'),            -- MaNhom = 1
(N'Thực phẩm & Đồ ăn nhanh'),        -- MaNhom = 2
(N'Sữa & Sản phẩm từ sữa'),          -- MaNhom = 3
(N'Bánh kẹo & Snack ăn vặt'),        -- MaNhom = 4
(N'Bia, Rượu & Thuốc lá (18+)'),      -- MaNhom = 5 (Giới hạn độ tuổi)
(N'Hóa mỹ phẩm & Đồ tiện ích');      -- MaNhom = 6

-- 5. DANH MỤC HÀNG HÓA (Đa dạng, có hàng 18+ để kích hoạt cảnh báo POS)
INSERT INTO dbo.HangHoa (MaVach, TenHang, MaNhom, DonViTinh, GiaBan, GiaNhap, DinhMucTon, GioiHanDoTuoi, SoNgayBaoQuanMacDinh, TrangThaiKinhDoanh) VALUES
('893456789001', N'Nước ngọt Coca-Cola Sleek lon 320ml',       1, N'Lon',   11000,  8000, 24,  0, 365, 1), -- MaHang = 1
('893456789002', N'Trà xanh Không Độ chai 455ml',              1, N'Chai',  10000,  7000, 24,  0, 365, 1), -- MaHang = 2
('893456789003', N'Nước khoáng thiên nhiên LaVie 500ml',       1, N'Chai',   6000,  3800, 24,  0, 730, 1), -- MaHang = 3
('893456789004', N'Mì Hảo Hảo tôm chua cay 75g',               2, N'Gói',    4500,  3200, 30,  0, 180, 1), -- MaHang = 4
('893456789005', N'Xúc xích tiệt trùng Vissan Heo 4 cây 100g',  2, N'Gói',   16000, 11500, 15,  0, 120, 1), -- MaHang = 5
('893456789006', N'Bánh mì sandwich bơ sữa Kinh Đô 250g',      2, N'Ổ',     18000, 13000, 10,  0,  15, 1), -- MaHang = 6
('893456789007', N'Sữa tươi tiệt trùng Vinamilk ít đường 180ml',3, N'Hộp',    9000,  6500, 48,  0, 180, 1), -- MaHang = 7
('893456789008', N'Sữa chua Vinamilk có đường lốc 4 hộp',      3, N'Lốc',   28000, 21000, 12,  0,  45, 1), -- MaHang = 8
('893456789009', N'Snack khoai tây O''Star vị kim chi 63g',     4, N'Gói',   13500,  9500, 20,  0, 240, 1), -- MaHang = 9
('893456789010', N'Bia Tiger lon 330ml (18+)',                 5, N'Lon',   19000, 15000, 48, 18, 365, 1), -- MaHang = 10 (18+)
('893456789011', N'Bia Heineken lon Sleek 330ml (18+)',        5, N'Lon',   23000, 18500, 48, 18, 365, 1), -- MaHang = 11 (18+)
('893456789012', N'Thuốc lá Craven "A" đỏ (18+)',              5, N'Bao',   34000, 28000, 20, 18, 730, 1), -- MaHang = 12 (18+)
('893456789013', N'Khăn ướt không mùi kháng khuẩn Puri 80 tờ', 6, N'Gói',   26000, 18000, 15,  0, 730, 1); -- MaHang = 13

-- 6. THIẾT LẬP LÔ HÀNG VỚI CÁC MỐC HSD ĐỂ PHỤC VỤ NGUYÊN TẮC FEFO
-- Ví dụ: Sữa Vinamilk (MaHang = 7) có 2 lô:
--   Lô 1 (LO-VNM-2401) HSD còn 25 ngày -> FEFO sẽ tự động xuất trước
--   Lô 2 (LO-VNM-2402) HSD còn 90 ngày -> FEFO sẽ xuất sau khi lô 1 hết hàng

INSERT INTO dbo.LoHang (MaHang, SoLo, HanSuDung, NgayNhap, SlTonKho, SlTonQuay) VALUES
-- Sữa Vinamilk 180ml (MaHang = 7)
(7, 'LO-VNM-2401', DATEADD(DAY, 25, CAST(GETDATE() AS DATE)), DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), 30, 15), -- Lô 1: HSD còn 25 ngày (Xuất trước)
(7, 'LO-VNM-2402', DATEADD(DAY, 90, CAST(GETDATE() AS DATE)), DATEADD(DAY, -5, CAST(GETDATE() AS DATE)),  80, 40), -- Lô 2: HSD còn 90 ngày (Xuất sau)

-- Mì Hảo Hảo (MaHang = 4)
(4, 'LO-HH-05A',  DATEADD(DAY, 35, CAST(GETDATE() AS DATE)), DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 50, 20), -- Lô 3: HSD còn 35 ngày
(4, 'LO-HH-05B',  DATEADD(DAY, 120, CAST(GETDATE() AS DATE)), DATEADD(DAY, -2, CAST(GETDATE() AS DATE)), 100, 60),-- Lô 4: HSD còn 120 ngày

-- Nước Coca-Cola (MaHang = 1)
(1, 'LO-CC-101',  DATEADD(DAY, 180, CAST(GETDATE() AS DATE)), DATEADD(DAY, -30, CAST(GETDATE() AS DATE)), 60, 36), -- Lô 5

-- Trà xanh Không Độ (MaHang = 2)
(2, 'LO-TX-201',  DATEADD(DAY, 150, CAST(GETDATE() AS DATE)), DATEADD(DAY, -25, CAST(GETDATE() AS DATE)), 40, 24), -- Lô 6

-- Nước LaVie (MaHang = 3)
(3, 'LO-LV-301',  DATEADD(DAY, 300, CAST(GETDATE() AS DATE)), DATEADD(DAY, -40, CAST(GETDATE() AS DATE)), 70, 30), -- Lô 7

-- Xúc xích Vissan (MaHang = 5)
(5, 'LO-VS-401',  DATEADD(DAY, 18, CAST(GETDATE() AS DATE)), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 20, 10), -- Lô 8: Cảnh báo sắp hết hạn!

-- Bánh mì Kinh Đô (MaHang = 6)
(6, 'LO-BM-501',  DATEADD(DAY, 5, CAST(GETDATE() AS DATE)), DATEADD(DAY, -2, CAST(GETDATE() AS DATE)),  15, 8),   -- Lô 9: Hạn cực ngắn (5 ngày)

-- Sữa chua Vinamilk (MaHang = 8)
(8, 'LO-SC-601',  DATEADD(DAY, 20, CAST(GETDATE() AS DATE)), DATEADD(DAY, -5, CAST(GETDATE() AS DATE)),  25, 12),  -- Lô 10

-- Snack O'Star (MaHang = 9)
(9, 'LO-OS-701',  DATEADD(DAY, 110, CAST(GETDATE() AS DATE)), DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 35, 18), -- Lô 11

-- Bia Tiger (MaHang = 10 - 18+)
(10, 'LO-TG-801', DATEADD(DAY, 60, CAST(GETDATE() AS DATE)), DATEADD(DAY, -20, CAST(GETDATE() AS DATE)), 50, 24), -- Lô 12
(10, 'LO-TG-802', DATEADD(DAY, 210, CAST(GETDATE() AS DATE)), DATEADD(DAY, -1, CAST(GETDATE() AS DATE)), 80, 48), -- Lô 13

-- Bia Heineken (MaHang = 11 - 18+)
(11, 'LO-HN-901', DATEADD(DAY, 120, CAST(GETDATE() AS DATE)), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 60, 24), -- Lô 14

-- Thuốc lá Craven (MaHang = 12 - 18+)
(12, 'LO-CR-001', DATEADD(DAY, 400, CAST(GETDATE() AS DATE)), DATEADD(DAY, -15, CAST(GETDATE() AS DATE)), 40, 20), -- Lô 15

-- Khăn ướt Puri (MaHang = 13)
(13, 'LO-PR-101', DATEADD(DAY, 500, CAST(GETDATE() AS DATE)), DATEADD(DAY, -10, CAST(GETDATE() AS DATE)), 30, 15); -- Lô 16

-- 7. THẺ KHO BAN ĐẦU (Ghi nhận lịch sử luân chuyển kho & quầy)
INSERT INTO dbo.TheKho (MaHang, MaLo, KhuVuc, Ngay, LoaiGiaoDich, LoaiChungTu, MaChungTu, SlNhap, SlXuat, TonSau) VALUES
(7, 1, 'KHO',  DATEADD(DAY, -20, SYSDATETIME()), 'NHAP', 'PN', 1, 45, 0, 45),
(7, 1, 'QUAY', DATEADD(DAY, -19, SYSDATETIME()), 'XUAT_LEN_QUAY', 'PX', 1, 15, 0, 15),
(7, 2, 'KHO',  DATEADD(DAY, -5, SYSDATETIME()),  'NHAP', 'PN', 2, 120, 0, 120),
(7, 2, 'QUAY', DATEADD(DAY, -4, SYSDATETIME()),  'XUAT_LEN_QUAY', 'PX', 2, 40, 0, 40),
(4, 3, 'KHO',  DATEADD(DAY, -15, SYSDATETIME()), 'NHAP', 'PN', 3, 70, 0, 70),
(4, 3, 'QUAY', DATEADD(DAY, -14, SYSDATETIME()), 'XUAT_LEN_QUAY', 'PX', 3, 20, 0, 20),
(10, 12, 'KHO', DATEADD(DAY, -20, SYSDATETIME()), 'NHAP', 'PN', 4, 74, 0, 74),
(10, 12, 'QUAY', DATEADD(DAY, -19, SYSDATETIME()), 'XUAT_LEN_QUAY', 'PX', 4, 24, 0, 24);

-- 8. KHÁCH HÀNG THÂN THIẾT
INSERT INTO dbo.KhachHang (HoTen, SoDienThoai, DiemTichLuy) VALUES
(N'Nguyễn Văn An',   '0901234567', 150), -- Có 150 điểm (= 150.000 VNĐ tiền đổi điểm)
(N'Trần Thị Bích',   '0912345678',  65), -- Có 65 điểm
(N'Lê Hoàng Tuấn',   '0987654321',  20), -- Có 20 điểm
(N'Võ Ngọc Hà',      '0933445566',   0); -- 0 điểm

-- 9. CHƯƠNG TRÌNH KHUYẾN MÃI ĐANG CHẠY
INSERT INTO dbo.KhuyenMai (TenKM, LoaiGiam, GiaTri, NgayBatDau, NgayKetThuc, TrangThai) VALUES
(N'Mùa hè sảng khoái - Giảm 10% Coca-Cola & Trà xanh', 'PHAN_TRAM', 10.00, DATEADD(DAY, -7, SYSDATETIME()), DATEADD(DAY, 30, SYSDATETIME()), 'DANG_CHAY'),
(N'Tiết kiệm mỗi ngày - Giảm 500đ mỗi gói Mì Hảo Hảo', 'SO_TIEN', 500.00, DATEADD(DAY, -7, SYSDATETIME()), DATEADD(DAY, 30, SYSDATETIME()), 'DANG_CHAY');

-- Liên kết khuyến mãi với hàng hóa
INSERT INTO dbo.KhuyenMai_HangHoa (MaKM, MaHang) VALUES
(1, 1), -- Coca-Cola
(1, 2), -- Trà xanh Không Độ
(2, 4); -- Mì Hảo Hảo

-- 10. GHI NHẬT KÝ KIỂM TOÁN BAN ĐẦU
INSERT INTO dbo.NhatKyKiemToan (MaTK, ThoiGian, HanhDong, DoiTuong, MaDoiTuong, DiaChiIP, ThietBi, GiaTriMoi) VALUES
(1, SYSDATETIME(), N'KHOI_TAO_HE_THONG', N'HeThong', N'SETUP', '127.0.0.1', N'Windows Server / Setup Script', N'Khởi tạo dữ liệu mẫu ban đầu cho hệ thống Cửa Hàng Tiện Lợi');
GO

PRINT N'======================================================================';
PRINT N'ĐÃ KHỞI TẠO VÀ NẠP TOÀN BỘ DỮ LIỆU THÀNH CÔNG CHO CSDL CuaHangTienLoi!';
PRINT N'Tài khoản đăng nhập hệ thống:';
PRINT N'  1. Quản lý:      admin     / Admin@123';
PRINT N'  2. Kho:          kho01     / Kho@123';
PRINT N'  3. Thu ngân 1:   thungan01 / ThuNgan@123  (Quầy 01)';
PRINT N'  4. Thu ngân 2:   thungan02 / ThuNgan@123  (Quầy 02)';
PRINT N'======================================================================';
GO
