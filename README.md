# HỆ THỐNG QUẢN LÝ KHO VÀ CỬA HÀNG TIỆN LỢI (CuaHangTienLoi)
> **Ứng dụng Web Full-Stack ASP.NET Core MVC chuyên nghiệp, bảo mật cao và sẵn sàng đưa vào triển khai thực tế.**

---

## 1. TỔNG QUAN HỆ THỐNG & KIẾN TRÚC PHÂN TẦNG

Hệ thống được thiết kế và xây dựng theo chuẩn mực kiến trúc 3 tầng phân tách độc lập (Separation of Concerns), đáp ứng đầy đủ các tiêu chuẩn bảo mật doanh nghiệp, giao dịch ACID toàn vẹn và tối ưu hóa trải nghiệm người dùng:

```
[ Giao diện người dùng: Razor Views (.cshtml) + HTML5 + CSS3 + Bootstrap 5 + ES6 JavaScript ]
                                      │ (HTTP / JSON / AJAX)
                                      ▼
[ Bộ điều phối (Controller Layer) & Action Filters kiểm soát Phân quyền Vai trò RBAC ]
                                      │ (DTOs / ViewModels)
                                      ▼
[ Lớp Dịch vụ & Logic Nghiệp Vụ (Service Layer): Thuật toán FEFO, POS, Ca, Đổi trả, Excel ]
                                      │ (Strongly-typed Entities / ACID Transactions)
                                      ▼
[ Lớp Truy cập Dữ liệu (DAO Layer): ADO.NET Parameterized Queries, Connection Pooling ]
                                      │ (T-SQL với Row-level Locking UPDLOCK)
                                      ▼
[ Cơ sở dữ liệu: Microsoft SQL Server (Triggers, Sequences, Filtered Unique Indexes, Constraints) ]
```

---

## 2. CÔNG NGHỆ SỬ DỤNG

- **Backend:** C# .NET 10 / ASP.NET Core MVC
- **Database:** Microsoft SQL Server (ACID, Sequence `seq_HoaDon` & `seq_PhieuTra`, Trigger bất biến `TR_NhatKy_ChiThem`, Conditional Filtered Index `UX_Ca_QuayDangMo`, `IX_LoHang_FEFO`)
- **Data Access:** ADO.NET (`Microsoft.Data.SqlClient`) với `SqlTransaction` tường minh, khóa dòng `UPDLOCK, ROWLOCK` chống tranh chấp dữ liệu (Race Condition).
- **Bảo mật & Xác thực:**
  - Phiên làm việc (Session) bảo mật cao (HttpOnly, Secure, SameSite, Timeout 60 phút).
  - Băm mật khẩu chuẩn **Bcrypt** (`Org.BouncyCastle.Crypto.Generators.OpenBsdBCrypt`) kèm muối ngẫu nhiên 16 bytes và cost factor 10.
  - Tự động khóa tài khoản sau 5 lần đăng nhập sai liên tiếp (FR01).
  - Phân quyền động theo vai trò (RBAC): Quản lý (`QUAN_LY`), Thủ kho (`NHAN_VIEN_KHO`), Thu ngân (`THU_NGAN`).
  - Nhật ký kiểm toán bất biến (**Audit Trail**) được khóa cứng bằng SQL Trigger `INSTEAD OF UPDATE, DELETE` (chỉ thêm, cấm sửa và cấm xóa).
- **Xử lý tệp & Báo cáo:** Thư viện **EPPlus** xuất/nhập Excel (.xlsx) định dạng chuẩn doanh nghiệp; in hóa đơn nhiệt K80 và in báo cáo ca.
- **Web Server & Triển khai:** Nginx Reverse Proxy hỗ trợ HTTPS, SSL/TLS, Gzip, Rate Limiting (20 req/s), Security Headers.
- **Sao lưu tự động:** Script PowerShell `Backup-Database.ps1` và Stored Procedure `sp_SaoLuuCSDL_HangNgay` chạy theo lịch định kỳ hàng ngày (NFR03).

