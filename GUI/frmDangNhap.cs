using System;
using System.Drawing;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.BUS;
using DeTai18_QuanLyKhachSan.DAL;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.GUI
{
    /// <summary>
    /// Form Đăng nhập hệ thống & Cấu hình chế độ hoạt động (3 Lớp Bộ nhớ vs 3 Lớp ADO.NET SQL Server).
    /// Minh họa: Điều khiển cơ bản, Xử lý sự kiện, Kiểm tra dữ liệu bằng ErrorProvider.
    /// </summary>
    public class frmDangNhap : Form
    {
        private readonly NghiepVuKhachSanBUS _bus = new NghiepVuKhachSanBUS();
        private TextBox txtTaiKhoan;
        private TextBox txtMatKhau;
        private TextBox txtConnectionString;
        private RadioButton radSqlServer;
        private RadioButton radInMemory;
        private CheckBox chkHienMatKhau;
        private Button btnDangNhap;
        private Button btnKiemTraKetNoi;
        private Button btnThoat;
        private Label lblTrangThaiKetNoi;
        private ErrorProvider errorProvider;

        public frmDangNhap()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Đăng Nhập Hệ Thống - Quản Lý Khách Sạn (.NET)";
            this.Size = new Size(540, 430);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(245, 248, 252);
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);

            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            Panel pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 68,
                BackColor = Color.FromArgb(24, 58, 108)
            };
            Label lblTitle = new Label
            {
                Text = "PHẦN MỀM QUẢN LÝ KHÁCH SẠN (ĐỀ TÀI 18)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            pnlHeader.Controls.Add(lblTitle);

            GroupBox grpThongTin = new GroupBox
            {
                Text = "Thông tin tài khoản (Mẫu: admin/123 hoặc letan/123)",
                Location = new Point(20, 80),
                Size = new Size(485, 130)
            };

            Label lblUser = new Label { Text = "Tài khoản:", Location = new Point(20, 32), AutoSize = true };
            txtTaiKhoan = new TextBox { Location = new Point(120, 29), Width = 330, Text = "admin" };

            Label lblPass = new Label { Text = "Mật khẩu:", Location = new Point(20, 68), AutoSize = true };
            txtMatKhau = new TextBox { Location = new Point(120, 65), Width = 210, Text = "123", UseSystemPasswordChar = true };

            chkHienMatKhau = new CheckBox
            {
                Text = "Hiện mật khẩu",
                Location = new Point(340, 67),
                AutoSize = true
            };
            chkHienMatKhau.CheckedChanged += (s, e) => txtMatKhau.UseSystemPasswordChar = !chkHienMatKhau.Checked;

            grpThongTin.Controls.AddRange(new Control[] { lblUser, txtTaiKhoan, lblPass, txtMatKhau, chkHienMatKhau });

            GroupBox grpCheDo = new GroupBox
            {
                Text = "Chế độ Tầng DAL (Mô hình 3 lớp)",
                Location = new Point(20, 220),
                Size = new Size(485, 115)
            };

            radSqlServer = new RadioButton
            {
                Text = "Kết nối CSDL SQL Server qua ADO.NET (SqlConnection)",
                Location = new Point(18, 25),
                AutoSize = true,
                Checked = true
            };
            radInMemory = new RadioButton
            {
                Text = "Mô hình 3 lớp chưa kết nối CSDL (Dữ liệu mẫu trong bộ nhớ)",
                Location = new Point(18, 52),
                AutoSize = true
            };

            txtConnectionString = new TextBox
            {
                Location = new Point(18, 80),
                Width = 345,
                Text = DatabaseHelper.ConnectionString,
                Font = new Font("Consolas", 8.5F)
            };

            btnKiemTraKetNoi = new Button
            {
                Text = "Kiểm tra SQL",
                Location = new Point(370, 77),
                Size = new Size(98, 26),
                BackColor = Color.FromArgb(230, 238, 250)
            };
            btnKiemTraKetNoi.Click += BtnKiemTraKetNoi_Click;

            grpCheDo.Controls.AddRange(new Control[] { radSqlServer, radInMemory, txtConnectionString, btnKiemTraKetNoi });

            lblTrangThaiKetNoi = new Label
            {
                Text = "Mẹo: Nếu máy chưa bật SQL Server, hệ thống sẽ tự động chuyển sang chế độ 3 lớp Bộ nhớ.",
                Location = new Point(22, 340),
                Size = new Size(480, 20),
                ForeColor = Color.FromArgb(60, 90, 140),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };

            btnDangNhap = new Button
            {
                Text = "Đăng nhập hệ thống",
                Location = new Point(155, 362),
                Size = new Size(165, 32),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnDangNhap.Click += BtnDangNhap_Click;

            btnThoat = new Button
            {
                Text = "Thoát",
                Location = new Point(335, 362),
                Size = new Size(95, 32)
            };
            btnThoat.Click += (s, e) => this.Close();

            this.AcceptButton = btnDangNhap;
            this.Controls.AddRange(new Control[] { pnlHeader, grpThongTin, grpCheDo, lblTrangThaiKetNoi, btnDangNhap, btnThoat });
        }

        private void BtnKiemTraKetNoi_Click(object sender, EventArgs e)
        {
            DatabaseHelper.ConnectionString = txtConnectionString.Text.Trim();
            string msg;
            if (DatabaseHelper.TestSqlConnection(out msg))
            {
                lblTrangThaiKetNoi.ForeColor = Color.Green;
                lblTrangThaiKetNoi.Text = msg;
                MessageBox.Show(msg, "Kết nối ADO.NET thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                lblTrangThaiKetNoi.ForeColor = Color.DarkOrange;
                lblTrangThaiKetNoi.Text = "Chưa kết nối được SQL Server -> Có thể chọn chế độ 3 lớp Bộ nhớ.";
                MessageBox.Show(msg, "Thông báo kết nối", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnDangNhap_Click(object sender, EventArgs e)
        {
            errorProvider.Clear();
            bool valid = true;

            if (string.IsNullOrWhiteSpace(txtTaiKhoan.Text))
            {
                errorProvider.SetError(txtTaiKhoan, "Vui lòng nhập tài khoản!");
                valid = false;
            }
            if (string.IsNullOrWhiteSpace(txtMatKhau.Text))
            {
                errorProvider.SetError(txtMatKhau, "Vui lòng nhập mật khẩu!");
                valid = false;
            }
            if (!valid) return;

            DatabaseHelper.ConnectionString = txtConnectionString.Text.Trim();
            if (radInMemory.Checked)
            {
                DatabaseHelper.UseInMemoryMode = true;
            }
            else
            {
                string sqlMsg;
                if (DatabaseHelper.TestSqlConnection(out sqlMsg))
                {
                    DatabaseHelper.UseInMemoryMode = false;
                }
                else
                {
                    DialogResult dr = MessageBox.Show(
                        sqlMsg + "\n\nBạn có muốn chuyển sang chạy ở chế độ [Mô hình 3 lớp Bộ nhớ] ngay bây giờ không?",
                        "Tự động chuyển chế độ 3 lớp",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (dr == DialogResult.Yes)
                    {
                        DatabaseHelper.UseInMemoryMode = true;
                        radInMemory.Checked = true;
                    }
                    else
                    {
                        return;
                    }
                }
            }

            try
            {
                NhanVien nv = _bus.KiemTraDangNhap(txtTaiKhoan.Text, txtMatKhau.Text);
                if (nv == null)
                {
                    MessageBox.Show("Sai tài khoản hoặc mật khẩu! (Thử: admin / 123 hoặc letan / 123)",
                        "Đăng nhập thất bại", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                PhienDangNhap.NhanVienHienTai = nv;
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi khi đăng nhập: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
