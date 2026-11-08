using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.BUS;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.GUI
{
    /// <summary>
    /// Form Quản lý Loại phòng (Master) & Phòng (Detail).
    /// Minh họa các kỹ thuật trọng tâm trong đề cương:
    /// 1. Quan hệ MASTER-DETAIL bằng DataSet + DataRelation ("FK_LoaiPhong_Phong") + 2 BindingSource
    /// 2. Hai điều khiển DataGridView tự động đồng bộ: Chọn Loại phòng ở lưới trên -> Lưới dưới tự lọc đúng các Phòng thuộc loại đó
    /// 3. CRUD Tham số hóa (SqlCommand + SqlParameter + ExecuteNonQuery)
    /// 4. Kiểm tra dữ liệu bằng ErrorProvider và sự kiện KeyPress (chỉ cho phép nhập số).
    /// </summary>
    public class frmQuanLyPhongMasterDetail : Form
    {
        private readonly PhongBUS _phongBus = new PhongBUS();
        public event Action<string> OnDataChanged;

        private DataSet _dsMasterDetail;
        private BindingSource _bsMasterLoaiPhong;
        private BindingSource _bsDetailPhong;

        private DataGridView dgvMasterLoaiPhong;
        private DataGridView dgvDetailPhong;

        // Điều khiển CRUD Phòng
        private TextBox txtMaPhong;
        private TextBox txtTenPhong;
        private NumericUpDown numTang;
        private ComboBox cboLoaiPhong;
        private ComboBox cboTrangThai;
        private TextBox txtGhiChu;

        // Điều khiển cập nhật nhanh Giá Loại Phòng
        private TextBox txtMaLoai;
        private TextBox txtTenLoai;
        private TextBox txtDonGiaNgay;
        private TextBox txtDonGiaGio;
        private NumericUpDown numSoNguoi;

        private ErrorProvider errorProvider;

        public frmQuanLyPhongMasterDetail()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Quản Lý Loại Phòng & Phòng (Master-Detail DataSet & CRUD SqlParameter)";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(245, 248, 252);

            errorProvider = new ErrorProvider { BlinkStyle = ErrorBlinkStyle.NeverBlink };

            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 270
            };

            // ================= KHU VỰC 1: MASTER (LOẠI PHÒNG) =================
            Panel pnlMaster = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            GroupBox grpMasterGrid = new GroupBox
            {
                Text = "BẢNG MASTER: Danh sách Loại Phòng (Chọn 1 dòng để lọc bảng Detail bên dưới)",
                Dock = DockStyle.Fill
            };
            dgvMasterLoaiPhong = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dgvMasterLoaiPhong.SelectionChanged += DgvMasterLoaiPhong_SelectionChanged;
            grpMasterGrid.Controls.Add(dgvMasterLoaiPhong);

            GroupBox grpMasterEdit = new GroupBox
            {
                Text = "Cập nhật Loại Phòng (Master)",
                Dock = DockStyle.Right,
                Width = 420
            };

            Label l1 = new Label { Text = "Mã loại:", Location = new Point(14, 28), AutoSize = true };
            txtMaLoai = new TextBox { Location = new Point(110, 25), Width = 120 };
            Label l2 = new Label { Text = "Số người:", Location = new Point(245, 28), AutoSize = true };
            numSoNguoi = new NumericUpDown { Location = new Point(315, 25), Width = 80, Minimum = 1, Maximum = 20, Value = 2 };

            Label l3 = new Label { Text = "Tên loại:", Location = new Point(14, 60), AutoSize = true };
            txtTenLoai = new TextBox { Location = new Point(110, 57), Width = 285 };

            Label l4 = new Label { Text = "Giá ngày (đ):", Location = new Point(14, 92), AutoSize = true };
            txtDonGiaNgay = new TextBox { Location = new Point(110, 89), Width = 120 };
            txtDonGiaNgay.KeyPress += ChiChoNhapSo_KeyPress;

            Label l5 = new Label { Text = "Giá giờ (đ):", Location = new Point(238, 92), AutoSize = true };
            txtDonGiaGio = new TextBox { Location = new Point(310, 89), Width = 85 };
            txtDonGiaGio.KeyPress += ChiChoNhapSo_KeyPress;

            Button btnThemLoai = new Button
            {
                Text = "Thêm loại",
                Location = new Point(110, 128),
                Size = new Size(130, 30),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThemLoai.Click += BtnThemLoai_Click;

            Button btnSuaLoai = new Button
            {
                Text = "Lưu giá loại phòng",
                Location = new Point(250, 128),
                Size = new Size(145, 30),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSuaLoai.Click += BtnSuaLoai_Click;

            grpMasterEdit.Controls.AddRange(new Control[]
            {
                l1, txtMaLoai, l2, numSoNguoi, l3, txtTenLoai, l4, txtDonGiaNgay, l5, txtDonGiaGio,
                btnThemLoai, btnSuaLoai
            });

            pnlMaster.Controls.Add(grpMasterGrid);
            pnlMaster.Controls.Add(grpMasterEdit);

            // ================= KHU VỰC 2: DETAIL (PHÒNG THUỘC LOẠI PHÒNG ĐANG CHỌN) =================
            Panel pnlDetail = new Panel { Dock = DockStyle.Fill, Padding = new Padding(8) };

            GroupBox grpDetailGrid = new GroupBox
            {
                Text = "BẢNG DETAIL: Danh sách các Phòng thuộc Loại Phòng đang chọn (Tự động lọc qua DataRelation)",
                Dock = DockStyle.Fill
            };
            dgvDetailPhong = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = Color.White
            };
            dgvDetailPhong.SelectionChanged += DgvDetailPhong_SelectionChanged;
            grpDetailGrid.Controls.Add(dgvDetailPhong);

            GroupBox grpDetailEdit = new GroupBox
            {
                Text = "CRUD Phòng (SqlCommand & SqlParameter)",
                Dock = DockStyle.Right,
                Width = 420
            };

            Label p1 = new Label { Text = "Mã phòng:", Location = new Point(14, 28), AutoSize = true };
            txtMaPhong = new TextBox { Location = new Point(110, 25), Width = 120 };

            Label p2 = new Label { Text = "Tầng:", Location = new Point(250, 28), AutoSize = true };
            numTang = new NumericUpDown { Location = new Point(305, 25), Width = 90, Minimum = 1, Maximum = 30, Value = 1 };

            Label p3 = new Label { Text = "Tên phòng:", Location = new Point(14, 60), AutoSize = true };
            txtTenPhong = new TextBox { Location = new Point(110, 57), Width = 285 };

            Label p4 = new Label { Text = "Loại phòng:", Location = new Point(14, 92), AutoSize = true };
            cboLoaiPhong = new ComboBox
            {
                Location = new Point(110, 89),
                Width = 285,
                DropDownStyle = ComboBoxStyle.DropDownList,
                DisplayMember = "TenLoai",
                ValueMember = "MaLoai"
            };

            Label p5 = new Label { Text = "Trạng thái:", Location = new Point(14, 124), AutoSize = true };
            cboTrangThai = new ComboBox
            {
                Location = new Point(110, 121),
                Width = 285,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cboTrangThai.Items.AddRange(new object[] { "Trống", "Đang thuê", "Đang dọn", "Bảo trì" });
            cboTrangThai.SelectedIndex = 0;

            Label p6 = new Label { Text = "Ghi chú:", Location = new Point(14, 156), AutoSize = true };
            txtGhiChu = new TextBox { Location = new Point(110, 153), Width = 285 };

            Button btnThemPhong = new Button
            {
                Text = "Thêm Phòng",
                Location = new Point(14, 195),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnThemPhong.Click += BtnThemPhong_Click;

            Button btnSuaPhong = new Button
            {
                Text = "Cập Nhật",
                Location = new Point(114, 195),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnSuaPhong.Click += BtnSuaPhong_Click;

            Button btnXoaPhong = new Button
            {
                Text = "Xóa Phòng",
                Location = new Point(214, 195),
                Size = new Size(92, 32),
                BackColor = Color.FromArgb(185, 45, 45),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnXoaPhong.Click += BtnXoaPhong_Click;

            Button btnLamMoi = new Button
            {
                Text = "Nhập mới",
                Location = new Point(314, 195),
                Size = new Size(82, 32)
            };
            btnLamMoi.Click += (s, e) =>
            {
                txtMaPhong.Clear();
                txtTenPhong.Clear();
                txtGhiChu.Clear();
                cboTrangThai.SelectedIndex = 0;
                txtMaPhong.Focus();
            };

            grpDetailEdit.Controls.AddRange(new Control[]
            {
                p1, txtMaPhong, p2, numTang, p3, txtTenPhong, p4, cboLoaiPhong, p5, cboTrangThai, p6, txtGhiChu,
                btnThemPhong, btnSuaPhong, btnXoaPhong, btnLamMoi
            });

            pnlDetail.Controls.Add(grpDetailGrid);
            pnlDetail.Controls.Add(grpDetailEdit);

            splitMain.Panel1.Controls.Add(pnlMaster);
            splitMain.Panel2.Controls.Add(pnlDetail);

            this.Controls.Add(splitMain);
            this.Load += (s, e) => TaiLaiDuLieu();
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
            // Nạp danh sách loại phòng cho ComboBox
            cboLoaiPhong.DataSource = _phongBus.GetAllLoaiPhong();
            cboLoaiPhong.DisplayMember = "TenLoai";
            cboLoaiPhong.ValueMember = "MaLoai";

            // Nạp DataSet Master-Detail (LoaiPhong -> Phong)
            _dsMasterDetail = _phongBus.GetMasterDetailDataSet();

            _bsMasterLoaiPhong = new BindingSource
            {
                DataSource = _dsMasterDetail,
                DataMember = "LoaiPhong"
            };

            // Liên kết BindingSource Detail với quan hệ FK_LoaiPhong_Phong
            _bsDetailPhong = new BindingSource
            {
                DataSource = _bsMasterLoaiPhong,
                DataMember = "FK_LoaiPhong_Phong"
            };

            dgvMasterLoaiPhong.DataSource = _bsMasterLoaiPhong;
            dgvDetailPhong.DataSource = _bsDetailPhong;

            if (dgvMasterLoaiPhong.Columns["DonGiaNgay"] != null)
                dgvMasterLoaiPhong.Columns["DonGiaNgay"].DefaultCellStyle.Format = "N0";
            if (dgvMasterLoaiPhong.Columns["DonGiaGio"] != null)
                dgvMasterLoaiPhong.Columns["DonGiaGio"].DefaultCellStyle.Format = "N0";
        }

        private void DgvMasterLoaiPhong_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvMasterLoaiPhong.CurrentRow == null) return;
            DataRowView drv = dgvMasterLoaiPhong.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            txtMaLoai.Text = drv["MaLoai"].ToString();
            txtTenLoai.Text = drv["TenLoai"].ToString();
            txtDonGiaNgay.Text = Convert.ToDecimal(drv["DonGiaNgay"]).ToString("0");
            txtDonGiaGio.Text = Convert.ToDecimal(drv["DonGiaGio"]).ToString("0");
            numSoNguoi.Value = Convert.ToInt32(drv["SoNguoiToiDa"]);

            cboLoaiPhong.SelectedValue = drv["MaLoai"].ToString();
        }

        private void DgvDetailPhong_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvDetailPhong.CurrentRow == null) return;
            DataRowView drv = dgvDetailPhong.CurrentRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            txtMaPhong.Text = drv["MaPhong"].ToString();
            txtTenPhong.Text = drv["TenPhong"].ToString();
            numTang.Value = Convert.ToInt32(drv["Tang"]);
            cboLoaiPhong.SelectedValue = drv["MaLoai"].ToString();
            cboTrangThai.SelectedItem = drv["TrangThai"].ToString();
            txtGhiChu.Text = drv["GhiChu"].ToString();
        }

        private void BtnThemLoai_Click(object sender, EventArgs e)
        {
            try
            {
                if (!decimal.TryParse(txtDonGiaNgay.Text, out decimal giaNgay) ||
                    !decimal.TryParse(txtDonGiaGio.Text, out decimal giaGio))
                {
                    MessageBox.Show("Đơn giá ngày và giờ phải là số hợp lệ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoaiPhong lp = new LoaiPhong(
                    txtMaLoai.Text.Trim(),
                    txtTenLoai.Text.Trim(),
                    giaNgay,
                    giaGio,
                    (int)numSoNguoi.Value,
                    "Loại phòng mới"
                );
                _phongBus.ThemLoaiPhong(lp);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã thêm loại phòng: " + lp.TenLoai);
                MessageBox.Show("Thêm loại phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSuaLoai_Click(object sender, EventArgs e)
        {
            try
            {
                if (!decimal.TryParse(txtDonGiaNgay.Text, out decimal giaNgay) ||
                    !decimal.TryParse(txtDonGiaGio.Text, out decimal giaGio))
                {
                    MessageBox.Show("Đơn giá ngày và giờ phải là số hợp lệ.", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                LoaiPhong lp = new LoaiPhong(
                    txtMaLoai.Text.Trim(),
                    txtTenLoai.Text.Trim(),
                    giaNgay,
                    giaGio,
                    (int)numSoNguoi.Value,
                    ""
                );
                _phongBus.CapNhatLoaiPhong(lp);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã cập nhật bảng giá loại phòng: " + lp.TenLoai);
                MessageBox.Show("Cập nhật loại phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool KiemTraHopLePhong()
        {
            errorProvider.Clear();
            bool ok = true;
            if (string.IsNullOrWhiteSpace(txtMaPhong.Text))
            {
                errorProvider.SetError(txtMaPhong, "Mã phòng không được để trống!");
                ok = false;
            }
            if (string.IsNullOrWhiteSpace(txtTenPhong.Text))
            {
                errorProvider.SetError(txtTenPhong, "Tên phòng không được để trống!");
                ok = false;
            }
            return ok;
        }

        private void BtnThemPhong_Click(object sender, EventArgs e)
        {
            if (!KiemTraHopLePhong()) return;
            try
            {
                Phong p = new Phong
                {
                    MaPhong = txtMaPhong.Text.Trim(),
                    TenPhong = txtTenPhong.Text.Trim(),
                    Tang = (int)numTang.Value,
                    MaLoai = cboLoaiPhong.SelectedValue.ToString(),
                    TrangThai = cboTrangThai.SelectedItem.ToString(),
                    GhiChu = txtGhiChu.Text.Trim()
                };
                _phongBus.ThemPhong(p);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã thêm phòng mới: " + p.MaPhong);
                MessageBox.Show("Thêm phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi thêm phòng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnSuaPhong_Click(object sender, EventArgs e)
        {
            if (!KiemTraHopLePhong()) return;
            try
            {
                Phong p = new Phong
                {
                    MaPhong = txtMaPhong.Text.Trim(),
                    TenPhong = txtTenPhong.Text.Trim(),
                    Tang = (int)numTang.Value,
                    MaLoai = cboLoaiPhong.SelectedValue.ToString(),
                    TrangThai = cboTrangThai.SelectedItem.ToString(),
                    GhiChu = txtGhiChu.Text.Trim()
                };
                _phongBus.CapNhatPhong(p);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã cập nhật phòng: " + p.MaPhong);
                MessageBox.Show("Cập nhật phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi cập nhật phòng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnXoaPhong_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtMaPhong.Text)) return;
            if (MessageBox.Show("Bạn có chắc muốn xóa phòng " + txtMaPhong.Text + "?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            {
                return;
            }

            try
            {
                string ma = txtMaPhong.Text.Trim();
                _phongBus.XoaPhong(ma);
                TaiLaiDuLieu();
                OnDataChanged?.Invoke("Đã xóa phòng: " + ma);
                MessageBox.Show("Xóa phòng thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Không thể xóa phòng", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