---

## 3. CẤU TRÚC THƯ MỤC DỰ ÁN

```
d:\TH_PTTK_HDT\Báo cáo\Code\
├── CuaHangTienLoi.sln                 # File Solution mở bằng Visual Studio / dotnet CLI
├── Database\
│   ├── 01_CreateDatabase.sql          # Script DDL tạo Database, Sequence, Trigger, Bảng, Index
│   ├── 02_SeedData.sql                # Dữ liệu mẫu hoàn chỉnh: Tài khoản Bcrypt, Hàng hóa, Lô FEFO, v.v.
│   └── 03_DailyBackup.sql             # Thủ tục sao lưu CSDL hàng ngày theo lịch (NFR03)
├── Deploy\
│   ├── nginx.conf                     # File cấu hình Nginx Reverse Proxy (HTTPS, Gzip, SSL, Rate Limit)
│   └── Backup-Database.ps1            # Script tự động hóa sao lưu chạy với Windows Task Scheduler
└── CuaHangTienLoi\
    ├── CuaHangTienLoi.csproj          # File cấu hình dự án ASP.NET Core MVC (.NET 10)
    ├── Program.cs                     # Khởi tạo DI, Middleware, Session, Routing
    ├── appsettings.json               # Cấu hình Chuỗi kết nối SQL Server, Session Timeout
    ├── Common\
    │   ├── PasswordHasher.cs          # Mã hóa băm Bcrypt Salted Hash
    │   ├── DbHelper.cs                # Đọc dữ liệu an toàn & chống SQL Injection
    │   └── SessionExtensions.cs       # Mở rộng Session lưu trữ Object JSON
    ├── Filters\
    │   └── AuthorizePermissionAttribute.cs # Bộ lọc phân quyền RBAC dựa trên Quyen & VaiTro
    ├── Models\
    │   ├── Entities.cs                # Các Entity khớp 100% với cấu trúc bảng CSDL
    │   └── ViewModels.cs              # DTOs giỏ hàng POS, Mở/Đóng ca, Kiểm kê, Báo cáo
    ├── DataAccess\                    # Lớp DAO (Data Access Objects)
    │   ├── BaseDao.cs
    │   ├── TaiKhoanDao.cs
    │   ├── NhanVienDao.cs
    │   ├── HangHoaDao.cs
    │   ├── KhoDao.cs
    │   ├── CaDao.cs
    │   ├── BanHangDao.cs
    │   ├── KhachHangDao.cs
    │   ├── KhuyenMaiDao.cs
    │   ├── DoiTraDao.cs
    │   ├── NhatKyDao.cs
    │   └── BaoCaoDao.cs
    ├── Services\                      # Lớp Logic Nghiệp Vụ (Business Logic Layer)
    │   ├── IAuthService.cs & AuthService.cs
    │   ├── IFefoService.cs & FefoService.cs (Thuật toán FEFO cốt lõi)
    │   ├── IPosService.cs & PosService.cs (Xử lý giỏ hàng, xác nhận tuổi, thanh toán)
    │   ├── IKhoService.cs & KhoService.cs (Nhập kho, xuất quầy, kiểm kê, thẻ kho)
    │   ├── ICaService.cs & CaService.cs (Mở/đóng ca, đối chiếu chênh lệch tiền mặt)
    │   ├── IDoiTraService.cs & DoiTraService.cs (Quy trình đổi trả & Quản lý duyệt)
    │   └── IExcelExportService.cs & ExcelExportService.cs (Xử lý Excel bằng EPPlus)
    ├── Controllers\
    │   ├── AuthController.cs
    │   ├── PosController.cs
    │   ├── CaController.cs
    │   ├── KhoController.cs
    │   ├── AdminController.cs
    │   └── BaoCaoController.cs
    ├── Views\
    │   ├── Shared\ (_Layout.cshtml, _LayoutPOS.cshtml, _NotificationPartial.cshtml)
    │   ├── Auth\ (Login.cshtml, AccessDenied.cshtml, DoiMatKhau.cshtml)
    │   ├── Pos\ (Index.cshtml - Màn hình POS tương tác, InHoaDon.cshtml - In nhiệt K80)
    │   ├── Ca\ (Index.cshtml, MoCa.cshtml, DongCa.cshtml, BaoCaoCa.cshtml)
    │   ├── Kho\ (Index.cshtml, NhapKho.cshtml, XuatKho.cshtml, KiemKe.cshtml, TheKho.cshtml)
    │   ├── Admin\ (Dashboard, NhanVien, HangHoa, KhuyenMai, KhachHang, DoiTra, NhatKy)
    │   └── BaoCao\ (Index.cshtml)
    └── wwwroot\
        ├── css\ (site.css, pos.css)
        └── js\ (site.js, pos.js - Engine POS hoàn chỉnh)
```

