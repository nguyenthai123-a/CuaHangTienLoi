-- =========================================================================================
-- HỆ THỐNG QUẢN LÝ KHO VÀ CỬA HÀNG TIỆN LỢI (CuaHangTienLoi)
-- File: 01_CreateDatabase.sql
-- Mô tả: Khởi tạo CSDL, Sequence, Bảng, Khóa chính/ngoại, Check constraints, Triggers và Indexes
-- =========================================================================================

USE master;
GO

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

-- =========================================================================================
-- SEQUENCE CẤP SỐ TỰ ĐỘNG CHO HÓA ĐƠN VÀ PHIẾU TRẢ HÀNG
-- =========================================================================================
CREATE SEQUENCE dbo.seq_HoaDon   AS INT START WITH 1 INCREMENT BY 1;
CREATE SEQUENCE dbo.seq_PhieuTra AS INT START WITH 1 INCREMENT BY 1;
GO

-- =========================================================================================
-- M1. PHÂN HỆ TÀI KHOẢN, PHÂN QUYỀN & NHẬT KÝ KIỂM TOÁN
-- =========================================================================================

-- Bảng Vai trò người dùng
CREATE TABLE dbo.VaiTro (
    MaVaiTro  VARCHAR(20)   NOT NULL PRIMARY KEY,
    TenVaiTro NVARCHAR(50)  NOT NULL,
    MoTa      NVARCHAR(200) NULL
);

-- Bảng Quyền hạn chi tiết
CREATE TABLE dbo.Quyen (
    MaQuyen   VARCHAR(30)   NOT NULL PRIMARY KEY,
    TenQuyen  NVARCHAR(100) NOT NULL,
    LaNhayCam BIT NOT NULL DEFAULT 0
);

-- Bảng Phân quyền Vai trò - Quyền
CREATE TABLE dbo.VaiTro_Quyen (
    MaVaiTro VARCHAR(20) NOT NULL REFERENCES dbo.VaiTro(MaVaiTro),
    MaQuyen  VARCHAR(30) NOT NULL REFERENCES dbo.Quyen(MaQuyen),
    PRIMARY KEY (MaVaiTro, MaQuyen)
);

-- Bảng Nhân viên
CREATE TABLE dbo.NhanVien (
    MaNV        INT IDENTITY(1,1) PRIMARY KEY,
    HoTen       NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15)   NULL,
    TrangThai   BIT NOT NULL DEFAULT 1              -- 1 = đang làm việc, 0 = đã nghỉ
);

-- Bảng Tài khoản đăng nhập (Bảo mật: Bcrypt hash, Tự động khóa sau số lần nhập sai)
CREATE TABLE dbo.TaiKhoan (
    MaTK         INT IDENTITY(1,1) PRIMARY KEY,
    MaNV         INT NOT NULL UNIQUE REFERENCES dbo.NhanVien(MaNV),    -- Quan hệ 1-1
    TenDangNhap  VARCHAR(50)  NOT NULL UNIQUE,
    MatKhauHash  VARCHAR(255) NOT NULL,                -- bcrypt/argon2
    TrangThai    BIT NOT NULL DEFAULT 1,               -- 1 = hoạt động, 0 = bị khóa
    SoLanNhapSai INT NOT NULL DEFAULT 0 CHECK (SoLanNhapSai >= 0),
    MaVaiTro     VARCHAR(20) NOT NULL REFERENCES dbo.VaiTro(MaVaiTro)
);

-- Bảng Quầy thu ngân
CREATE TABLE dbo.Quay (
    MaQuay    INT IDENTITY(1,1) PRIMARY KEY,
    TenQuay   NVARCHAR(50) NOT NULL,
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'HOAT_DONG' CHECK (TrangThai IN ('HOAT_DONG','NGUNG'))
);

-- Bảng Phân công Quầy cho Nhân viên
CREATE TABLE dbo.PhanCongQuay (
    MaNV   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaQuay INT NOT NULL REFERENCES dbo.Quay(MaQuay),
    PRIMARY KEY (MaNV, MaQuay)
);

