using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public class AuthService : IAuthService
    {
        private readonly TaiKhoanDao _taiKhoanDao;
        private readonly CaDao _caDao;
        private readonly NhatKyDao _nhatKyDao;
        private readonly int _maxFailedAttempts;

        public AuthService(IConfiguration configuration)
        {
            _taiKhoanDao = new TaiKhoanDao(configuration);
            _caDao = new CaDao(configuration);
            _nhatKyDao = new NhatKyDao(configuration);
            _maxFailedAttempts = configuration.GetValue<int>("AppSettings:MaxFailedLoginAttempts", 5);
        }

        public (bool Success, string Message, UserSessionModel? User) Authenticate(string username, string password, string ip, string userAgent)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return (false, "Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu.", null);
            }

            var account = _taiKhoanDao.GetByUsername(username.Trim());
            if (account == null)
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không chính xác.", null);
            }

            // Kiểm tra trạng thái tài khoản
            if (!account.TrangThai)
            {
                WriteAuditLog(account.MaTK, "DANG_NHAP_THAT_BAI", "TaiKhoan", account.TenDangNhap, ip, userAgent, null, "Tài khoản đang bị khóa do nhập sai quá 5 lần");
                return (false, "Tài khoản đã bị tạm khóa do nhập sai mật khẩu quá 5 lần. Vui lòng liên hệ Quản lý để mở khóa.", null);
            }

            // Kiểm tra mật khẩu băm Bcrypt
            bool isPasswordValid = PasswordHasher.VerifyPassword(password, account.MatKhauHash);
            if (!isPasswordValid)
            {
                int currentFailed = _taiKhoanDao.IncrementFailedLogin(account.MaTK, _maxFailedAttempts);
                int remaining = _maxFailedAttempts - currentFailed;

                string logMsg = remaining <= 0 
                    ? $"Nhập sai mật khẩu lần {currentFailed} -> TỰ ĐỘNG KHÓA TÀI KHOẢN" 
                    : $"Nhập sai mật khẩu lần {currentFailed}. Còn {remaining} lần thử.";

                WriteAuditLog(account.MaTK, "DANG_NHAP_THAT_BAI", "TaiKhoan", account.TenDangNhap, ip, userAgent, null, logMsg);

                if (remaining <= 0)
                {
                    return (false, "Tài khoản đã bị KHÓA do nhập sai mật khẩu liên tiếp 5 lần!", null);
                }

                return (false, $"Mật khẩu không chính xác. Bạn còn {remaining} lần thử trước khi tài khoản bị khóa.", null);
            }

            // Đăng nhập thành công -> Reset số lần nhập sai
            _taiKhoanDao.ResetFailedLogin(account.MaTK);

            // Lấy danh sách quyền hạn chi tiết
            var permissions = _taiKhoanDao.GetUserPermissions(account.MaVaiTro);

            // Kiểm tra ca làm việc đang mở (nếu là Thu ngân)
            var openShift = _caDao.GetOpenShiftByThuNgan(account.MaNV);

            var userSession = new UserSessionModel
            {
                MaTK = account.MaTK,
                MaNV = account.MaNV,
                TenDangNhap = account.TenDangNhap,
                HoTen = account.NhanVien?.HoTen ?? account.TenDangNhap,
                MaVaiTro = account.MaVaiTro,
                TenVaiTro = account.VaiTro?.TenVaiTro ?? account.MaVaiTro,
                QuyenList = permissions,
                CurrentMaCa = openShift?.MaCa,
                CurrentMaQuay = openShift?.MaQuay,
                CurrentTenQuay = openShift?.TenQuay
            };

            WriteAuditLog(account.MaTK, "DANG_NHAP_THANH_CONG", "TaiKhoan", account.TenDangNhap, ip, userAgent, null, $"Đăng nhập thành công vào hệ thống với vai trò {userSession.TenVaiTro}");

            return (true, "Đăng nhập thành công.", userSession);
        }

        public (bool Success, string Message) ChangePassword(int maTK, string oldPassword, string newPassword, string ip, string userAgent)
        {
            var account = _taiKhoanDao.GetById(maTK);
            if (account == null)
            {
                return (false, "Không tìm thấy thông tin tài khoản.");
            }

            if (!PasswordHasher.VerifyPassword(oldPassword, account.MatKhauHash))
            {
                WriteAuditLog(maTK, "DOI_MAT_KHAU_THAT_BAI", "TaiKhoan", account.TenDangNhap, ip, userAgent, null, "Nhập sai mật khẩu cũ");
                return (false, "Mật khẩu hiện tại không chính xác.");
            }

            string newHash = PasswordHasher.HashPassword(newPassword);
            _taiKhoanDao.UpdatePassword(maTK, newHash);

            WriteAuditLog(maTK, "DOI_MAT_KHAU_THANH_CONG", "TaiKhoan", account.TenDangNhap, ip, userAgent, "Hash cũ", "Đã cập nhật mật khẩu mới");
            return (true, "Đổi mật khẩu thành công.");
        }

        public void WriteAuditLog(int maTK, string action, string objectType, string objectId, string ip, string device, string? oldVal = null, string? newVal = null)
        {
            try
            {
                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = maTK,
                    HanhDong = action,
                    DoiTuong = objectType,
                    MaDoiTuong = objectId,
                    DiaChiIP = ip,
                    ThietBi = device,
                    GiaTriCu = oldVal,
                    GiaTriMoi = newVal
                });
            }
            catch
            {
                // Trigger TR_NhatKy_ChiThem bảo vệ tính bất biến.
                // Nếu lỗi ghi log ngoài ý muốn, không được làm sập luồng chính.
            }
        }
    }
}
