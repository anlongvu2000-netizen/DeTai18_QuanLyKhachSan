using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Text.RegularExpressions;

namespace DeTai18_QuanLyKhachSan.DAL
{
    /// <summary>
    /// Interface chuẩn cho tầng truy cập dữ liệu (DAL).
    /// Minh họa kỹ thuật INTERFACE trong mô hình 3 lớp (cho phép hoán đổi giữa Mock In-Memory và SQL Server ADO.NET).
    /// </summary>
    public interface IRepository<T>
    {
        List<T> GetAll();
        bool Insert(T entity);
        bool Update(T entity);
        bool Delete(string id);
    }

    /// <summary>
    /// Lớp quản lý kết nối Cơ sở dữ liệu SQL Server bằng ADO.NET (SqlConnection).
    /// Tích hợp sẵn SQL Server LocalDB ((localdb)\MSSQLLocalDB) đã có sẵn trong máy cùng Visual Studio,
    /// kèm tính năng TỰ ĐỘNG KHỞI TẠO CSDL + BẢNG + STORED PROCEDURE từ file QuanLyKhachSanDB.sql!
    /// </summary>
    public static class DatabaseHelper
    {
        private static string _connectionString =
            ConfigurationManager.ConnectionStrings["QuanLyKhachSanDB"] != null
                ? ConfigurationManager.ConnectionStrings["QuanLyKhachSanDB"].ConnectionString
                : @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=QuanLyKhachSanDB;Integrated Security=True";

        /// <summary>
        /// Cho phép chuyển đổi linh hoạt giữa Chế độ 3 lớp Bộ nhớ (Chưa kết nối CSDL) và Chế độ ADO.NET SQL Server.
        /// </summary>
        public static bool UseInMemoryMode { get; set; } = false;

        public static string ConnectionString
        {
            get => _connectionString;
            set => _connectionString = value;
        }

        /// <summary>
        /// Khởi tạo đối tượng SqlConnection mới.
        /// </summary>
        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Kiểm tra kết nối tới SQL Server. Nếu CSDL QuanLyKhachSanDB chưa được tạo trên máy,
        /// tự động kết nối vào master và chạy script tạo CSDL + Bảng + Stored Procedure + Dữ liệu mẫu!
        /// </summary>
        public static bool TestSqlConnection(out string message)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    message = "Kết nối SQL Server (QuanLyKhachSanDB) thành công!";
                    return true;
                }
            }
            catch
            {
                // Thử tự động tạo CSDL QuanLyKhachSanDB từ file script QuanLyKhachSanDB.sql
                string createMsg;
                if (TuDongKhoiTaoCSDLTuScript(out createMsg))
                {
                    message = createMsg;
                    return true;
                }

                message = createMsg;
                return false;
            }
        }

        /// <summary>
        /// Tự động đọc file Database/QuanLyKhachSanDB.sql và thực thi trên SQL Server LocalDB / SQL Express.
        /// Giúp người dùng không cần phải cài đặt thêm SSMS hay chạy script thủ công.
        /// </summary>
        public static bool TuDongKhoiTaoCSDLTuScript(out string message)
        {
            try
            {
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_connectionString)
                {
                    InitialCatalog = "master",
                    ConnectTimeout = 10
                };

                string scriptPath = TimDuongDanFileSql();
                if (string.IsNullOrEmpty(scriptPath) || !File.Exists(scriptPath))
                {
                    message = "Không tìm thấy file Database\\QuanLyKhachSanDB.sql để tự động khởi tạo.";
                    return false;
                }

                string scriptContent = File.ReadAllText(scriptPath);
                string[] batches = Regex.Split(scriptContent, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

                using (SqlConnection masterConn = new SqlConnection(builder.ConnectionString))
                {
                    masterConn.Open();
                    foreach (string batch in batches)
                    {
                        string trimmed = batch.Trim();
                        if (string.IsNullOrWhiteSpace(trimmed)) continue;

                        using (SqlCommand cmd = new SqlCommand(trimmed, masterConn))
                        {
                            cmd.CommandTimeout = 30;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // Kiểm tra mở lại kết nối vào QuanLyKhachSanDB vừa tạo
                using (SqlConnection checkConn = new SqlConnection(_connectionString))
                {
                    checkConn.Open();
                }

                message = "Đã tự động khởi tạo CSDL QuanLyKhachSanDB (8 Bảng + 5 Stored Procedures + Dữ liệu mẫu) thành công!";
                return true;
            }
            catch (Exception ex)
            {
                message = "Chưa kết nối được SQL Server (" + ex.Message + "). Hệ thống sẵn sàng chạy ở chế độ Mô hình 3 lớp Bộ nhớ.";
                return false;
            }
        }

        private static string TimDuongDanFileSql()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates = new[]
            {
                Path.Combine(baseDir, "Database", "QuanLyKhachSanDB.sql"),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\Database\QuanLyKhachSanDB.sql")),
                Path.GetFullPath(Path.Combine(baseDir, @"..\..\..\Database\QuanLyKhachSanDB.sql"))
            };

            foreach (string path in candidates)
            {
                if (File.Exists(path)) return path;
            }
            return null;
        }
    }
}