-- Bảng Nhật ký kiểm toán bất biến (Audit Trail)
CREATE TABLE dbo.NhatKyKiemToan (
    MaNK       BIGINT IDENTITY(1,1) PRIMARY KEY,
    MaTK       INT NOT NULL REFERENCES dbo.TaiKhoan(MaTK),
    ThoiGian   DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    HanhDong   NVARCHAR(50)  NOT NULL,
    DoiTuong   NVARCHAR(50)  NULL,                     -- Bảng/đối tượng bị tác động
    MaDoiTuong NVARCHAR(50)  NULL,                     -- Mã đối tượng (vd HD-0012)
    DiaChiIP   VARCHAR(45)   NULL,
    ThietBi    NVARCHAR(200) NULL,                     -- NFR02: IP/thiết bị truy cập
    GiaTriCu   NVARCHAR(MAX) NULL,
    GiaTriMoi  NVARCHAR(MAX) NULL
);
GO

-- TRIGGER BẢO VỆ NHẬT KÝ KIỂM TOÁN: Bất biến - Chỉ thêm, cấm sửa và cấm xóa
CREATE TRIGGER dbo.TR_NhatKy_ChiThem ON dbo.NhatKyKiemToan
INSTEAD OF UPDATE, DELETE
AS
BEGIN
    THROW 50001, N'Nhật ký kiểm toán chỉ được thêm, không được sửa hoặc xóa.', 1;
END;
GO

-- =========================================================================================
-- M2. PHÂN HỆ DANH MỤC HÀNG HÓA & NHÀ CUNG CẤP
-- =========================================================================================

-- Bảng Nhóm hàng hóa
CREATE TABLE dbo.NhomHang (
    MaNhom  INT IDENTITY(1,1) PRIMARY KEY,
    TenNhom NVARCHAR(100) NOT NULL
);

-- Bảng Hàng hóa
CREATE TABLE dbo.HangHoa (
    MaHang  INT IDENTITY(1,1) PRIMARY KEY,
    MaVach  VARCHAR(30)    NOT NULL UNIQUE,
    TenHang NVARCHAR(200)  NOT NULL,
    MaNhom  INT NOT NULL REFERENCES dbo.NhomHang(MaNhom),
    DonViTinh NVARCHAR(30) NOT NULL,
    GiaBan  DECIMAL(18,0)  NOT NULL CHECK (GiaBan >= 0),
    GiaNhap DECIMAL(18,0)  NULL CHECK (GiaNhap >= 0),
    DinhMucTon INT NOT NULL DEFAULT 0 CHECK (DinhMucTon >= 0),
    GioiHanDoTuoi INT NOT NULL DEFAULT 0 CHECK (GioiHanDoTuoi >= 0),    -- 0 = không giới hạn; 18 = trên 18 tuổi (Bia, Rượu, Thuốc lá)
    SoNgayBaoQuanMacDinh INT NULL CHECK (SoNgayBaoQuanMacDinh > 0),
    TrangThaiKinhDoanh BIT NOT NULL DEFAULT 1                         -- 1 = Đang kinh doanh, 0 = ngừng
);

-- Bảng Nhà cung cấp
CREATE TABLE dbo.NhaCungCap (
    MaNCC         INT IDENTITY(1,1) PRIMARY KEY,
    TenNCC        NVARCHAR(150) NOT NULL,
    SoDienThoai   VARCHAR(15)   NOT NULL,
    DiaChi        NVARCHAR(255) NULL,
    NguoiLienHe   NVARCHAR(100) NULL
);

-- =========================================================================================
-- M3. PHÂN HỆ KHO HÀNG & NGUYÊN TẮC FEFO (FIRST EXPIRED, FIRST OUT)
-- =========================================================================================

-- Bảng Lô hàng quản lý theo Hạn sử dụng (HSD)
CREATE TABLE dbo.LoHang (
    MaLo      INT IDENTITY(1,1) PRIMARY KEY,
    MaHang    INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    SoLo      NVARCHAR(50) NOT NULL,
    HanSuDung DATE NOT NULL,
    NgayNhap  DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    SlTonKho  INT NOT NULL DEFAULT 0 CHECK (SlTonKho >= 0),
    SlTonQuay INT NOT NULL DEFAULT 0 CHECK (SlTonQuay >= 0)
);

-- Bảng Phiếu nhập kho
CREATE TABLE dbo.PhieuNhap (
    MaPN INT IDENTITY(1,1) PRIMARY KEY,
    SoPhieuNhap AS ('PN-' + CASE WHEN MaPN < 10000 THEN RIGHT('0000' + CAST(MaPN AS VARCHAR(10)), 4) ELSE CAST(MaPN AS VARCHAR(10)) END) PERSISTED,
    NgayNhap    DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    SoPhieuGiao NVARCHAR(50) NOT NULL,
    TongTien    DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (TongTien >= 0),
    MaNCC   INT NOT NULL REFERENCES dbo.NhaCungCap(MaNCC),
    MaNVLap INT NOT NULL REFERENCES dbo.NhanVien(MaNV)
);

