using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace CuaHangTienLoi.DataAccess
{
    /// <summary>
    /// Lớp DAO cơ sở cung cấp kết nối CSDL và quản lý Transaction cho các lớp DAO con
    /// </summary>
    public abstract class BaseDao
    {
        protected readonly string _connectionString;

        public BaseDao(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") 
                ?? throw new InvalidOperationException("Chuỗi kết nối 'DefaultConnection' không tìm thấy trong cấu hình.");
        }

        public BaseDao(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqlConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        protected SqlConnection OpenConnection()
        {
            var conn = CreateConnection();
            conn.Open();
            return conn;
        }
    }
}
