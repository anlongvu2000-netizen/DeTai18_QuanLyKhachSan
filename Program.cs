using System;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.GUI;

namespace DeTai18_QuanLyKhachSan
{
    internal static class Program
    {
        /// <summary>
        /// Điểm khởi đầu của ứng dụng Quản lý Khách sạn (Đề tài 18).
        /// Mở Form Đăng nhập trước, nếu đăng nhập hợp lệ mới mở Form chính đa cửa sổ (frmMain).
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using (frmDangNhap loginForm = new frmDangNhap())
            {
                if (loginForm.ShowDialog() == DialogResult.OK)
                {
                    Application.Run(new frmMain());
                }
            }
        }
    }
}
