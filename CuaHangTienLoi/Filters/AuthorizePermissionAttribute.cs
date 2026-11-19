using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using CuaHangTienLoi.Common;
using CuaHangTienLoi.Models;

namespace CuaHangTienLoi.Filters
{
    /// <summary>
    /// Action Filter kiểm soát phân quyền dựa trên Vai trò & Quyền (RBAC) thông qua Session
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizePermissionAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string? _requiredPermission;
        private readonly string? _requiredRole;

        public AuthorizePermissionAttribute(string? permission = null, string? role = null)
        {
            _requiredPermission = permission;
            _requiredRole = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var session = context.HttpContext.Session;
            var user = session.GetObject<UserSessionModel>("CurrentUser");

            // 1. Kiểm tra đăng nhập
            if (user == null)
            {
                if (IsAjaxRequest(context.HttpContext.Request))
                {
                    context.Result = new JsonResult(new { success = false, message = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại.", redirectUrl = "/Auth/Login" })
                    {
                        StatusCode = 401
                    };
                }
                else
                {
                    var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
                    context.Result = new RedirectToActionResult("Login", "Auth", new { returnUrl });
                }
                return;
            }

            // 2. Quản lý (QUAN_LY) có toàn quyền
            if (user.MaVaiTro == "QUAN_LY")
            {
                return;
            }

            // 3. Kiểm tra vai trò yêu cầu
            if (!string.IsNullOrEmpty(_requiredRole) && user.MaVaiTro != _requiredRole)
            {
                ForbidUser(context);
                return;
            }

            // 4. Kiểm tra quyền chi tiết (Quyen)
            if (!string.IsNullOrEmpty(_requiredPermission))
            {
                bool hasPermission = user.QuyenList != null && user.QuyenList.Contains(_requiredPermission);
                if (!hasPermission)
                {
                    ForbidUser(context);
                    return;
                }
            }
        }

        private static void ForbidUser(AuthorizationFilterContext context)
        {
            if (IsAjaxRequest(context.HttpContext.Request))
            {
                context.Result = new JsonResult(new { success = false, message = "Bạn không có quyền thực hiện thao tác này." })
                {
                    StatusCode = 403
                };
            }
            else
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Auth", null);
            }
        }

        private static bool IsAjaxRequest(Microsoft.AspNetCore.Http.HttpRequest request)
        {
            return request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                   (request.Headers.Accept.ToString().Contains("application/json", StringComparison.OrdinalIgnoreCase));
        }
    }
}
