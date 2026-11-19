using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using CuaHangTienLoi.DataAccess;
using CuaHangTienLoi.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký Controllers & Views
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// 2. Cấu hình Quản lý phiên làm việc (Session) có thời hạn 60 phút
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("AppSettings:SessionTimeoutMinutes", 60));
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".CuaHangTienLoi.Session";
});

// 3. Đăng ký Lớp truy cập dữ liệu (Data Access Objects - DAO)
builder.Services.AddScoped<TaiKhoanDao>();
builder.Services.AddScoped<NhanVienDao>();
builder.Services.AddScoped<HangHoaDao>();
builder.Services.AddScoped<KhoDao>();
builder.Services.AddScoped<CaDao>();
builder.Services.AddScoped<BanHangDao>();
builder.Services.AddScoped<KhachHangDao>();
builder.Services.AddScoped<KhuyenMaiDao>();
builder.Services.AddScoped<DoiTraDao>();
builder.Services.AddScoped<NhatKyDao>();
builder.Services.AddScoped<BaoCaoDao>();

// 4. Đăng ký Lớp Dịch vụ & Logic nghiệp vụ (Service / Business Logic Layer)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFefoService, FefoService>();
builder.Services.AddScoped<IPosService, PosService>();
builder.Services.AddScoped<IKhoService, KhoService>();
builder.Services.AddScoped<ICaService, CaService>();
builder.Services.AddScoped<IDoiTraService, DoiTraService>();
builder.Services.AddScoped<IExcelExportService, ExcelExportService>();

var app = builder.Build();

// 5. Cấu hình HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Bắt buộc kích hoạt Session trước khi xử lý MVC Controllers
app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 6. Tự động kiểm tra và đảm bảo dữ liệu hiển thị tiếng Việt chuẩn UTF-8
var defaultConn = app.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(defaultConn))
{
    CuaHangTienLoi.Common.DbSeeder.EnsureCleanData(defaultConn);
}

if (args.Length > 0 && args[0] == "--seed")
{
    Console.WriteLine("Hoàn tất nạp dữ liệu qua cờ --seed.");
    return;
}

app.Run();