---

## 4. HƯỚNG DẪN CÀI ĐẶT & CHẠY ỨNG DỤNG

### Bước 1: Khởi tạo Cơ sở Dữ liệu SQL Server
1. Mở **SQL Server Management Studio (SSMS)** hoặc **Azure Data Studio**.
2. Mở và thực thi toàn bộ nội dung file:
   `Database\01_CreateDatabase.sql` (Tạo database `CuaHangTienLoi`, các bảng, trigger, sequence và chỉ mục).
3. Mở và thực thi file:
   `Database\02_SeedData.sql` (Nạp dữ liệu mẫu ban đầu: nhân viên, tài khoản, quầy, hàng hóa, lô hàng, v.v.).

### Bước 2: Cấu hình chuỗi kết nối
Mở file `CuaHangTienLoi\appsettings.json`, chỉnh sửa chuỗi kết nối phù hợp với máy của bạn (nếu dùng tài khoản `sa` hoặc Windows Authentication):
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost;Database=CuaHangTienLoi;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;"
}
```

### Bước 3: Biên dịch và chạy ứng dụng
Mở terminal tại thư mục chứa mã nguồn:
```bash
# Biên dịch dự án
dotnet build CuaHangTienLoi.sln

# Chạy ứng dụng web
dotnet run --project CuaHangTienLoi\CuaHangTienLoi.csproj
```
Truy cập trình duyệt tại địa chỉ: `http://localhost:5000` (hoặc cổng hiển thị trên console).

---

## 5. TÀI KHOẢN MẪU DÙNG THỬ (DEMO)

Giao diện đăng nhập hỗ trợ các nút **Click để điền nhanh** 3 nhóm tài khoản mẫu:

| Vai trò | Tên đăng nhập | Mật khẩu mặc định | Thẩm quyền chính |
| :--- | :--- | :--- | :--- |
| **Quản lý** | `admin` | `Admin@123` | Toàn quyền quản trị, duyệt xuất hủy, duyệt kiểm kê, duyệt đổi trả, sửa giá, xem nhật ký kiểm toán, xem báo cáo doanh thu. |
| **Thủ kho** | `kho01` | `Kho@123` | Nhập kho, xuất hàng lên quầy (FEFO), lập phiếu kiểm kê, tra cứu sổ thẻ kho và cảnh báo hạn dùng. |
| **Thu ngân** | `thungan01` | `ThuNgan@123` | Mở/Đóng ca làm việc tại quầy, bán hàng POS, quét mã vạch, xác nhận độ tuổi 18+, lập yêu cầu đổi trả. |
| **Thu ngân 2**| `thungan02` | `ThuNgan@123` | Bán hàng ca tiếp theo tại Quầy 02. |

---

## 6. CHI TIẾT CÁC PHÂN HỆ NGHIỆP VỤ CỐT LÕI

