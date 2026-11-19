-- =========================================================================================
-- HỆ THỐNG QUẢN LÝ KHO VÀ CỬA HÀNG TIỆN LỢI (CuaHangTienLoi)
-- File: 03_DailyBackup.sql
-- Mô tả: Thủ tục lưu trữ sao lưu CSDL hàng ngày theo lịch (NFR03: Sao lưu định kỳ, kiểm tra tính toàn vẹn)
-- =========================================================================================

USE CuaHangTienLoi;
GO

IF OBJECT_ID('dbo.sp_SaoLuuCSDL_HangNgay', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sp_SaoLuuCSDL_HangNgay;
GO

CREATE PROCEDURE dbo.sp_SaoLuuCSDL_HangNgay
    @ThuMucSaoLuu NVARCHAR(500) = N'C:\Backups\CuaHangTienLoi\',
    @SoNgayLuuTru INT = 30
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TenTepSaoLuu NVARCHAR(500);
    DECLARE @ThoiGian NVARCHAR(50);
    DECLARE @CauLenhSQL NVARCHAR(MAX);
    DECLARE @Message NVARCHAR(500);

    -- 1. Tạo chuỗi thời gian định dạng YYYYMMDD_HHMMSS
    SET @ThoiGian = REPLACE(REPLACE(REPLACE(CONVERT(VARCHAR(19), GETDATE(), 120), '-', ''), ' ', '_'), ':', '');
    SET @TenTepSaoLuu = @ThuMucSaoLuu + N'CuaHangTienLoi_Full_' + @ThoiGian + N'.bak';

    -- 2. Đảm bảo thư mục kết thúc bằng dấu gạch chéo
    IF RIGHT(@ThuMucSaoLuu, 1) <> '\'
        SET @ThuMucSaoLuu = @ThuMucSaoLuu + '\';

    BEGIN TRY
        PRINT N'Bắt đầu sao lưu CSDL CuaHangTienLoi vào: ' + @TenTepSaoLuu;

        -- 3. Thực thi câu lệnh BACKUP DATABASE có COMPRESSION và CHECKSUM
        BACKUP DATABASE CuaHangTienLoi
        TO DISK = @TenTepSaoLuu
        WITH FORMAT,
             INIT,
             NAME = N'CuaHangTienLoi-Full Database Backup',
             SKIP,
             NOREWIND,
             NOUNLOAD,
             COMPRESSION,
             CHECKSUM,
             STATS = 10;

        -- 4. Kiểm tra tính toàn vẹn của tệp sao lưu (RESTORE VERIFYONLY)
        RESTORE VERIFYONLY
        FROM DISK = @TenTepSaoLuu
        WITH CHECKSUM;

        PRINT N'Kiểm tra tính toàn vẹn tệp sao lưu thành công!';

        -- 5. Ghi vào nhật ký kiểm toán hệ thống
        INSERT INTO dbo.NhatKyKiemToan (MaTK, ThoiGian, HanhDong, DoiTuong, MaDoiTuong, DiaChiIP, ThietBi, GiaTriMoi)
        VALUES (
            (SELECT TOP 1 MaTK FROM dbo.TaiKhoan WHERE MaVaiTro = 'QUAN_LY'),
            SYSDATETIME(),
            N'SAO_LUU_CSDL',
            N'Database',
            @TenTepSaoLuu,
            '127.0.0.1',
            N'SQL Server Agent / Stored Procedure',
            N'Sao lưu CSDL thành công. Tệp: ' + @TenTepSaoLuu
        );

        PRINT N'Hoàn tất sao lưu CSDL lúc: ' + CONVERT(NVARCHAR(30), GETDATE(), 120);
    END TRY
    BEGIN CATCH
        SET @Message = ERROR_MESSAGE();
        PRINT N'LỖI KHI SAO LƯU CSDL: ' + @Message;
        THROW;
    END CATCH
END;
GO