-- Bảng Chi tiết phiếu nhập kho (Mỗi dòng tạo ra 1 Lô hàng mới)
CREATE TABLE dbo.ChiTietPhieuNhap (
    MaCTPN INT IDENTITY(1,1) PRIMARY KEY,
    MaPN   INT NOT NULL REFERENCES dbo.PhieuNhap(MaPN),
    MaLo   INT NOT NULL UNIQUE REFERENCES dbo.LoHang(MaLo),
    SoLuong   INT NOT NULL CHECK (SoLuong > 0),
    GiaNhap   DECIMAL(18,0) NOT NULL CHECK (GiaNhap >= 0),
    HanSuDung DATE NOT NULL
);

-- Bảng Phiếu xuất kho (Xuất lên quầy, Xuất hủy hư hỏng/hết hạn, Trả NCC)
CREATE TABLE dbo.PhieuXuat (
    MaPX INT IDENTITY(1,1) PRIMARY KEY,
    SoPhieuXuat AS ('PX-' + CASE WHEN MaPX < 10000 THEN RIGHT('0000' + CAST(MaPX AS VARCHAR(10)), 4) ELSE CAST(MaPX AS VARCHAR(10)) END) PERSISTED,
    LoaiXuat  VARCHAR(10) NOT NULL CHECK (LoaiXuat IN ('LEN_QUAY','HUY','TRA_NCC')),
    NgayXuat  DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    LyDo      NVARCHAR(255) NULL,
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'HOAN_TAT' CHECK (TrangThai IN ('CHO_DUYET','HOAN_TAT','TU_CHOI')),
    KhuVuc    VARCHAR(5) NULL CHECK (KhuVuc IN ('KHO','QUAY')),  -- chỉ dành cho xuất hủy
    MaNCC     INT NULL REFERENCES dbo.NhaCungCap(MaNCC),          -- chỉ dành cho trả NCC
    MaNVLap   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNVDuyet INT NULL REFERENCES dbo.NhanVien(MaNV),
    CONSTRAINT CK_PX_KhuVuc CHECK ((LoaiXuat = 'HUY' AND KhuVuc IS NOT NULL AND LyDo IS NOT NULL) OR (LoaiXuat <> 'HUY' AND KhuVuc IS NULL)),
    CONSTRAINT CK_PX_NCC    CHECK ((LoaiXuat = 'TRA_NCC' AND MaNCC IS NOT NULL AND LyDo IS NOT NULL) OR (LoaiXuat <> 'TRA_NCC' AND MaNCC IS NULL)),
    CONSTRAINT CK_PX_Duyet  CHECK (LoaiXuat = 'HUY' OR TrangThai = 'HOAN_TAT')    -- chỉ xuất hủy cần qua duyệt
);

-- Bảng Chi tiết phiếu xuất kho
CREATE TABLE dbo.ChiTietPhieuXuat (
    MaCTPX INT IDENTITY(1,1) PRIMARY KEY,
    MaPX   INT NOT NULL REFERENCES dbo.PhieuXuat(MaPX),
    MaLo   INT NOT NULL REFERENCES dbo.LoHang(MaLo),
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    GhiChu  NVARCHAR(255) NULL
);

-- Bảng Phiếu kiểm kê kho / quầy
CREATE TABLE dbo.PhieuKiemKe (
    MaKK INT IDENTITY(1,1) PRIMARY KEY,
    SoPhieuKiemKe AS ('KK-' + CASE WHEN MaKK < 10000 THEN RIGHT('0000' + CAST(MaKK AS VARCHAR(10)), 4) ELSE CAST(MaKK AS VARCHAR(10)) END) PERSISTED,
    NgayKiemKe DATE NOT NULL DEFAULT (CONVERT(date, SYSDATETIME())),
    KhuVuc VARCHAR(5) NOT NULL CHECK (KhuVuc IN ('KHO','QUAY')),
    TrangThaiDuyet VARCHAR(10) NOT NULL DEFAULT 'CHO_DUYET' CHECK (TrangThaiDuyet IN ('CHO_DUYET','DA_DUYET','TU_CHOI')),
    MaNVLap   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNVDuyet INT NULL REFERENCES dbo.NhanVien(MaNV)
);

