using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.BUS;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.GUI
{
    /// <summary>
    /// Form Quản lý Khách hàng & Dịch vụ khách sạn.
    /// Minh họa các kỹ thuật trong đề cương:
    /// 1. Dữ liệu ngắt kết nối (Disconnected Mode): SqlDataAdapter, DataTable và lọc tức thì bằng DataView.RowFilter
    /// 2. Thực thi CRUD bằng Stored Procedure (sp_ThemKhachHang, sp_CapNhatKhachHang, sp_XoaKhachHang)
    /// 3. Kế thừa & Đa hình OOP: Lớp trừu tượng NguoiBase -> KhachHang -> KhachHangVIP (Tự động tính % ưu đãi đa hình).
    /// </summary>
    public class frmKhachHangVaDichVu : Form
    {
        private readonly KhachHangBUS _khBus = new KhachHangBUS();
        private readonly DichVuBUS _dvBus = new DichVuBUS();
        public event Action<string> OnDataChanged;

        private DataTable _dtKhachHang;
        private DataView _dvKhachHang;
        private DataGridView dgvKhachHang;

        private TextBox txtMaKH;
        private TextBox txtHoTen;
        private TextBox txtCCCD;
        private TextBox txtSDT;
        private TextBox txtDiaChi;
        private ComboBox cboLoaiKhach;
        private NumericUpDown numDiemTichLuy;
        private Label lblMinhHoaDaHinh;
        private TextBox txtTimKhachDisconnected;
        private ComboBox cboLocLoaiKhachDisconnected;

        // Tab 2: Dịch vụ
        private DataTable _dtDichVu;
        private DataView _dvDichVu;
        private DataGridView dgvDichVu;
        private TextBox txtMaDV;
        private TextBox txtTenDV;
        private ComboBox cboDanhMucDV;
        private TextBox txtDonViTinh;
        private TextBox txtDonGiaDV;
        private NumericUpDown numSoLuongTon;
        private TextBox txtTimDichVuDisconnected;

        public frmKhachHangVaDichVu()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Quản Lý Khách Hàng & Dịch Vụ (DataAdapter, DataTable, DataView, Stored Procedure & Đa Hình OOP)";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(245, 248, 252);

            TabControl tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5F)
            };

            TabPage tabKhachHang = new TabPage("1. Quản Lý Khách Hàng (Stored Procedure & Đa Hình VIP/Thường)");
            TabPage tabDichVu = new TabPage("2. Quản Lý Dịch Vụ & Kho Minibar (DataAdapter & DataView)");

            XayDungTabKhachHang(tabKhachHang);
            XayDungTabDichVu(tabDichVu);

            tabMain.TabPages.Add(tabKhachHang);
            tabMain.TabPages.Add(tabDichVu);

            this.Controls.Add(tabMain);
            this.Load += (s, e) => TaiLaiDuLieu();
        }

        private void XayDungTabKhachHang(TabPage tab)
        {
            Panel pnlTopFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            Label lblTim = new Label
            {
                Text = "Lọc ngắt kết nối (DataView.RowFilter - Không truy vấn lại SQL):",
                Location = new Point(12, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 58, 108)
            };
            txtTimKhachDisconnected = new TextBox { Location = new Point(405, 13), Width = 240 };
            txtTimKhachDisconnected.TextChanged += (s, e) => LocDataViewKhachHang();

            Label lblLocLoai = new Label { Text = "Hạng khách:", Location = new Point(660, 16), AutoSize = true };
            cboLocLoaiKhachDisconnected = new ComboBox
            {
                Location = new Point(745, 13),
                Width = 140,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLocLoaiKhachDisconnected.Items.AddRange(new object[] { "Tất cả", "Thường", "VIP" });
            cboLocLoaiKhachDisconnected.SelectedIndex = 0;
            cboLocLoaiKhachDisconnected.SelectedIndexChanged += (s, e) => LocDataViewKhachHang();

            pnlTopFilter.Controls.AddRange(new Control[] { lblTim, txtTimKhachDisconnected, lblLocLoai, cboLocLoaiKhachDisconnected });

            GroupBox grpEdit = new GroupBox
            {
                Text = "Thông tin Khách hàng (Gọi Stored Procedure & Đa hình NguoiBase)",
                Dock = DockStyle.Right,
                Width = 430
            };

            Label k1 = new Label { Text = "Mã KH:", Location = new Point(16, 30), AutoSize = true };
            txtMaKH = new TextBox { Location = new Point(120, 27), Width = 120 };

            Label k2 = new Label { Text = "Hạng khách:", Location = new Point(250, 30), AutoSize = true };
            cboLoaiKhach = new ComboBox
            {
                Location = new Point(330, 27),
                Width = 82,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboLoaiKhach.Items.AddRange(new object[] { "Thường", "VIP" });
            cboLoaiKhach.SelectedIndex = 0;
            cboLoaiKhach.SelectedIndexChanged += (s, e) => CapNhatNhanDaHinh();

            Label k3 = new Label { Text = "Họ và tên:", Location = new Point(16, 64), AutoSize = true };
            txtHoTen = new TextBox { Location = new Point(120, 61), Width = 292 };

            Label k4 = new Label { Text = "Số CCCD:", Location = new Point(16, 98), AutoSize = true };
            txtCCCD = new TextBox { Location = new Point(120, 95), Width = 292 };
            txtCCCD.KeyPress += ChiChoNhapSo_KeyPress;

            Label k5 = new Label { Text = "Điện thoại:", Location = new Point(16, 132), AutoSize = true };
            txtSDT = new TextBox { Location = new Point(120, 129), Width = 292 };
            txtSDT.KeyPress += ChiChoNhapSo_KeyPress;

            Label k6 = new Label { Text = "Địa chỉ:", Location = new Point(16, 166), AutoSize = true };
            txtDiaChi = new TextBox { Location = new Point(120, 163), Width = 292 };

            Label k7 = new Label { Text = "Điểm tích lũy:", Location = new Point(16, 200), AutoSize = true };
            numDiemTichLuy = new NumericUpDown
            {
                Location = new Point(120, 197),
                Width = 120,
                Minimum = 0,
                Maximum = 100000,
                Value = 0
            };
            numDiemTichLuy.ValueChanged += (s, e) => CapNhatNhanDaHinh();

            lblMinhHoaDaHinh = new Label
            {
                Location = new Point(16, 235),
                Size = new Size(396, 48),
                BackColor = Color.FromArgb(232, 244, 236),
                ForeColor = Color.FromArgb(20, 95, 50),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleCenter
            };

            Button btnThemKH = new Button
            {
                Text = "Thêm (SP)",
                Location = new Point(16, 298),
                Size = new Size(94, 34),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThemKH.Click += BtnThemKH_Click;

            Button btnSuaKH = new Button
            {
                Text = "Sửa (SP)",
                Location = new Point(116, 298),
                Size = new Size(94, 34),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSuaKH.Click += BtnSuaKH_Click;

            Button btnXoaKH = new Button
            {
                Text = "Xóa (SP)",
                Location = new Point(216, 298),
                Size = new Size(94, 34),
                BackColor = Color.FromArgb(185, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnXoaKH.Click += BtnXoaKH_Click;

            Button btnMoiKH = new Button
            {
                Text = "Làm mới",
                Location = new Point(316, 298),
                Size = new Size(94, 34)
            };
            btnMoiKH.Click += (s, e) =>
            {
                txtMaKH.Clear();
                txtHoTen.Clear();
                txtCCCD.Clear();
                txtSDT.Clear();
                txtDiaChi.Clear();
                cboLoaiKhach.SelectedIndex = 0;
                numDiemTichLuy.Value = 0;
                txtMaKH.Focus();
            };

            grpEdit.Controls.AddRange(new Control[]
            {
                k1, txtMaKH, k2, cboLoaiKhach, k3, txtHoTen, k4, txtCCCD, k5, txtSDT, k6, txtDiaChi, k7, numDiemTichLuy,
                lblMinhHoaDaHinh, btnThemKH, btnSuaKH, btnXoaKH, btnMoiKH
            });

            GroupBox grpGrid = new GroupBox
            {
                Text = "Danh sách Khách hàng (DataTable & DataView - Hiển thị cột Đa hình VaiTrò & % Ưu Đãi)",
                Dock = DockStyle.Fill
            };
            dgvKhachHang = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dgvKhachHang.SelectionChanged += DgvKhachHang_SelectionChanged;
            grpGrid.Controls.Add(dgvKhachHang);

            tab.Controls.Add(grpGrid);
            tab.Controls.Add(grpEdit);
            tab.Controls.Add(pnlTopFilter);
        }

        private void XayDungTabDichVu(TabPage tab)
        {
            Panel pnlTopDV = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Color.White,
                Padding = new Padding(10)
            };
            Label lblTimDV = new Label
            {
                Text = "Tìm kiếm Dịch vụ ngắt kết nối (DataView.RowFilter):",
                Location = new Point(12, 16),
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 58, 108)
            };
            txtTimDichVuDisconnected = new TextBox { Location = new Point(355, 13), Width = 280 };
            txtTimDichVuDisconnected.TextChanged += (s, e) => LocDataViewDichVu();
            pnlTopDV.Controls.AddRange(new Control[] { lblTimDV, txtTimDichVuDisconnected });

            GroupBox grpEditDV = new GroupBox
            {
                Text = "Thông tin Dịch vụ & Tồn kho",
                Dock = DockStyle.Right,
                Width = 420
            };

            Label d1 = new Label { Text = "Mã dịch vụ:", Location = new Point(16, 32), AutoSize = true };
            txtMaDV = new TextBox { Location = new Point(120, 29), Width = 140 };

            Label d2 = new Label { Text = "Tên dịch vụ:", Location = new Point(16, 68), AutoSize = true };
            txtTenDV = new TextBox { Location = new Point(120, 65), Width = 275 };

            Label d3 = new Label { Text = "Danh mục:", Location = new Point(16, 104), AutoSize = true };
            cboDanhMucDV = new ComboBox
            {
                Location = new Point(120, 101),
                Width = 275,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboDanhMucDV.Items.AddRange(new object[]
            {
                "Đồ uống & Minibar",
                "Ẩm thực & Ăn sáng",
                "Giặt ủi & Tiện ích"
            });
            cboDanhMucDV.SelectedIndex = 0;

            Label d4 = new Label { Text = "Đơn vị tính:", Location = new Point(16, 140), AutoSize = true };
            txtDonViTinh = new TextBox { Location = new Point(120, 137), Width = 140, Text = "Chai" };

            Label d5 = new Label { Text = "Đơn giá (VNĐ):", Location = new Point(16, 176), AutoSize = true };
            txtDonGiaDV = new TextBox { Location = new Point(120, 173), Width = 140, Text = "20000" };
            txtDonGiaDV.KeyPress += ChiChoNhapSo_KeyPress;

            Label d6 = new Label { Text = "Số lượng tồn:", Location = new Point(16, 212), AutoSize = true };
            numSoLuongTon = new NumericUpDown
            {
                Location = new Point(120, 209),
                Width = 140,
                Minimum = 0,
                Maximum = 100000,
                Value = 50
            };

            Button btnThemDV = new Button
            {
                Text = "Thêm DV",
                Location = new Point(16, 260),
                Size = new Size(92, 34),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThemDV.Click += BtnThemDV_Click;

            Button btnSuaDV = new Button
            {
                Text = "Cập nhật",
                Location = new Point(116, 260),
                Size = new Size(92, 34),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSuaDV.Click += BtnSuaDV_Click;

            Button btnXoaDV = new Button
            {
                Text = "Xóa DV",
                Location = new Point(216, 260),
                Size = new Size(92, 34),
                BackColor = Color.FromArgb(185, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnXoaDV.Click += BtnXoaDV_Click;

            grpEditDV.Controls.AddRange(new Control[]
            {
                d1, txtMaDV, d2, txtTenDV, d3, cboDanhMucDV, d4, txtDonViTinh, d5, txtDonGiaDV, d6, numSoLuongTon,
                btnThemDV, btnSuaDV, btnXoaDV
            });

            GroupBox grpGridDV = new GroupBox
            {
                Text = "Danh sách Dịch vụ khách sạn (DataTable & DataView)",
                Dock = DockStyle.Fill
            };
            dgvDichVu = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dgvDichVu.SelectionChanged += DgvDichVu_SelectionChanged;
            grpGridDV.Controls.Add(dgvDichVu);

            tab.Controls.Add(grpGridDV);
            tab.Controls.Add(grpEditDV);
            tab.Controls.Add(pnlTopDV);
        }

        private void ChiChoNhapSo_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        public void TaiLaiDuLieu()
        {
            // 1. Nạp DataTable Khách hàng và tạo DataView để lọc ngắt kết nối
            _dtKhachHang = _khBus.GetDataTableKhachHang();
            _dvKhachHang = new DataView(_dtKhachHang);
            dgvKhachHang.DataSource = _dvKhachHang;

            // 2. Nạp DataTable Dịch vụ và tạo DataView
            _dtDichVu = _dvBus.GetDataTableDichVu();
            _dvDichVu = new DataView(_dtDichVu);
            dgvDichVu.DataSource = _dvDichVu;

            if (dgvDichVu.Columns["DonGia"] != null)
                dgvDichVu.Columns["DonGia"].DefaultCellStyle.Format = "N0";
            if (dgvDichVu.Columns["GiaTriTonKho"] != null)
                dgvDichVu.Columns["GiaTriTonKho"].DefaultCellStyle.Format = "N0";

            CapNhatNhanDaHinh();
        }

        private void LocDataViewKhachHang()
        {
            if (_dvKhachHang == null) return;
            string kw = txtTimKhachDisconnected.Text.Trim().Replace("'", "''");
            string loai = cboLocLoaiKhachDisconnected.SelectedIndex > 0
                ? cboLocLoaiKhachDisconnected.SelectedItem.ToString()
                : "";

            string filter = string.Format("(MaKH LIKE '%{0}%' OR HoTen LIKE '%{0}%' OR CCCD LIKE '%{0}%' OR SoDienThoai LIKE '%{0}%')", kw);
            if (!string.IsNullOrEmpty(loai))
            {
                filter += string.Format(" AND LoaiKhach = '{0}'", loai);
            }
            _dvKhachHang.RowFilter = filter;
        }

        private void LocDataViewDichVu()
        {
            if (_dvDichVu == null) return;
            string kw = txtTimDichVuDisconnected.Text.Trim().Replace("'", "''");
            _dvDichVu.RowFilter = string.Format("MaDV LIKE '%{0}%' OR TenDV LIKE '%{0}%' OR DanhMuc LIKE '%{0}%'", kw);
        }

        private void CapNhatNhanDaHinh()
        {
            string loai = cboLoaiKhach.SelectedItem != null ? cboLoaiKhach.SelectedItem.ToString() : "Thường";
            int diem = (int)numDiemTichLuy.Value;
            NguoiBase doiTuongDaHinh = _khBus.TaoDoiTuongKhachHang("DEMO", "Khách", "000000000", "0900000000", "", loai, diem);

            lblMinhHoaDaHinh.Text = string.Format(
                "ĐA HÌNH OOP ({0}):\nVai trò: {1} | Giảm giá tự động: {2:0}%",
                doiTuongDaHinh.GetType().Name,
                doiTuongDaHinh.LayVaiTro(),
                doiTuongDaHinh.TinhTyLeGiamGia() * 100m);
        }

        private void DgvKhachHang_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvKhachHang.CurrentRow == null) return;
            DataRowView row = dgvKhachHang.CurrentRow.DataBoundItem as DataRowView;
            if (row == null) return;

            txtMaKH.Text = row["MaKH"].ToString();
            txtHoTen.Text = row["HoTen"].ToString();
            txtCCCD.Text = row["CCCD"].ToString();
            txtSDT.Text = row["SoDienThoai"].ToString();
            txtDiaChi.Text = row["DiaChi"].ToString();
            cboLoaiKhach.SelectedItem = row["LoaiKhach"].ToString();
            numDiemTichLuy.Value = Convert.ToInt32(row["DiemTichLuy"]);
        }

        private void DgvDichVu_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDichVu.CurrentRow == null) return;
            DataRowView row = dgvDichVu.CurrentRow.DataBoundItem as DataRowView;
            if (row == null) return;

            txtMaDV.Text = row["MaDV"].ToString();
            txtTenDV.Text = row["TenDV"].ToString();
            cboDanhMucDV.SelectedItem = row["DanhMuc"].ToString();
            txtDonViTinh.Text = row["DonViTinh"].ToString();
            txtDonGiaDV.Text = Convert.ToDecimal(row["DonGia"]).ToString("0");
            numSoLuongTon.Value = Convert.ToInt32(row["SoLuongTon"]);
        }

        private void BtnThemKH_Click(object sender, EventArgs e)
        {
            try
            {
                KhachHang kh = _khBus.TaoDoiTuongKhachHang(
                    txtMaKH.Text,
                    txtHoTen.Text,
                    txtCCCD.Text,
                    txtSDT.Text,
                    txtDiaChi.Text,
                    cboLoaiKhach.SelectedItem.ToString(),
                    (int)numDiemTichLuy.Value
                );
                _khBus.ThemKhachHang(kh);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã thêm khách hàng bằng Stored Procedure: " + kh.HoTen);
                MessageBox.Show("Thêm khách hàng thành công!", "Stored Procedure", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSuaKH_Click(object sender, EventArgs e)
        {
            try
            {
                KhachHang kh = _khBus.TaoDoiTuongKhachHang(
                    txtMaKH.Text,
                    txtHoTen.Text,
                    txtCCCD.Text,
                    txtSDT.Text,
                    txtDiaChi.Text,
                    cboLoaiKhach.SelectedItem.ToString(),
                    (int)numDiemTichLuy.Value
                );
                _khBus.CapNhatKhachHang(kh);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã cập nhật khách hàng bằng Stored Procedure: " + kh.HoTen);
                MessageBox.Show("Cập nhật khách hàng thành công!", "Stored Procedure", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnXoaKH_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaKH.Text)) return;
            if (MessageBox.Show("Xác nhận xóa khách hàng " + txtMaKH.Text + "?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string ma = txtMaKH.Text.Trim();
                _khBus.XoaKhachHang(ma);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã xóa khách hàng: " + ma);
                MessageBox.Show("Xóa khách hàng thành công!", "Stored Procedure", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể xóa khách hàng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnThemDV_Click(object sender, EventArgs e)
        {
            try
            {
                if (!decimal.TryParse(txtDonGiaDV.Text, out decimal donGia))
                {
                    MessageBox.Show("Đơn giá dịch vụ không hợp lệ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DichVu dv = new DichVu(
                    txtMaDV.Text,
                    txtTenDV.Text,
                    cboDanhMucDV.SelectedItem.ToString(),
                    txtDonViTinh.Text,
                    donGia,
                    (int)numSoLuongTon.Value
                );
                _dvBus.ThemDichVu(dv);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã thêm dịch vụ: " + dv.TenDV);
                MessageBox.Show("Thêm dịch vụ thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSuaDV_Click(object sender, EventArgs e)
        {
            try
            {
                if (!decimal.TryParse(txtDonGiaDV.Text, out decimal donGia))
                {
                    MessageBox.Show("Đơn giá dịch vụ không hợp lệ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DichVu dv = new DichVu(
                    txtMaDV.Text,
                    txtTenDV.Text,
                    cboDanhMucDV.SelectedItem.ToString(),
                    txtDonViTinh.Text,
                    donGia,
                    (int)numSoLuongTon.Value
                );
                _dvBus.CapNhatDichVu(dv);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã cập nhật dịch vụ: " + dv.TenDV);
                MessageBox.Show("Cập nhật dịch vụ thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnXoaDV_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaDV.Text)) return;
            try
            {
                string ma = txtMaDV.Text.Trim();
                _dvBus.XoaDichVu(ma);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã xóa dịch vụ: " + ma);
                MessageBox.Show("Xóa dịch vụ thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