### Phân hệ 1: Xác thực, Phân quyền RBAC & Nhật ký kiểm toán bất biến (M1)
- **Tự động khóa tài khoản sau 5 lần sai (FR01):** Cột `SoLanNhapSai` trong bảng `TaiKhoan` đếm số lần nhập sai. Khi đạt ngưỡng 5 lần, trường `TrangThai` tự động chuyển về `0` (khóa). Quản lý có thể mở khóa trong màn hình *Quản lý Nhân viên*.
- **Phân quyền vai trò (RBAC):** Bộ lọc `AuthorizePermissionAttribute` kiểm tra quyền theo cấu trúc `VaiTro -> VaiTro_Quyen -> Quyen`, bảo vệ chặt chẽ các controller/action.
- **Nhật ký kiểm toán bất biến (Audit Trail):** Mọi hành vi đăng nhập sai, mở/đóng ca, thanh toán POS, sửa giá bán, duyệt trả hàng đều được ghi vào bảng `NhatKyKiemToan`. Trigger `TR_NhatKy_ChiThem` ngăn chặn mọi hành vi can thiệp `UPDATE` hoặc `DELETE`.

### Phân hệ 2: Bán hàng tại quầy POS & Đổi trả (M4, M6)
- **Màn hình POS tốc độ cao:** Tối ưu hóa cho màn hình cảm ứng, máy quét mã vạch và bàn phím (Phím tắt: `F2` tìm kiếm/quét mã, `F4` tìm khách hàng, `F8` tiền mặt, `F9` VietQR, `F10` quẹt thẻ, `Esc` xóa giỏ).
- **Cảnh báo & Modal xác nhận độ tuổi (18+):** Khi quét sản phẩm có `GioiHanDoTuoi >= 18` (ví dụ: Bia Tiger, Bia Heineken, Thuốc lá Craven A), hệ thống hiển thị biểu tượng 18+ nổi bật. Khi thanh toán, modal cảnh báo bắt buộc thu ngân phải kiểm tra giấy tờ tùy thân của khách và xác nhận mới cho phép hoàn tất giao dịch.
- **Tự động cấp số hóa đơn qua Sequence:** Câu lệnh `NEXT VALUE FOR dbo.seq_HoaDon` sinh số thứ tự liên tục, định dạng tự động thành `HD-0001`, `HD-0002` qua computed column `PERSISTED`.
- **Thanh toán đa phương thức:** Hỗ trợ tính tiền thối lại tự động khi trả tiền mặt; nhập mã chuẩn chi khi cà thẻ; sinh mã VietQR động theo đúng số tiền cần thanh toán.
- **Khách hàng thân thiết & Đổi điểm:** Tích lũy 1 điểm cho mỗi 10.000đ chi tiêu; đổi điểm trừ trực tiếp vào tiền phải thanh toán (1 điểm = 1.000đ).
- **In hóa đơn bán hàng K80:** Định dạng chuẩn khổ giấy in nhiệt 80mm, hiển thị số lô và hạn dùng của từng sản phẩm.
- **Quy trình Đổi/Trả hàng:** Thu ngân lập yêu cầu trả (`CHO_DUYET`), Quản lý kiểm tra và duyệt (`DA_DUYET`), hệ thống cấp số phiếu từ `dbo.seq_PhieuTra` (`PT-xxxx`), hoàn tiền cho khách, cộng lại tồn quầy cho hàng còn dùng được và trừ điểm tích lũy của khách hàng.

### Phân hệ 3: Quản lý Kho & Thuật toán FEFO (M3)
- **Thuật toán FEFO (First Expired, First Out):**
  - Khi bán hàng trên POS hoặc xuất kho bổ sung lên quầy, hàm `AllocateLotsForSale` và `AllocateLotsForWarehouseTransfer` tự động truy vấn chỉ mục điều kiện `IX_LoHang_FEFO (MaHang, HanSuDung WHERE SlTonQuay > 0)`.
  - Hàng hóa trong lô có ngày hết hạn sớm nhất sẽ luôn được ưu tiên xuất trước. Khi lô đó hết hàng, thuật toán tự động cắt sang lô tiếp theo có hạn dùng kế tiếp!