-- Bảng Chi tiết kiểm kê kho / quầy
CREATE TABLE dbo.ChiTietKiemKe (
    MaCTKK INT IDENTITY(1,1) PRIMARY KEY,
    MaKK   INT NOT NULL REFERENCES dbo.PhieuKiemKe(MaKK),
    MaLo   INT NOT NULL REFERENCES dbo.LoHang(MaLo),
    SlHeThong INT NOT NULL CHECK (SlHeThong >= 0),
    SlThucTe  INT NOT NULL CHECK (SlThucTe >= 0),
    ChenhLech AS (SlThucTe - SlHeThong) PERSISTED
);

-- Bảng Thẻ kho (Truy vết lịch sử xuất nhập tồn từng mặt hàng/lô hàng)
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

-- =========================================================================================
-- M5. PHÂN HỆ QUẢN LÝ CA LÀM VIỆC & TIỀN MẶT
-- =========================================================================================

-- Bảng Ca làm việc (Mỗi quầy chỉ có DUY NHẤT 1 ca đang mở tại một thời điểm)
CREATE TABLE dbo.Ca (
    MaCa INT IDENTITY(1,1) PRIMARY KEY,
    MaQuay      INT NOT NULL REFERENCES dbo.Quay(MaQuay),
    MaThuNgan   INT NOT NULL REFERENCES dbo.NhanVien(MaNV),
    MaNguoiChot INT NULL REFERENCES dbo.NhanVien(MaNV),              -- Quản lý có thể chốt thay
    GioMo  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    GioDong DATETIME2(0) NULL,
    TienDauCa DECIMAL(18,0) NOT NULL CHECK (TienDauCa >= 0),
    TienMatHeThong DECIMAL(18,0) NULL,                              -- Tiền mặt hệ thống tính toán khi chốt ca
    TienMatThucTe  DECIMAL(18,0) NULL CHECK (TienMatThucTe >= 0),   -- Tiền mặt đếm thực tế
    ChenhLech AS (TienMatThucTe - TienMatHeThong) PERSISTED,
    LyDoChenhLech NVARCHAR(255) NULL,
    TrangThai VARCHAR(5) NOT NULL DEFAULT 'MO' CHECK (TrangThai IN ('MO','DONG')),
    CONSTRAINT CK_Ca_TrangThai CHECK (
        (TrangThai = 'MO'   AND GioDong IS NULL AND TienMatThucTe IS NULL AND TienMatHeThong IS NULL AND MaNguoiChot IS NULL) OR
        (TrangThai = 'DONG' AND GioDong IS NOT NULL AND TienMatThucTe IS NOT NULL AND TienMatHeThong IS NOT NULL AND MaNguoiChot IS NOT NULL))
);

-- Bảng Báo cáo tổng kết ca
CREATE TABLE dbo.BaoCaoCa (
    MaBC INT IDENTITY(1,1) PRIMARY KEY,
    MaCa INT NOT NULL UNIQUE REFERENCES dbo.Ca(MaCa),               -- 1 - 0..1
    NgayLap DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    DoanhThuTienMat DECIMAL(18,0) NOT NULL DEFAULT 0,
    DoanhThuThe     DECIMAL(18,0) NOT NULL DEFAULT 0,
    DoanhThuQR      DECIMAL(18,0) NOT NULL DEFAULT 0,
    TongDoanhThu AS (DoanhThuTienMat + DoanhThuThe + DoanhThuQR) PERSISTED
);

-- =========================================================================================
-- M6. PHÂN HỆ KHÁCH HÀNG THÂN THIẾT & KHUYẾN MÃI
-- =========================================================================================

-- Bảng Khách hàng thân thiết
CREATE TABLE dbo.KhachHang (
    MaKH INT IDENTITY(1,1) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL UNIQUE,
    DiemTichLuy INT NOT NULL DEFAULT 0 CHECK (DiemTichLuy >= 0)
);

-- Bảng Chương trình khuyến mãi
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

-- Bảng Liên kết Khuyến mãi - Hàng hóa
CREATE TABLE dbo.KhuyenMai_HangHoa (
    MaKM   INT NOT NULL REFERENCES dbo.KhuyenMai(MaKM),
    MaHang INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    PRIMARY KEY (MaKM, MaHang)
);

-- =========================================================================================
-- M4. PHÂN HỆ BÁN HÀNG TẠI QUẦY (POS) & HÓA ĐƠN
-- =========================================================================================

