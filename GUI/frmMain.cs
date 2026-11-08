using System;
using System.Drawing;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.BUS;
using DeTai18_QuanLyKhachSan.DAL;

namespace DeTai18_QuanLyKhachSan.GUI
{
    /// <summary>
    /// Form chính đa cửa sổ (MDI Parent) liên kết toàn bộ các phân hệ của phần mềm Quản lý khách sạn.
    /// Minh họa: Ứng dụng đa Form (MDI), truyền sự kiện đồng bộ dữ liệu giữa các Form con.
    /// </summary>
    public class frmMain : Form
    {
        private ToolStripStatusLabel lblUserStatus;
        private ToolStripStatusLabel lblModeStatus;
        private ToolStripStatusLabel lblActionStatus;
        private ToolStripStatusLabel lblClock;
        private Timer timerClock;

        public frmMain()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "ĐỀ TÀI 18: PHẦN MỀM QUẢN LÝ KHÁCH SẠN - KIẾN TRÚC 3 LỚP & ADO.NET";
            this.WindowState = FormWindowState.Maximized;
            this.MinimumSize = new Size(1180, 740);
            this.IsMdiContainer = true;
            this.Font = new Font("Segoe UI", 9.5F);

            // 1. MenuStrip
            MenuStrip menuStrip = new MenuStrip { BackColor = Color.FromArgb(20, 50, 95), ForeColor = Color.White };
            menuStrip.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);

            ToolStripMenuItem mnuHeThong = new ToolStripMenuItem("1. Hệ thống") { ForeColor = Color.White };
            ToolStripMenuItem mnuChuyenCheDo = new ToolStripMenuItem("Chuyển đổi chế độ SQL Server / Bộ nhớ");
            mnuChuyenCheDo.Click += MnuChuyenCheDo_Click;
            ToolStripMenuItem mnuResetMau = new ToolStripMenuItem("Khôi phục dữ liệu mẫu ban đầu (Bộ nhớ)");
            mnuResetMau.Click += (s, e) =>
            {
                InMemoryStore.ResetSampleData();
                BroadcastRefresh("Đã khôi phục dữ liệu mẫu trong bộ nhớ.");
            };
            ToolStripMenuItem mnuThoat = new ToolStripMenuItem("Thoát chương trình");
            mnuThoat.Click += (s, e) => this.Close();
            mnuHeThong.DropDownItems.AddRange(new ToolStripItem[] { mnuChuyenCheDo, mnuResetMau, new ToolStripSeparator(), mnuThoat });

            ToolStripMenuItem mnuSoDoPhong = new ToolStripMenuItem("2. Sơ đồ Phòng (TreeView & ListView)") { ForeColor = Color.White };
            mnuSoDoPhong.Click += (s, e) => MoFormSoDoPhong();

            ToolStripMenuItem mnuPhongMasterDetail = new ToolStripMenuItem("3. Loại Phòng & Phòng (Master-Detail DataSet)") { ForeColor = Color.White };
            mnuPhongMasterDetail.Click += (s, e) => MoFormMasterDetailPhong();

            ToolStripMenuItem mnuKhachVaDichVu = new ToolStripMenuItem("4. Khách hàng & Dịch vụ (DataTable/DataView & StoredProc)") { ForeColor = Color.White };
            mnuKhachVaDichVu.Click += (s, e) => MoFormKhachHangVaDichVu();

            ToolStripMenuItem mnuNhanTraPhong = new ToolStripMenuItem("5. Nhận/Trả phòng & Hóa đơn (Transaction & Đa hình)") { ForeColor = Color.White };
            mnuNhanTraPhong.Click += (s, e) => MoFormNhanTraPhong(null);

            menuStrip.Items.AddRange(new ToolStripItem[]
            {
                mnuHeThong,
                mnuSoDoPhong,
                mnuPhongMasterDetail,
                mnuKhachVaDichVu,
                mnuNhanTraPhong
            });

            // 2. StatusStrip
            StatusStrip statusStrip = new StatusStrip();
            lblUserStatus = new ToolStripStatusLabel
            {
                Text = "Nhân viên: " + PhienDangNhap.TenHienThi,
                ForeColor = Color.FromArgb(20, 70, 140),
                BorderSides = ToolStripStatusLabelBorderSides.Right
            };
            lblModeStatus = new ToolStripStatusLabel
            {
                BorderSides = ToolStripStatusLabelBorderSides.Right
            };
            lblActionStatus = new ToolStripStatusLabel
            {
                Spring = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Sẵn sàng."
            };
            lblClock = new ToolStripStatusLabel();

            statusStrip.Items.AddRange(new ToolStripItem[] { lblUserStatus, lblModeStatus, lblActionStatus, lblClock });