- **Nhập kho:** Mỗi dòng sản phẩm nhập vào sẽ khởi tạo 1 Lô hàng mới (`LoHang`) với Số lô, HSD và số lượng tồn ban đầu, đồng thời ghi nhận vào Sổ Thẻ Kho (`TheKho`).
- **Xuất hủy hàng hỏng/hết hạn:** Lập phiếu xuất hủy dạng chờ duyệt (`CHO_DUYET`). Quản lý phê duyệt mới thực hiện trừ tồn kho/quầy và ghi Thẻ kho (`LoaiGiaoDich = 'XUAT_HUY'`).
- **Kiểm kê kho & quầy:** Lập bảng kiểm đếm thực tế, tính toán chênh lệch tự động (`ChenhLech = SlThucTe - SlHeThong`). Khi Quản lý duyệt, tồn kho được điều chỉnh chính xác và ghi sổ thẻ kho.
- **Sổ Thẻ kho (TheKho):** Cho phép lọc và truy vết toàn bộ lịch sử xuất-nhập-tồn của từng mặt hàng và lô hàng.

### Phân hệ 4: Quản lý Ca làm việc & Tiền mặt (M5)
- **Quy tắc mỗi quầy chỉ mở duy nhất 1 ca:** Được bảo vệ cả ở tầng ứng dụng và tầng CSDL thông qua chỉ mục duy nhất có điều kiện:
  `CREATE UNIQUE INDEX UX_Ca_QuayDangMo ON dbo.Ca(MaQuay) WHERE TrangThai = 'MO'`
- **Đóng ca & Đối chiếu chênh lệch tiền mặt:**
  - Hệ thống tự động tính toán: `TienMatHeThong = TienDauCa + DoanhThuTienMat`.
  - Thu ngân đếm tiền mặt thực tế tại quầy và nhập vào.
  - Hệ thống tính chênh lệch `ChenhLech = TienMatThucTe - TienMatHeThong`.
  - Nếu có chênh lệch (`ChenhLech != 0`), hệ thống **bắt buộc thu ngân/quản lý phải nhập giải trình lý do**.
  - Tự động kết chuyển và tạo bản ghi Báo cáo ca (`BaoCaoCa`).
  - Hỗ trợ in Biên bản Báo cáo ca chuẩn A4/K80 có chữ ký bàn giao của Thu ngân và Quản lý.

---

## 7. TRIỂN KHAI NGINX REVERSE PROXY & SAO LƯU ĐỊNH KỲ

### Cấu hình Nginx (`Deploy/nginx.conf`)
- Chuyển hướng toàn bộ cổng 80 (HTTP) sang cổng 443 (HTTPS).
- Kích hoạt giao thức TLSv1.2, TLSv1.3 với chuẩn mã hóa cao.
- Thiết lập Rate Limiting giới hạn 20 request/giây/IP để chống tấn công DoS/Brute-force.
- Nén Gzip các tệp CSS, JS, SVG, JSON để tăng tốc độ tải trang.
- Chuyển tiếp cổng upstream về Kestrel (`127.0.0.1:5000`).

### Tác vụ sao lưu định kỳ hàng ngày (`Deploy/Backup-Database.ps1` & `Database/03_DailyBackup.sql`)
- Tạo bản sao lưu đầy đủ (`BACKUP DATABASE ... WITH COMPRESSION, CHECKSUM`).
- Kiểm tra tính toàn vẹn bản sao lưu ngay sau khi backup (`RESTORE VERIFYONLY`).
- Tự động xóa các bản sao lưu cũ vượt quá 30 ngày (Retention Policy).
- Tích hợp vào **Windows Task Scheduler** chạy tự động lúc 01:00 AM mỗi ngày.