-- Bảng Hóa đơn bán hàng
CREATE TABLE dbo.HoaDon (
    MaHoaDon INT IDENTITY(1,1) PRIMARY KEY,
    SoHD INT NOT NULL UNIQUE DEFAULT (NEXT VALUE FOR dbo.seq_HoaDon),
    SoHoaDon AS ('HD-' + CASE WHEN SoHD < 10000 THEN RIGHT('0000' + CAST(SoHD AS VARCHAR(10)), 4) ELSE CAST(SoHD AS VARCHAR(10)) END) PERSISTED,
    NgayGio  DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    TongTien DECIMAL(18,0) NOT NULL CHECK (TongTien >= 0),           -- Tổng tiền hàng
    GiamGia  DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (GiamGia >= 0), -- Khuyến mãi + đổi điểm
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

-- Bảng Chi tiết hóa đơn (Áp dụng FEFO tự động gắn MaLo)
CREATE TABLE dbo.ChiTietHoaDon (
    MaCT INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),
    MaHang   INT NOT NULL REFERENCES dbo.HangHoa(MaHang),
    MaLo     INT NOT NULL REFERENCES dbo.LoHang(MaLo),               -- Lô lấy theo FEFO
    MaKM     INT NULL REFERENCES dbo.KhuyenMai(MaKM),
    SoLuong  INT NOT NULL CHECK (SoLuong > 0),
    DonGia   DECIMAL(18,0) NOT NULL CHECK (DonGia >= 0),
    SoTienGiam DECIMAL(18,0) NOT NULL DEFAULT 0 CHECK (SoTienGiam >= 0),
    GiaVon   DECIMAL(18,0) NOT NULL CHECK (GiaVon >= 0),
    ThanhTien AS (SoLuong * DonGia) PERSISTED,
    CONSTRAINT CK_CTHD_Giam CHECK (SoTienGiam <= SoLuong * DonGia)
);

-- Bảng Giao dịch Thanh toán (Hỗ trợ Đa phương thức: Tiền mặt, Thẻ, QR)
CREATE TABLE dbo.ThanhToan (
    MaTT INT IDENTITY(1,1) PRIMARY KEY,
    MaHoaDon INT NOT NULL REFERENCES dbo.HoaDon(MaHoaDon),          -- 1 hóa đơn có thể có nhiều phương thức
    LoaiThanhToan VARCHAR(10) NOT NULL CHECK (LoaiThanhToan IN ('TIEN_MAT','THE','QR')),
    SoTien DECIMAL(18,0) NOT NULL CHECK (SoTien > 0),
    TrangThai VARCHAR(10) NOT NULL DEFAULT 'THANH_CONG' CHECK (TrangThai IN ('THANH_CONG','DA_HOAN')),
    TienKhachDua DECIMAL(18,0) NULL, TienThoi DECIMAL(18,0) NULL,   -- Cho tiền mặt
    SoTheCuoi CHAR(4) NULL, MaChuanChi VARCHAR(50) NULL,            -- Cho thẻ
    MaGiaoDich VARCHAR(50) NULL, LoaiVi NVARCHAR(30) NULL,          -- Cho QR/Ví điện tử
    CONSTRAINT CK_TT_TienMat CHECK (LoaiThanhToan <> 'TIEN_MAT' OR (TienKhachDua IS NOT NULL AND TienThoi IS NOT NULL AND TienKhachDua >= SoTien)),
    CONSTRAINT CK_TT_The     CHECK (LoaiThanhToan <> 'THE' OR (SoTheCuoi IS NOT NULL AND MaChuanChi IS NOT NULL)),
    CONSTRAINT CK_TT_QR      CHECK (LoaiThanhToan <> 'QR'  OR MaGiaoDich IS NOT NULL)
);

