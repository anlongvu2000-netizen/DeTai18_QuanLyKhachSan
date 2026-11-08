using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.BUS;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.GUI
{
    /// <summary>
    /// Form Nghiệp vụ trung tâm: Nhận Phòng (Check-In), Gọi Dịch Vụ, Thanh Toán Trả Phòng (Check-Out) & Báo Cáo Doanh Thu.
    /// Minh họa các kỹ thuật quan trọng nhất:
    /// 1. MASTER - DETAIL giữa Danh sách Phiếu thuê (Master) và Chi tiết dịch vụ phòng đã gọi (Detail)
    /// 2. GIAO DỊCH (SqlTransaction - BeginTransaction, Commit, Rollback) cho cả 3 nghiệp vụ:
    ///    - Nhận phòng (Thêm PhieuThuePhong + Đổi trạng thái Phong -> Đang thuê)
    ///    - Gọi dịch vụ (Thêm ChiTietSuDungDichVu + Trừ tồn kho DichVu)
    ///    - Thanh toán trả phòng (Tạo HoaDon + Đóng PhieuThuePhong + Giải phóng Phong -> Trống + Cộng điểm KhachHang)
    /// 3. ĐA HÌNH (Polymorphism): Tính giá phòng tự động theo Ngày / Giờ (ChinhSachGiaThue) và ưu đãi VIP (NguoiBase).
    /// </summary>
    public class frmNhanTraPhongTransaction : Form
    {
        private readonly NghiepVuKhachSanBUS _nghiepVuBus = new NghiepVuKhachSanBUS();
        private readonly PhongBUS _phongBus = new PhongBUS();
        private readonly KhachHangBUS _khBus = new KhachHangBUS();
        private readonly DichVuBUS _dvBus = new DichVuBUS();

        public event Action<string> OnTransactionCompleted;

        private string _maPhongChonTruoc;
        private PhieuThuePhong _phieuDangChon;
        private KetQuaTinhTienHoaDon _hoaDonTamTinhHienTai;

        // Khu vực 1: Nhận phòng mới (Check-in Transaction)
        private ComboBox cboKhachHangCheckIn;
        private ComboBox cboPhongTrongCheckIn;
        private ComboBox cboHinhThucThue;
        private DateTimePicker dtpNgayTraDuKien;
        private TextBox txtTienDatCoc;

        // Khu vực 2: Master-Detail Phiếu thuê & Dịch vụ
        private DataGridView dgvPhieuThueMaster;
        private DataGridView dgvChiTietDichVuDetail;
        private ComboBox cboDichVuOrder;
        private NumericUpDown numSoLuongDV;

        // Khu vực 3: Bảng tính tiền Đa hình & Check-out Transaction
        private Label lblThongTinPhieuChon;
        private Label lblDienGiaiDaHinh;
        private Label lblTienPhong;
        private Label lblPhuThu;
        private Label lblTienDichVu;
        private Label lblGiamGiaVIP;
        private Label lblTienCocTru;
        private Label lblTongThanhToan;
        private TextBox txtGhiChuHoaDon;
        private Button btnThanhToanTransaction;

        // Tab 2: Báo cáo doanh thu (sp_ThongKeDoanhThu)
        private DateTimePicker dtpTuNgay;
        private DateTimePicker dtpDenNgay;
        private DataGridView dgvBaoCaoDoanhThu;
        private Label lblTongDoanhThuBaoCao;

        public frmNhanTraPhongTransaction(string maPhongChonTruoc = null)
        {
            _maPhongChonTruoc = maPhongChonTruoc;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Nhận Phòng - Gọi Dịch Vụ - Thanh Toán Hóa Đơn (Master-Detail, SqlTransaction & Đa Hình)";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(245, 248, 252);

            TabControl tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F)
            };

            TabPage tabNghiepVu = new TabPage("1. Nhận Phòng, Gọi Dịch Vụ & Thanh Toán (Master-Detail & SqlTransaction)");
            TabPage tabBaoCao = new TabPage("2. Thống Kê Hóa Đơn & Doanh Thu (Stored Procedure sp_ThongKeDoanhThu)");

            XayDungTabNghiepVu(tabNghiepVu);
            XayDungTabBaoCao(tabBaoCao);

            tabControl.TabPages.Add(tabNghiepVu);
            tabControl.TabPages.Add(tabBaoCao);

            this.Controls.Add(tabControl);
            this.Load += (s, e) => TaiLaiDuLieu(_maPhongChonTruoc);
        }

        private void XayDungTabNghiepVu(TabPage tab)
        {
            // Panel Top: Nhận phòng mới (Check-in Transaction)
            GroupBox grpCheckIn = new GroupBox
            {
                Text = "NGHIỆP VỤ 1: NHẬN PHÒNG MỚI - CHECK-IN (SqlTransaction: Tạo Phiếu thuê + Đổi trạng thái Phòng -> Đang thuê)",
                Dock = DockStyle.Top,
                Height = 90,
                BackColor = Color.White
            };

            Label c1 = new Label { Text = "Khách hàng:", Location = new Point(14, 34), AutoSize = true };
            cboKhachHangCheckIn = new ComboBox
            {
                Location = new Point(98, 31),
                Width = 215,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label c2 = new Label { Text = "Phòng trống:", Location = new Point(325, 34), AutoSize = true };
            cboPhongTrongCheckIn = new ComboBox
            {
                Location = new Point(412, 31),
                Width = 175,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label c3 = new Label { Text = "Hình thức:", Location = new Point(598, 34), AutoSize = true };
            cboHinhThucThue = new ComboBox
            {
                Location = new Point(668, 31),
                Width = 105,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboHinhThucThue.Items.AddRange(new object[] { "Theo ngày", "Theo giờ" });
            cboHinhThucThue.SelectedIndex = 0;

            Label c4 = new Label { Text = "Trả dự kiến:", Location = new Point(782, 34), AutoSize = true };
            dtpNgayTraDuKien = new DateTimePicker
            {
                Location = new Point(862, 31),
                Width = 115,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddDays(1)
            };

            Label c5 = new Label { Text = "Tiền cọc:", Location = new Point(985, 34), AutoSize = true };
            txtTienDatCoc = new TextBox { Location = new Point(1048, 31), Width = 85, Text = "200000" };

            Button btnCheckIn = new Button
            {
                Text = "Nhận Phòng (Transaction)",
                Location = new Point(1142, 28),
                Size = new Size(185, 34),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnCheckIn.Click += BtnCheckIn_Click;

            grpCheckIn.Controls.AddRange(new Control[]
            {
                c1, cboKhachHangCheckIn, c2, cboPhongTrongCheckIn, c3, cboHinhThucThue,
                c4, dtpNgayTraDuKien, c5, txtTienDatCoc, btnCheckIn
            });

            // Panel Right: Bảng tính tiền Đa hình & Thanh toán Trả phòng (Check-out Transaction)
            GroupBox grpCheckout = new GroupBox
            {
                Text = "NGHIỆP VỤ 3: TÍNH TIỀN ĐA HÌNH & TRẢ PHÒNG (SqlTransaction)",
                Dock = DockStyle.Right,
                Width = 435,
                BackColor = Color.FromArgb(250, 252, 255)
            };

            lblThongTinPhieuChon = new Label
            {
                Text = "Chọn 1 Phiếu thuê bên trái để xem chi tiết hóa đơn",
                Location = new Point(16, 28),
                Size = new Size(400, 26),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 58, 108)
            };

            lblDienGiaiDaHinh = new Label
            {
                Text = "Chính sách giá Đa hình: ...",
                Location = new Point(16, 56),
                Size = new Size(400, 40),
                BackColor = Color.FromArgb(232, 240, 252),
                ForeColor = Color.FromArgb(20, 60, 120),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter
            };

            lblTienPhong = new Label { Text = "1. Tiền phòng chính: 0 VNĐ", Location = new Point(18, 110), AutoSize = true };
            lblPhuThu = new Label { Text = "2. Phụ thu (Trả muộn / Giờ): 0 VNĐ", Location = new Point(18, 140), AutoSize = true };
            lblTienDichVu = new Label { Text = "3. Tổng tiền dịch vụ (ITinhTien): 0 VNĐ", Location = new Point(18, 170), AutoSize = true };
            lblGiamGiaVIP = new Label
            {
                Text = "4. Giảm giá Đa hình (VIP/Thường): -0 VNĐ",
                Location = new Point(18, 200),
                AutoSize = true,
                ForeColor = Color.FromArgb(25, 115, 65),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            lblTienCocTru = new Label { Text = "5. Trừ tiền đặt cọc ban đầu: -0 VNĐ", Location = new Point(18, 230), AutoSize = true };

            lblTongThanhToan = new Label
            {
                Text = "TỔNG CẦN THANH TOÁN: 0 VNĐ",
                Location = new Point(16, 265),
                Size = new Size(400, 38),
                BackColor = Color.FromArgb(255, 242, 215),
                ForeColor = Color.FromArgb(185, 35, 35),
                Font = new Font("Segoe UI", 11.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblGhiChu = new Label { Text = "Ghi chú hóa đơn:", Location = new Point(16, 315), AutoSize = true };
            txtGhiChuHoaDon = new TextBox { Location = new Point(16, 338), Width = 400 };

            btnThanhToanTransaction = new Button
            {
                Text = "XÁC NHẬN THANH TOÁN & TRẢ PHÒNG (SqlTransaction)",
                Location = new Point(16, 378),
                Size = new Size(400, 42),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };
            btnThanhToanTransaction.Click += BtnThanhToanTransaction_Click;

            grpCheckout.Controls.AddRange(new Control[]
            {
                lblThongTinPhieuChon, lblDienGiaiDaHinh,
                lblTienPhong, lblPhuThu, lblTienDichVu, lblGiamGiaVIP, lblTienCocTru,
                lblTongThanhToan, lblGhiChu, txtGhiChuHoaDon, btnThanhToanTransaction
            });

            // Panel Center: SplitContainer Master (Phiếu thuê) - Detail (Chi tiết dịch vụ)
            SplitContainer splitMasterDetail = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 240
            };

            GroupBox grpMasterPhieu = new GroupBox
            {
                Text = "BẢNG MASTER: Danh sách Phiếu Thuê Phòng (Click chọn 1 phiếu để nạp Chi tiết dịch vụ & Tính tiền)",
                Dock = DockStyle.Fill
            };
            dgvPhieuThueMaster = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dgvPhieuThueMaster.SelectionChanged += DgvPhieuThueMaster_SelectionChanged;
            grpMasterPhieu.Controls.Add(dgvPhieuThueMaster);

            GroupBox grpDetailDichVu = new GroupBox
            {
                Text = "BẢNG DETAIL: Chi tiết Dịch Vụ của Phiếu đang chọn & NGHIỆP VỤ 2: GỌI DỊCH VỤ TRỪ KHO (SqlTransaction)",
                Dock = DockStyle.Fill
            };

            Panel pnlOrderBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 46,
                BackColor = Color.FromArgb(236, 243, 252)
            };
            Label lblChonDV = new Label { Text = "Chọn dịch vụ:", Location = new Point(12, 13), AutoSize = true };
            cboDichVuOrder = new ComboBox
            {
                Location = new Point(102, 10),
                Width = 310,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            Label lblSL = new Label { Text = "Số lượng:", Location = new Point(425, 13), AutoSize = true };
            numSoLuongDV = new NumericUpDown
            {
                Location = new Point(495, 10),
                Width = 75,
                Minimum = 1,
                Maximum = 100,
                Value = 1
            };
            Button btnGoiDichVu = new Button
            {
                Text = "Thêm Dịch Vụ Vào Phòng (Transaction Trừ Kho)",
                Location = new Point(585, 8),
                Size = new Size(310, 30),
                BackColor = Color.FromArgb(200, 95, 20),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            btnGoiDichVu.Click += BtnGoiDichVu_Click;
            pnlOrderBar.Controls.AddRange(new Control[] { lblChonDV, cboDichVuOrder, lblSL, numSoLuongDV, btnGoiDichVu });

            dgvChiTietDichVuDetail = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            grpDetailDichVu.Controls.Add(dgvChiTietDichVuDetail);
            grpDetailDichVu.Controls.Add(pnlOrderBar);

            splitMasterDetail.Panel1.Controls.Add(grpMasterPhieu);
            splitMasterDetail.Panel2.Controls.Add(grpDetailDichVu);

            tab.Controls.Add(splitMasterDetail);
            tab.Controls.Add(grpCheckout);
            tab.Controls.Add(grpCheckIn);
        }

        private void XayDungTabBaoCao(TabPage tab)
        {
            Panel pnlFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.White,
                Padding = new Padding(12)
            };

            Label b1 = new Label { Text = "Từ ngày:", Location = new Point(15, 18), AutoSize = true };
            dtpTuNgay = new DateTimePicker
            {
                Location = new Point(80, 15),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };

            Label b2 = new Label { Text = "Đến ngày:", Location = new Point(225, 18), AutoSize = true };
            dtpDenNgay = new DateTimePicker
            {
                Location = new Point(300, 15),
                Width = 130,
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddDays(1)
            };

            Button btnXemBaoCao = new Button
            {
                Text = "Thống Kê Doanh Thu (sp_ThongKeDoanhThu)",
                Location = new Point(445, 12),
                Size = new Size(290, 32),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnXemBaoCao.Click += (s, e) => NapBaoCaoDoanhThu();

            lblTongDoanhThuBaoCao = new Label
            {
                Text = "Tổng doanh thu: 0 VNĐ",
                Location = new Point(755, 17),
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 115, 65)
            };

            pnlFilter.Controls.AddRange(new Control[] { b1, dtpTuNgay, b2, dtpDenNgay, btnXemBaoCao, lblTongDoanhThuBaoCao });

            dgvBaoCaoDoanhThu = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };

            tab.Controls.Add(dgvBaoCaoDoanhThu);
            tab.Controls.Add(pnlFilter);
        }

        public void TaiLaiDuLieu(string maPhongUuTien)
        {
            if (!string.IsNullOrWhiteSpace(maPhongUuTien))
            {
                _maPhongChonTruoc = maPhongUuTien;
            }

            // 1. Nạp danh sách khách hàng
            List<KhachHang> dsKH = _khBus.GetAll();
            cboKhachHangCheckIn.DataSource = dsKH;
            cboKhachHangCheckIn.DisplayMember = "HoTen";
            cboKhachHangCheckIn.ValueMember = "MaKH";

            // 2. Nạp danh sách phòng đang Trống để Check-in
            List<Phong> dsPhongTrong = _phongBus.TimKiemPhong(0, "", "Trống");
            cboPhongTrongCheckIn.DataSource = dsPhongTrong;
            cboPhongTrongCheckIn.DisplayMember = "TenPhong";
            cboPhongTrongCheckIn.ValueMember = "MaPhong";

            if (!string.IsNullOrWhiteSpace(_maPhongChonTruoc))
            {
                if (dsPhongTrong.Any(x => string.Equals(x.MaPhong, _maPhongChonTruoc, StringComparison.OrdinalIgnoreCase)))
                {
                    cboPhongTrongCheckIn.SelectedValue = _maPhongChonTruoc;
                }
            }

            // 3. Nạp danh sách dịch vụ để gọi món
            List<DichVu> dsDV = _dvBus.GetAll();
            cboDichVuOrder.DataSource = dsDV;
            cboDichVuOrder.DisplayMember = "TenDV";
            cboDichVuOrder.ValueMember = "MaDV";

            // 4. Nạp danh sách Phiếu thuê (Master)
            List<PhieuThuePhong> dsPhieu = _nghiepVuBus.GetDanhSachPhieuThue("");
            dgvPhieuThueMaster.DataSource = dsPhieu;
            if (dgvPhieuThueMaster.Columns["TienDatCoc"] != null)
                dgvPhieuThueMaster.Columns["TienDatCoc"].DefaultCellStyle.Format = "N0";

            // Nếu người dùng click từ Sơ đồ phòng vào 1 phòng đang thuê -> Tự động chọn phiếu thuê của phòng đó
            if (!string.IsNullOrWhiteSpace(_maPhongChonTruoc))
            {
                for (int i = 0; i < dgvPhieuThueMaster.Rows.Count; i++)
                {
                    if (dgvPhieuThueMaster.Rows[i].DataBoundItem is PhieuThuePhong pt &&
                        string.Equals(pt.MaPhong, _maPhongChonTruoc, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(pt.TrangThaiPhieu, "Đang thuê", StringComparison.OrdinalIgnoreCase))
                    {
                        dgvPhieuThueMaster.ClearSelection();
                        dgvPhieuThueMaster.Rows[i].Selected = true;
                        dgvPhieuThueMaster.CurrentCell = dgvPhieuThueMaster.Rows[i].Cells[0];
                        break;
                    }
                }
            }

            NapBaoCaoDoanhThu();
        }

        private void DgvPhieuThueMaster_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvPhieuThueMaster.CurrentRow == null) return;
            _phieuDangChon = dgvPhieuThueMaster.CurrentRow.DataBoundItem as PhieuThuePhong;
            if (_phieuDangChon == null) return;

            // Nạp Detail: Danh sách dịch vụ của Phiếu thuê đang chọn
            List<ChiTietSuDungDichVu> dsChiTiet = _nghiepVuBus.GetChiTietDichVu(_phieuDangChon.MaPhieuThue);
            dgvChiTietDichVuDetail.DataSource = dsChiTiet;

            if (dgvChiTietDichVuDetail.Columns["DonGiaLucGoi"] != null)
                dgvChiTietDichVuDetail.Columns["DonGiaLucGoi"].DefaultCellStyle.Format = "N0";
            if (dgvChiTietDichVuDetail.Columns["ThanhTien"] != null)
                dgvChiTietDichVuDetail.Columns["ThanhTien"].DefaultCellStyle.Format = "N0";

            // Tính toán Hóa đơn tạm tính bằng Đa hình
            DateTime thoiDiemTinh = _phieuDangChon.NgayTraThucTe ?? DateTime.Now;
            _hoaDonTamTinhHienTai = _nghiepVuBus.TinhToanHoaDonTamTinh(_phieuDangChon, thoiDiemTinh);

            lblThongTinPhieuChon.Text = string.Format("Phiếu #{0} - {1} ({2}) - [{3}]",
                _phieuDangChon.MaPhieuThue, _phieuDangChon.TenPhong, _phieuDangChon.TenKhachHang, _phieuDangChon.TrangThaiPhieu);
            lblDienGiaiDaHinh.Text = _hoaDonTamTinhHienTai.DienGiaiChiTiet;
            lblTienPhong.Text = string.Format("1. Tiền phòng chính: {0:N0} VNĐ", _hoaDonTamTinhHienTai.TienPhong);
            lblPhuThu.Text = string.Format("2. Phụ thu (Trả muộn / Giờ): {0:N0} VNĐ", _hoaDonTamTinhHienTai.PhuThu);
            lblTienDichVu.Text = string.Format("3. Tổng tiền dịch vụ (ITinhTien): {0:N0} VNĐ", _hoaDonTamTinhHienTai.TienDichVu);
            lblGiamGiaVIP.Text = string.Format("4. Giảm giá Đa hình ({0:0}%): -{1:N0} VNĐ",
                _hoaDonTamTinhHienTai.TyLeGiamGia * 100m, _hoaDonTamTinhHienTai.SoTienGiamGia);
            lblTienCocTru.Text = string.Format("5. Trừ tiền đặt cọc ban đầu: -{0:N0} VNĐ", _hoaDonTamTinhHienTai.TienDatCoc);
            lblTongThanhToan.Text = string.Format("TỔNG CẦN THANH TOÁN: {0:N0} VNĐ", _hoaDonTamTinhHienTai.TongCanThanhToan);

            bool dangThue = string.Equals(_phieuDangChon.TrangThaiPhieu, "Đang thuê", StringComparison.OrdinalIgnoreCase);
            btnThanhToanTransaction.Enabled = dangThue;
            btnThanhToanTransaction.Text = dangThue
                ? "XÁC NHẬN THANH TOÁN & TRẢ PHÒNG (SqlTransaction)"
                : "PHIẾU NÀY ĐÃ THANH TOÁN HOÀN TẤT";
        }

        private void BtnCheckIn_Click(object sender, EventArgs e)
        {
            if (cboKhachHangCheckIn.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn khách hàng.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cboPhongTrongCheckIn.SelectedValue == null)
            {
                MessageBox.Show("Hiện không có phòng trống được chọn!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(txtTienDatCoc.Text, out decimal tienCoc))
            {
                tienCoc = 0m;
            }

            try
            {
                string maKH = cboKhachHangCheckIn.SelectedValue.ToString();
                string maPhong = cboPhongTrongCheckIn.SelectedValue.ToString();
                string hinhThuc = cboHinhThucThue.SelectedItem.ToString();

                int newMaPhieu = _nghiepVuBus.NhanPhong(
                    maKH,
                    maPhong,
                    DateTime.Now,
                    dtpNgayTraDuKien.Value,
                    hinhThuc,
                    tienCoc
                );

                _maPhongChonTruoc = maPhong;
                TaiLaiDuLieu(maPhong);
                OnTransactionCompleted?.Invoke(string.Format("Giao dịch Nhận phòng thành công: Phòng {0} (Mã phiếu #{1}).", maPhong, newMaPhieu));
                MessageBox.Show(
                    string.Format("Nhận phòng thành công!\n- Mã phiếu thuê: #{0}\n- Phòng {1} đã tự động chuyển sang trạng thái [Đang thuê].", newMaPhieu, maPhong),
                    "SqlTransaction Commit",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giao dịch Nhận phòng đã Rollback do lỗi:\n" + ex.Message,
                    "SqlTransaction Rollback", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnGoiDichVu_Click(object sender, EventArgs e)
        {
            if (_phieuDangChon == null)
            {
                MessageBox.Show("Vui lòng chọn 1 phiếu thuê đang hoạt động ở bảng Master.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!string.Equals(_phieuDangChon.TrangThaiPhieu, "Đang thuê", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Chỉ có thể gọi thêm dịch vụ cho phiếu đang ở trạng thái [Đang thuê]!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (cboDichVuOrder.SelectedValue == null) return;

            try
            {
                string maDV = cboDichVuOrder.SelectedValue.ToString();
                int soLuong = (int)numSoLuongDV.Value;

                _nghiepVuBus.GoiDichVuChoPhieuThue(_phieuDangChon.MaPhieuThue, maDV, soLuong);
                DgvPhieuThueMaster_SelectionChanged(null, EventArgs.Empty);
                OnTransactionCompleted?.Invoke(string.Format("Đã thêm dịch vụ vào Phiếu #{0} và trừ kho thành công.", _phieuDangChon.MaPhieuThue));
                MessageBox.Show("Đã thêm dịch vụ vào phòng và cập nhật tồn kho (Transaction Commit)!",
                    "Gọi dịch vụ thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giao dịch Gọi dịch vụ đã Rollback:\n" + ex.Message,
                    "SqlTransaction Rollback", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnThanhToanTransaction_Click(object sender, EventArgs e)
        {
            if (_phieuDangChon == null || _hoaDonTamTinhHienTai == null) return;

            DialogResult confirm = MessageBox.Show(
                string.Format("Xác nhận Thanh toán & Trả phòng cho {0} ({1})?\n\nTổng số tiền cần thu: {2:N0} VNĐ",
                    _phieuDangChon.TenPhong, _phieuDangChon.TenKhachHang, _hoaDonTamTinhHienTai.TongCanThanhToan),
                "Xác nhận Giao dịch Thanh toán (SqlTransaction)",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            try
            {
                int maHD = _nghiepVuBus.ThanhToanVaTraPhong(_phieuDangChon, _hoaDonTamTinhHienTai, txtGhiChuHoaDon.Text.Trim());
                string tenPhong = _phieuDangChon.TenPhong;
                TaiLaiDuLieu(null);
                OnTransactionCompleted?.Invoke(string.Format("Đã thanh toán Hóa đơn #{0} - Giải phóng {1} về trạng thái [Trống].", maHD, tenPhong));

                MessageBox.Show(
                    string.Format("Thanh toán hoàn tất (SqlTransaction Commit)!\n" +
                                  "- Mã hóa đơn: #{0}\n" +
                                  "- {1} đã chuyển về trạng thái [Trống]\n" +
                                  "- Khách hàng được cộng +10 điểm tích lũy!", maHD, tenPhong),
                    "Hoàn tất Trả phòng",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Giao dịch Thanh toán đã Rollback:\n" + ex.Message,
                    "SqlTransaction Rollback", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void NapBaoCaoDoanhThu()
        {
            DataTable dt = _nghiepVuBus.GetBaoCaoDoanhThu(dtpTuNgay.Value, dtpDenNgay.Value);
            dgvBaoCaoDoanhThu.DataSource = dt;

            decimal tong = 0m;
            foreach (DataRow row in dt.Rows)
            {
                tong += Convert.ToDecimal(row["TongThanhToan"]);
            }
            lblTongDoanhThuBaoCao.Text = string.Format("Tổng doanh thu ({0} hóa đơn): {1:N0} VNĐ", dt.Rows.Count, tong);
        }
    }
}
