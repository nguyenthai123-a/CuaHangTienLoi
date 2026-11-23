using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Services
{
    public class PosService : IPosService
    {
        private readonly string _connectionString;
        private readonly HangHoaDao _hangHoaDao;
        private readonly KhoDao _khoDao;
        private readonly CaDao _caDao;
        private readonly BanHangDao _banHangDao;
        private readonly KhachHangDao _khachHangDao;
        private readonly KhuyenMaiDao _khuyenMaiDao;
        private readonly NhatKyDao _nhatKyDao;
        private readonly IFefoService _fefoService;

        public PosService(IConfiguration configuration, IFefoService fefoService)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("DefaultConnection not found");
            _hangHoaDao = new HangHoaDao(configuration);
            _khoDao = new KhoDao(configuration);
            _caDao = new CaDao(configuration);
            _banHangDao = new BanHangDao(configuration);
            _khachHangDao = new KhachHangDao(configuration);
            _khuyenMaiDao = new KhuyenMaiDao(configuration);
            _nhatKyDao = new NhatKyDao(configuration);
            _fefoService = fefoService;
        }

        public PosCartItem? ScanBarcode(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode)) return null;

            var product = _hangHoaDao.GetByBarcode(barcode.Trim());
            if (product == null) return null;

            return MapToCartItem(product);
        }

        public List<PosCartItem> SearchProducts(string? keyword)
        {
            var products = _hangHoaDao.GetAll(null, string.IsNullOrWhiteSpace(keyword) ? null : keyword.Trim(), false);
            return products.Select(MapToCartItem).ToList();
        }

        private PosCartItem MapToCartItem(HangHoa product)
        {
            // Kiểm tra chương trình khuyến mãi đang áp dụng cho sản phẩm
            var promo = _khuyenMaiDao.GetPromotionForProduct(product.MaHang);
            decimal discountPerUnit = 0;

            if (promo != null)
            {
                if (promo.LoaiGiam == "PHAN_TRAM")
                {
                    discountPerUnit = Math.Round((product.GiaBan * promo.GiaTri) / 100m, 0);
                }
                else if (promo.LoaiGiam == "SO_TIEN")
                {
                    discountPerUnit = Math.Min(product.GiaBan, promo.GiaTri);
                }
            }

            return new PosCartItem
            {
                MaHang = product.MaHang,
                MaNhom = product.MaNhom,
                TenNhom = product.TenNhom,
                MaVach = product.MaVach,
                TenHang = product.TenHang,
                DonViTinh = product.DonViTinh,
                DonGia = product.GiaBan,
                SoLuong = 1,
                GioiHanDoTuoi = product.GioiHanDoTuoi,
                MaKM = promo?.MaKM,
                TenKM = promo?.TenKM,
                GiamGiaDonVi = discountPerUnit,
                TonQuayHienTai = product.TongTonQuay
            };
        }

        public KhachHang? FindCustomerByPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return null;
            return _khachHangDao.GetByPhone(phone.Trim());
        }

        public KhachHang CreateCustomer(string name, string phone)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("Họ tên và số điện thoại khách hàng không được để trống.");

            var existing = _khachHangDao.GetByPhone(phone.Trim());
            if (existing != null) return existing;

            var newKh = new KhachHang
            {
                HoTen = name.Trim(),
                SoDienThoai = phone.Trim(),
                DiemTichLuy = 0
            };
            int newId = _khachHangDao.Insert(newKh);
            newKh.MaKH = newId;
            return newKh;
        }

        /// <summary>
        /// Xử lý quy trình thanh toán bán hàng POS hoàn chỉnh:
        /// - Kiểm tra ca làm việc đang mở
        /// - Bắt buộc xác nhận độ tuổi với hàng 18+ (Bia, Rượu, Thuốc lá)
        /// - Áp dụng thuật toán FEFO tự động trừ các lô hàng hết hạn sớm nhất trước
        /// - Áp dụng khuyến mãi & đổi điểm tích lũy
        /// - Cấp số hóa đơn từ Sequence (dbo.seq_HoaDon)
        /// - Ghi nhận thanh toán (Tiền mặt / Thẻ / QR)
        /// - Ghi nhận thẻ kho (TheKho)
        /// - Giao dịch ACID tường minh
        /// </summary>
        public PosCheckoutResult Checkout(PosCheckoutRequest req, int currentMaNV, int currentMaTK, string ip, string userAgent)
        {
            if (req.Items == null || req.Items.Count == 0)
                throw new InvalidOperationException("Giỏ hàng đang trống, không thể thanh toán.");

            // 1. Kiểm tra trạng thái Ca làm việc
            var ca = _caDao.GetById(req.MaCa);
            if (ca == null || ca.TrangThai != "MO")
                throw new InvalidOperationException("Ca làm việc hiện tại không hợp lệ hoặc đã đóng. Vui lòng mở ca trước khi bán hàng.");

            // 2. Kiểm tra hàng hóa giới hạn độ tuổi (GioiHanDoTuoi >= 18)
            bool containsRestrictedItems = req.Items.Any(i => i.GioiHanDoTuoi >= 18);
            if (containsRestrictedItems && !req.DaXacNhanDoTuoi)
            {
                throw new InvalidOperationException("Đơn hàng có chứa sản phẩm giới hạn độ tuổi (Bia/Rượu/Thuốc lá). Thu ngân bắt buộc phải kiểm tra giấy tờ tùy thân và xác nhận khách hàng đủ 18 tuổi.");
            }

            // 3. Tính toán tổng tiền hàng và giảm giá
            decimal tongTienHang = req.Items.Sum(i => i.DonGia * i.SoLuong);
            decimal giamGiaKhuyenMai = req.Items.Sum(i => i.GiamGiaDonVi * i.SoLuong);

            // Kiểm tra điểm đổi (1 điểm = 1.000 VNĐ)
            decimal giamGiaDiem = 0;
            if (req.MaKH.HasValue && req.DiemDoi > 0)
            {
                var customer = _khachHangDao.GetById(req.MaKH.Value);
                if (customer != null)
                {
                    int diemKhaDung = Math.Min(customer.DiemTichLuy, req.DiemDoi);
                    giamGiaDiem = diemKhaDung * 1000m;
                    // Không cho giảm quá số tiền cần trả
                    if (giamGiaKhuyenMai + giamGiaDiem > tongTienHang)
                    {
                        giamGiaDiem = tongTienHang - giamGiaKhuyenMai;
                        diemKhaDung = (int)(giamGiaDiem / 1000m);
                    }
                    req.DiemDoi = diemKhaDung;
                }
            }

            decimal tongGiamGia = giamGiaKhuyenMai + giamGiaDiem;
            decimal phaiThanhToan = tongTienHang - tongGiamGia;

            using var conn = new SqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            try
            {
                // 4. Tạo Hóa Đơn bán hàng
                var hoaDon = new HoaDon
                {
                    TongTien = tongTienHang,
                    GiamGia = tongGiamGia,
                    DaXacNhanDoTuoi = req.DaXacNhanDoTuoi,
                    MaCa = req.MaCa,
                    MaKH = req.MaKH
                };

                int maHoaDon = _banHangDao.CreateHoaDon(hoaDon, conn, trans);
                hoaDon.MaHoaDon = maHoaDon;

                // 5. Duyệt giỏ hàng, áp dụng FEFO để phân bổ từng lô hàng và trừ tồn quầy
                foreach (var item in req.Items)
                {
                    // Lấy phân bổ FEFO (Lô hạn dùng gần nhất xuất trước)
                    var allocations = _fefoService.AllocateLotsForSale(item.MaHang, item.SoLuong);

                    foreach (var alloc in allocations)
                    {
                        // Trừ tồn quầy
                        int tonSau = _khoDao.DeductShelfStock(alloc.MaLo, alloc.SoLuongXuat, conn, trans);

                        // Thêm chi tiết hóa đơn
                        decimal itemDiscount = item.GiamGiaDonVi * alloc.SoLuongXuat;
                        _banHangDao.AddChiTietHoaDon(new ChiTietHoaDon
                        {
                            MaHoaDon = maHoaDon,
                            MaHang = item.MaHang,
                            MaLo = alloc.MaLo,
                            MaKM = item.MaKM,
                            SoLuong = alloc.SoLuongXuat,
                            DonGia = item.DonGia,
                            SoTienGiam = itemDiscount,
                            GiaVon = alloc.GiaNhap
                        }, conn, trans);

                        // Ghi nhận biến động Thẻ kho
                        _khoDao.InsertTheKho(new TheKho
                        {
                            MaHang = item.MaHang,
                            MaLo = alloc.MaLo,
                            KhuVuc = "QUAY",
                            LoaiGiaoDich = "BAN",
                            LoaiChungTu = "HD",
                            MaChungTu = maHoaDon,
                            SlNhap = 0,
                            SlXuat = alloc.SoLuongXuat,
                            TonSau = tonSau
                        }, conn, trans);
                    }
                }

                // 6. Xử lý thanh toán
                decimal tienKhachDua = req.TienKhachDua;
                decimal tienThoi = 0;

                if (req.PhuongThuc == "TIEN_MAT")
                {
                    if (tienKhachDua < phaiThanhToan)
                        throw new InvalidOperationException($"Số tiền khách đưa ({tienKhachDua:N0}đ) không đủ để thanh toán ({phaiThanhToan:N0}đ).");

                    tienThoi = tienKhachDua - phaiThanhToan;

                    _banHangDao.AddThanhToan(new ThanhToan
                    {
                        MaHoaDon = maHoaDon,
                        LoaiThanhToan = "TIEN_MAT",
                        SoTien = phaiThanhToan,
                        TrangThai = "THANH_CONG",
                        TienKhachDua = tienKhachDua,
                        TienThoi = tienThoi
                    }, conn, trans);
                }
                else if (req.PhuongThuc == "THE")
                {
                    _banHangDao.AddThanhToan(new ThanhToan
                    {
                        MaHoaDon = maHoaDon,
                        LoaiThanhToan = "THE",
                        SoTien = phaiThanhToan,
                        TrangThai = "THANH_CONG",
                        SoTheCuoi = string.IsNullOrWhiteSpace(req.SoTheCuoi) ? "9999" : req.SoTheCuoi.Trim(),
                        MaChuanChi = string.IsNullOrWhiteSpace(req.MaChuanChi) ? Guid.NewGuid().ToString()[..8].ToUpper() : req.MaChuanChi.Trim()
                    }, conn, trans);
                }
                else if (req.PhuongThuc == "QR")
                {
                    _banHangDao.AddThanhToan(new ThanhToan
                    {
                        MaHoaDon = maHoaDon,
                        LoaiThanhToan = "QR",
                        SoTien = phaiThanhToan,
                        TrangThai = "THANH_CONG",
                        MaGiaoDich = string.IsNullOrWhiteSpace(req.MaGiaoDichQR) ? $"VQR{DateTime.Now:yyyyMMddHHmmss}" : req.MaGiaoDichQR.Trim(),
                        LoaiVi = string.IsNullOrWhiteSpace(req.LoaiVi) ? "VietQR / Banking" : req.LoaiVi.Trim()
                    }, conn, trans);
                }

                // 7. Xử lý điểm tích lũy & điểm đổi của Khách hàng thân thiết
                int diemTichMoi = 0;
                if (req.MaKH.HasValue)
                {
                    // Trừ điểm đổi nếu có
                    if (req.DiemDoi > 0)
                    {
                        _khachHangDao.UpdatePoints(req.MaKH.Value, -req.DiemDoi, conn, trans);
                        _khachHangDao.RecordPointTransaction(new GiaoDichDiem
                        {
                            MaKH = req.MaKH.Value,
                            Loai = "DOI",
                            SoDiem = req.DiemDoi,
                            MaHoaDon = maHoaDon
                        }, conn, trans);
                    }

                    // Tích lũy điểm mới (Cứ 10.000 VNĐ doanh thu thực = 1 điểm)
                    diemTichMoi = (int)(phaiThanhToan / 10000m);
                    if (diemTichMoi > 0)
                    {
                        _khachHangDao.UpdatePoints(req.MaKH.Value, diemTichMoi, conn, trans);
                        _khachHangDao.RecordPointTransaction(new GiaoDichDiem
                        {
                            MaKH = req.MaKH.Value,
                            Loai = "TICH",
                            SoDiem = diemTichMoi,
                            MaHoaDon = maHoaDon
                        }, conn, trans);
                    }
                }

                // 8. Ghi nhật ký kiểm toán bất biến
                _nhatKyDao.InsertLog(new NhatKyKiemToan
                {
                    MaTK = currentMaTK,
                    HanhDong = "BAN_HANG_POS",
                    DoiTuong = "HoaDon",
                    MaDoiTuong = maHoaDon.ToString(),
                    DiaChiIP = ip,
                    ThietBi = userAgent,
                    GiaTriMoi = $"Hóa đơn hoàn tất. Tổng tiền: {tongTienHang:N0}đ, Giảm: {tongGiamGia:N0}đ, Thu: {phaiThanhToan:N0}đ, Hình thức: {req.PhuongThuc}"
                }, conn, trans);

                // Commit toàn bộ giao dịch ACID
                trans.Commit();

                // Lấy thông tin hóa đơn vừa tạo để lấy SoHoaDon chính xác (HD-xxxx)
                var createdHd = _banHangDao.GetHoaDonById(maHoaDon);

                return new PosCheckoutResult
                {
                    Success = true,
                    Message = "Thanh toán đơn hàng thành công!",
                    MaHoaDon = maHoaDon,
                    SoHoaDon = createdHd?.SoHoaDon ?? $"HD-{maHoaDon:D4}",
                    TongTien = tongTienHang,
                    GiamGia = tongGiamGia,
                    ThanhTien = phaiThanhToan,
                    TienKhachDua = tienKhachDua,
                    TienThoi = tienThoi,
                    DiemTichLuyMoi = diemTichMoi
                };
            }
            catch
            {
                trans.Rollback();
                throw;
            }
        }

        public HoaDon? GetInvoice(int maHoaDon)
        {
            return _banHangDao.GetHoaDonById(maHoaDon);
        }

        public List<HoaDon> GetInvoicesByCa(int maCa)
        {
            return _banHangDao.GetInvoicesByCa(maCa);
        }
    }
}
