using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CuaHangTienLoi.Common
{
    /// <summary>
    /// Lớp tiện ích hỗ trợ truy xuất cơ sở dữ liệu Microsoft SQL Server an toàn, 
    /// chống SQL Injection và hỗ trợ quản lý giao dịch tường minh (ACID)
    /// </summary>
    public static class DbHelper
    {
        public static object ToDbValue(object? value)
        {
            return value ?? DBNull.Value;
        }

        public static string GetSafeString(SqlDataReader reader, string columnName, string defaultValue = "")
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetString(ordinal);
        }

        public static int GetSafeInt(SqlDataReader reader, string columnName, int defaultValue = 0)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt32(ordinal);
        }

        public static long GetSafeLong(SqlDataReader reader, string columnName, long defaultValue = 0)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetInt64(ordinal);
        }

        public static decimal GetSafeDecimal(SqlDataReader reader, string columnName, decimal defaultValue = 0m)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDecimal(ordinal);
        }

        public static DateTime GetSafeDateTime(SqlDataReader reader, string columnName, DateTime defaultValue = default)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetDateTime(ordinal);
        }

        public static DateTime? GetSafeNullableDateTime(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (DateTime?)null : reader.GetDateTime(ordinal);
        }

        public static int? GetSafeNullableInt(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (int?)null : reader.GetInt32(ordinal);
        }

        public static decimal? GetSafeNullableDecimal(SqlDataReader reader, string columnName)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? (decimal?)null : reader.GetDecimal(ordinal);
        }

        public static bool GetSafeBool(SqlDataReader reader, string columnName, bool defaultValue = false)
        {
            int ordinal = reader.GetOrdinal(columnName);
            return reader.IsDBNull(ordinal) ? defaultValue : reader.GetBoolean(ordinal);
        }
    }
}