            timerClock = new Timer { Interval = 1000 };
            timerClock.Tick += (s, e) => lblClock.Text = string.Format("Thời gian: {0:dd/MM/yyyy HH:mm:ss}", DateTime.Now);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
            this.Controls.Add(statusStrip);

            this.Load += FrmMain_Load;
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            CapNhatTrangThaiCheDo();
            lblClock.Text = string.Format("Thời gian: {0:dd/MM/yyyy HH:mm:ss}", DateTime.Now);
            timerClock.Start();

            // Mở mặc định Sơ đồ phòng khi vào chương trình
            MoFormSoDoPhong();
        }

        private void CapNhatTrangThaiCheDo()
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                lblModeStatus.Text = "Chế độ: 3 Lớp Bộ Nhớ (In-Memory)";
                lblModeStatus.ForeColor = Color.DarkOrange;
            }
            else
            {
                lblModeStatus.Text = "Chế độ: 3 Lớp ADO.NET (SQL Server)";
                lblModeStatus.ForeColor = Color.DarkGreen;
            }
        }

        private void MnuChuyenCheDo_Click(object sender, EventArgs e)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                string msg;
                if (DatabaseHelper.TestSqlConnection(out msg))
                {
                    DatabaseHelper.UseInMemoryMode = false;
                    CapNhatTrangThaiCheDo();
                    BroadcastRefresh("Đã chuyển sang chế độ kết nối SQL Server (ADO.NET).");
                }
                else
                {
                    MessageBox.Show(msg, "Chưa thể kết nối SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else
            {
                DatabaseHelper.UseInMemoryMode = true;
                CapNhatTrangThaiCheDo();
                BroadcastRefresh("Đã chuyển sang chế độ Mô hình 3 lớp Bộ nhớ (In-Memory).");
            }
        }

        public void MoFormSoDoPhong()
        {
            foreach (Form f in this.MdiChildren)
            {
                if (f is frmSoDoPhong existing)
                {
                    existing.Activate();
                    existing.TaiLaiDuLieu();
                    return;
                }
            }

            frmSoDoPhong frm = new frmSoDoPhong();
            frm.MdiParent = this;
            frm.OnStatusMessage += msg => lblActionStatus.Text = msg;
            frm.OnOpenBookingForRoom += maPhong => MoFormNhanTraPhong(maPhong);
            frm.Show();
        }

        public void MoFormMasterDetailPhong()
        {
            foreach (Form f in this.MdiChildren)
            {
                if (f is frmQuanLyPhongMasterDetail existing)
                {
                    existing.Activate();
                    existing.TaiLaiDuLieu();
                    return;
                }
            }

            frmQuanLyPhongMasterDetail frm = new frmQuanLyPhongMasterDetail();
            frm.MdiParent = this;
            frm.OnDataChanged += msg => BroadcastRefresh(msg);
            frm.Show();
        }

        public void MoFormKhachHangVaDichVu()
        {
            foreach (Form f in this.MdiChildren)
            {
                if (f is frmKhachHangVaDichVu existing)
                {
                    existing.Activate();
                    existing.TaiLaiDuLieu();
                    return;
                }
            }

            frmKhachHangVaDichVu frm = new frmKhachHangVaDichVu();
            frm.MdiParent = this;
            frm.OnDataChanged += msg => BroadcastRefresh(msg);
            frm.Show();
        }

        public void MoFormNhanTraPhong(string maPhongChonTruoc)
        {
            foreach (Form f in this.MdiChildren)
            {
                if (f is frmNhanTraPhongTransaction existing)
                {
                    existing.Activate();
                    existing.TaiLaiDuLieu(maPhongChonTruoc);
                    return;
                }
            }

            frmNhanTraPhongTransaction frm = new frmNhanTraPhongTransaction(maPhongChonTruoc);
            frm.MdiParent = this;
            frm.OnTransactionCompleted += msg => BroadcastRefresh(msg);
            frm.Show();
        }

        /// <summary>
        /// Khi 1 Form con thay đổi dữ liệu (Nhận phòng, Trả phòng, Thêm phòng/khách/dịch vụ),
        /// tự động thông báo cho tất cả các Form con khác đang mở cập nhật lại giao diện!
        /// </summary>
        private void BroadcastRefresh(string statusMsg)
        {
            lblActionStatus.Text = statusMsg;
            foreach (Form f in this.MdiChildren)
            {
                if (f is frmSoDoPhong sdp) sdp.TaiLaiDuLieu();
                else if (f is frmQuanLyPhongMasterDetail md) md.TaiLaiDuLieu();
                else if (f is frmKhachHangVaDichVu khdv) khdv.TaiLaiDuLieu();
                else if (f is frmNhanTraPhongTransaction nt) nt.TaiLaiDuLieu(null);
            }
        }
    }
}