-- Bảng Phiếu trả hàng / Đổi trả
CREATE TABLE dbo.PhieuTraHang (
    MaPhieuTra INT IDENTITY(1,1) PRIMARY KEY,
    SoPT INT NULL,                                                  -- Cấp từ seq_PhieuTra khi Quản lý duyệt
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

-- Bảng Chi tiết phiếu trả hàng
CREATE TABLE dbo.ChiTietPhieuTra (
    MaCTPT INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuTra INT NOT NULL REFERENCES dbo.PhieuTraHang(MaPhieuTra),
    MaCT   INT NOT NULL REFERENCES dbo.ChiTietHoaDon(MaCT),
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    TinhTrangHang VARCHAR(15) NOT NULL CHECK (TinhTrangHang IN ('CON_DUNG_DUOC','KHONG_DUNG_DUOC'))
);

-- Bảng Lịch sử giao dịch điểm tích lũy
CREATE TABLE dbo.GiaoDichDiem (
    MaGD INT IDENTITY(1,1) PRIMARY KEY,
    MaKH INT NOT NULL REFERENCES dbo.KhachHang(MaKH),
    Loai VARCHAR(15) NOT NULL CHECK (Loai IN ('TICH','DOI','TRU_TRA_HANG','TRU_HUY_HD')),
    SoDiem INT NOT NULL CHECK (SoDiem > 0),
    Ngay DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
    MaHoaDon    INT NULL REFERENCES dbo.HoaDon(MaHoaDon),
    MaPhieuTra INT NULL REFERENCES dbo.PhieuTraHang(MaPhieuTra)
);

-- =========================================================================================
-- BẢNG CẤU HÌNH HỆ THỐNG
-- =========================================================================================
CREATE TABLE dbo.CauHinhHeThong (
    Khoa  VARCHAR(50)    NOT NULL PRIMARY KEY,
    GiaTri NVARCHAR(100) NOT NULL,
    MoTa  NVARCHAR(200)  NULL
);
GO

-- =========================================================================================
-- CHỈ MỤC & RÀNG BUỘC DUY NHẤT CÓ ĐIỀU KIỆN (FILTERED UNIQUE INDEXES & PERFORMANCE INDEXES)
-- =========================================================================================

-- Quy tắc nghiệp vụ bắt buộc: Mỗi quầy chỉ có DUY NHẤT 1 ca đang mở tại một thời điểm
CREATE UNIQUE INDEX UX_Ca_QuayDangMo   ON dbo.Ca(MaQuay) WHERE TrangThai = 'MO';

-- Phiếu trả hàng chỉ có duy nhất một số phiếu sau khi được duyệt
CREATE UNIQUE INDEX UX_PhieuTra_SoPT   ON dbo.PhieuTraHang(SoPT) WHERE SoPT IS NOT NULL;

-- Chỉ mục hỗ trợ tối ưu thuật toán FEFO (Lọc lô còn tồn quầy, sắp xếp hạn sử dụng tăng dần)
CREATE INDEX IX_LoHang_FEFO            ON dbo.LoHang(MaHang, HanSuDung) INCLUDE (SlTonQuay) WHERE SlTonQuay > 0;

-- Các chỉ mục tối ưu hiệu năng truy vấn hóa đơn, thẻ kho, nhật ký
CREATE INDEX IX_HoaDon_Ca              ON dbo.HoaDon(MaCa);
CREATE INDEX IX_HoaDon_NgayGio         ON dbo.HoaDon(NgayGio);
CREATE INDEX IX_TheKho_Hang_Ngay       ON dbo.TheKho(MaHang, Ngay);
CREATE INDEX IX_ChiTietHoaDon_MaHang   ON dbo.ChiTietHoaDon(MaHang) INCLUDE (SoLuong, DonGia, SoTienGiam, GiaVon);
CREATE INDEX IX_NhatKy_ThoiGian        ON dbo.NhatKyKiemToan(ThoiGian, MaTK);
GO

-- =========================================================================================
-- DỮ LIỆU CỐT LÕI HỆ THỐNG: VAI TRÒ, QUYỀN VÀ CẤU HÌNH
-- =========================================================================================
INSERT dbo.VaiTro (MaVaiTro, TenVaiTro, MoTa) VALUES
 ('QUAN_LY', N'Quản lý', N'Toàn quyền quản trị, duyệt xuất hủy, duyệt kiểm kê, duyệt trả hàng, báo cáo'),
 ('NHAN_VIEN_KHO', N'Nhân viên kho', N'Quản lý nhập xuất kiểm kê hàng hóa, tra cứu thẻ kho'),
 ('THU_NGAN', N'Thu ngân', N'Bán hàng tại quầy, mở/đóng ca, lập yêu cầu đổi trả, tích điểm');

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
 ('TI_LE_QUY_DOI_DIEM', '1000', N'Số tiền giảm cho mỗi điểm tích lũy (VNĐ)'),
 ('TI_LE_TICH_DIEM', '10000', N'Số tiền chi tiêu tương ứng 1 điểm tích lũy (VNĐ)'),
 ('CANH_BAO_HSD_NGAY', '30', N'Số ngày cảnh báo hàng sắp hết hạn');
GO
