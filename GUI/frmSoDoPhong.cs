using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using DeTai18_QuanLyKhachSan.BUS;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.GUI
{
    /// <summary>
    /// Form Sơ đồ phòng trực quan của Khách sạn.
    /// Minh họa các kỹ thuật trong đề cương:
    /// 1. Điều khiển TREEVIEW (Phân cấp Khách sạn -> Tầng -> Phòng)
    /// 2. Điều khiển LISTVIEW (Hiển thị danh sách phòng có phân nhóm ListViewGroup và tô màu trạng thái)
    /// 3. Đọc, tìm kiếm & lọc dữ liệu bằng SqlDataReader + Stored Procedure (sp_TimKiemPhong)
    /// 4. Đếm nhanh số lượng phòng theo trạng thái bằng ExecuteScalar()
    /// 5. Liên kết đa Form: Click chọn phòng -> Chuyển thẳng sang Form Nhận/Trả phòng.
    /// </summary>
    public class frmSoDoPhong : Form
    {
        private readonly PhongBUS _phongBus = new PhongBUS();

        public event Action<string> OnStatusMessage;
        public event Action<string> OnOpenBookingForRoom;

        private TreeView tvSoDoTang;
        private ListView lvDanhSachPhong;
        private ComboBox cboLocTang;
        private ComboBox cboLocLoaiPhong;
        private ComboBox cboLocTrangThai;
        private Label lblThongKeScalar;
        private Label lblPhongDangChon;
        private Button btnMoNghiepVuPhong;
        private Button btnLamMoi;
        private string _maPhongDangChon = null;

        public frmSoDoPhong()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Sơ Đồ Phòng Khách Sạn (TreeView & ListView - SqlDataReader & ExecuteScalar)";
            this.WindowState = FormWindowState.Maximized;
            this.BackColor = Color.FromArgb(246, 249, 253);

            // Panel trên cùng: Bộ lọc tìm kiếm (SqlDataReader) + Thống kê (ExecuteScalar)
            Panel pnlTop = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                Padding = new Padding(10),
                BackColor = Color.White
            };

            Label lblFilterTitle = new Label
            {
                Text = "BỘ LỌC PHÒNG (SqlDataReader & sp_TimKiemPhong):",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(24, 58, 108),
                Location = new Point(14, 12),
                AutoSize = true
            };

            Label lblTang = new Label { Text = "Tầng:", Location = new Point(14, 45), AutoSize = true };
            cboLocTang = new ComboBox
            {
                Location = new Point(58, 42),
                Width = 105,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblLoai = new Label { Text = "Loại phòng:", Location = new Point(178, 45), AutoSize = true };
            cboLocLoaiPhong = new ComboBox
            {
                Location = new Point(258, 42),
                Width = 210,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Label lblTrangThai = new Label { Text = "Trạng thái:", Location = new Point(482, 45), AutoSize = true };
            cboLocTrangThai = new ComboBox
            {
                Location = new Point(558, 42),
                Width = 135,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            Button btnLoc = new Button
            {
                Text = "Lọc dữ liệu",
                Location = new Point(705, 40),
                Size = new Size(100, 29),
                BackColor = Color.FromArgb(24, 58, 108),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            btnLoc.Click += (s, e) => ApDungBoLoc();

            btnLamMoi = new Button
            {
                Text = "Tất cả phòng",
                Location = new Point(812, 40),
                Size = new Size(105, 29)
            };
            btnLamMoi.Click += (s, e) =>
            {
                cboLocTang.SelectedIndex = 0;
                cboLocLoaiPhong.SelectedIndex = 0;
                cboLocTrangThai.SelectedIndex = 0;
                TaiLaiDuLieu();
            };

            lblThongKeScalar = new Label
            {
                Location = new Point(360, 12),
                Size = new Size(760, 22),
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(25, 110, 60)
            };

            pnlTop.Controls.AddRange(new Control[]
            {
                lblFilterTitle, lblThongKeScalar,
                lblTang, cboLocTang, lblLoai, cboLocLoaiPhong, lblTrangThai, cboLocTrangThai,
                btnLoc, btnLamMoi
            });

            // Panel dưới cùng: Thanh liên kết nhanh sang Form Nhận/Trả phòng
            Panel pnlBottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 56,
                BackColor = Color.FromArgb(234, 241, 250),
                Padding = new Padding(12)
            };

            lblPhongDangChon = new Label
            {
                Text = "Hãy chọn 1 phòng trên TreeView hoặc ListView để thực hiện Nhận phòng / Gọi dịch vụ / Thanh toán.",
                Location = new Point(15, 17),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(20, 50, 95)
            };

            btnMoNghiepVuPhong = new Button
            {
                Text = "Chuyển sang Nhận / Trả Phòng & Gọi Dịch Vụ >>",
                Location = new Point(760, 11),
                Size = new Size(340, 34),
                BackColor = Color.FromArgb(28, 115, 68),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold)
            };
            btnMoNghiepVuPhong.Click += (s, e) =>
            {
                OnOpenBookingForRoom?.Invoke(_maPhongDangChon);
            };

            pnlBottom.Controls.AddRange(new Control[] { lblPhongDangChon, btnMoNghiepVuPhong });

            // SplitContainer chia trái (TreeView) và phải (ListView)
            SplitContainer splitMain = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 300
            };

            GroupBox grpTree = new GroupBox
            {
                Text = "Cây phân tầng khách sạn (TreeView)",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            tvSoDoTang = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                Font = new Font("Segoe UI", 9.5F)
            };
            tvSoDoTang.AfterSelect += TvSoDoTang_AfterSelect;
            grpTree.Controls.Add(tvSoDoTang);

            GroupBox grpList = new GroupBox
            {
                Text = "Danh sách chi tiết phòng theo tầng (ListView - Tô màu theo trạng thái)",
                Dock = DockStyle.Fill,
                Padding = new Padding(8)
            };
            lvDanhSachPhong = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false,
                Font = new Font("Segoe UI", 9.5F)
            };
            lvDanhSachPhong.Columns.Add("Mã Phòng", 95);
            lvDanhSachPhong.Columns.Add("Tên Phòng", 120);
            lvDanhSachPhong.Columns.Add("Tầng", 70);
            lvDanhSachPhong.Columns.Add("Loại Phòng", 210);
            lvDanhSachPhong.Columns.Add("Giá Ngày (VNĐ)", 130, HorizontalAlignment.Right);
            lvDanhSachPhong.Columns.Add("Giá Giờ (VNĐ)", 120, HorizontalAlignment.Right);
            lvDanhSachPhong.Columns.Add("Số Người", 85, HorizontalAlignment.Center);
            lvDanhSachPhong.Columns.Add("Trạng Thái", 120);
            lvDanhSachPhong.Columns.Add("Ghi Chú", 240);

            lvDanhSachPhong.SelectedIndexChanged += LvDanhSachPhong_SelectedIndexChanged;
            lvDanhSachPhong.DoubleClick += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_maPhongDangChon))
                {
                    OnOpenBookingForRoom?.Invoke(_maPhongDangChon);
                }
            };

            grpList.Controls.Add(lvDanhSachPhong);

            splitMain.Panel1.Controls.Add(grpTree);
            splitMain.Panel2.Controls.Add(grpList);

            this.Controls.Add(splitMain);
            this.Controls.Add(pnlBottom);
            this.Controls.Add(pnlTop);

            this.Load += (s, e) =>
            {
                KhoiTaoComboBoxLoc();
                TaiLaiDuLieu();
            };
        }

        private void KhoiTaoComboBoxLoc()
        {
            cboLocTang.Items.Clear();
            cboLocTang.Items.Add("Tất cả tầng");
            cboLocTang.Items.Add("Tầng 1");
            cboLocTang.Items.Add("Tầng 2");
            cboLocTang.Items.Add("Tầng 3");
            cboLocTang.SelectedIndex = 0;

            cboLocLoaiPhong.Items.Clear();
            cboLocLoaiPhong.Items.Add(new ComboItem("", "-- Tất cả loại phòng --"));
            foreach (LoaiPhong lp in _phongBus.GetAllLoaiPhong())
            {
                cboLocLoaiPhong.Items.Add(new ComboItem(lp.MaLoai, lp.TenLoai));
            }
            cboLocLoaiPhong.SelectedIndex = 0;

            cboLocTrangThai.Items.Clear();
            cboLocTrangThai.Items.Add("-- Tất cả --");
            cboLocTrangThai.Items.Add("Trống");
            cboLocTrangThai.Items.Add("Đang thuê");
            cboLocTrangThai.Items.Add("Đang dọn");
            cboLocTrangThai.Items.Add("Bảo trì");
            cboLocTrangThai.SelectedIndex = 0;
        }

        public void TaiLaiDuLieu()
        {
            NapTreeViewSoDoTang();
            ApDungBoLoc();
            CapNhatThongKeExecuteScalar();
        }

        private void CapNhatThongKeExecuteScalar()
        {
            int tong = _phongBus.DemSoPhongTheoTrangThai("");
            int trong = _phongBus.DemSoPhongTheoTrangThai("Trống");
            int dangThue = _phongBus.DemSoPhongTheoTrangThai("Đang thuê");
            int dangDon = _phongBus.DemSoPhongTheoTrangThai("Đang dọn");
            int baoTri = _phongBus.DemSoPhongTheoTrangThai("Bảo trì");

            lblThongKeScalar.Text = string.Format(
                "THỐNG KÊ NHANH (ExecuteScalar): Tổng {0} phòng | [Trống: {1}] - [Đang thuê: {2}] - [Đang dọn: {3}] - [Bảo trì: {4}]",
                tong, trong, dangThue, dangDon, baoTri);
        }

        private void NapTreeViewSoDoTang()
        {
            tvSoDoTang.Nodes.Clear();
            TreeNode root = new TreeNode("KHÁCH SẠN GRAND .NET") { Tag = "ROOT" };

            Dictionary<int, List<Phong>> dictTang = _phongBus.GetPhongNhomTheoTang();
            foreach (KeyValuePair<int, List<Phong>> kvp in dictTang)
            {
                TreeNode nodeTang = new TreeNode(string.Format("Tầng {0} ({1} phòng)", kvp.Key, kvp.Value.Count))
                {
                    Tag = "TANG:" + kvp.Key
                };

                foreach (Phong p in kvp.Value)
                {
                    TreeNode nodePhong = new TreeNode(string.Format("{0} - {1} [{2}]", p.MaPhong, p.TenPhong, p.TrangThai))
                    {
                        Tag = "PHONG:" + p.MaPhong
                    };
                    nodePhong.ForeColor = LayMauChuTheoTrangThai(p.TrangThai);
                    nodeTang.Nodes.Add(nodePhong);
                }

                root.Nodes.Add(nodeTang);
            }

            tvSoDoTang.Nodes.Add(root);
            root.ExpandAll();
        }

        private void ApDungBoLoc()
        {
            int tang = cboLocTang.SelectedIndex > 0 ? cboLocTang.SelectedIndex : 0;
            string maLoai = (cboLocLoaiPhong.SelectedItem as ComboItem)?.Value ?? "";
            string trangThai = cboLocTrangThai.SelectedIndex > 0 ? cboLocTrangThai.SelectedItem.ToString() : "";

            List<Phong> dsPhong = _phongBus.TimKiemPhong(tang, maLoai, trangThai);
            HienThiLenListView(dsPhong);
            OnStatusMessage?.Invoke(string.Format("Đã đọc {0} phòng bằng SqlDataReader.", dsPhong.Count));
        }

        private void HienThiLenListView(List<Phong> dsPhong)
        {
            lvDanhSachPhong.BeginUpdate();
            lvDanhSachPhong.Items.Clear();
            lvDanhSachPhong.Groups.Clear();

            foreach (var grpTang in dsPhong.GroupBy(x => x.Tang).OrderBy(g => g.Key))
            {
                ListViewGroup group = new ListViewGroup(string.Format("Khu vực Tầng {0}", grpTang.Key));
                lvDanhSachPhong.Groups.Add(group);

                foreach (Phong p in grpTang)
                {
                    ListViewItem item = new ListViewItem(p.MaPhong, group);
                    item.SubItems.Add(p.TenPhong);
                    item.SubItems.Add("Tầng " + p.Tang);
                    item.SubItems.Add(p.TenLoai);
                    item.SubItems.Add(p.DonGiaNgay.ToString("N0"));
                    item.SubItems.Add(p.DonGiaGio.ToString("N0"));
                    item.SubItems.Add(p.SoNguoiToiDa.ToString());
                    item.SubItems.Add(p.TrangThai);
                    item.SubItems.Add(p.GhiChu ?? "");
                    item.Tag = p;
                    item.BackColor = LayMauNenTheoTrangThai(p.TrangThai);

                    lvDanhSachPhong.Items.Add(item);
                }
            }

            lvDanhSachPhong.EndUpdate();
        }

        private void TvSoDoTang_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag == null) return;
            string tag = e.Node.Tag.ToString();

            if (tag == "ROOT")
            {
                cboLocTang.SelectedIndex = 0;
                ApDungBoLoc();
            }
            else if (tag.StartsWith("TANG:"))
            {
                int tang = int.Parse(tag.Substring(5));
                List<Phong> ds = _phongBus.TimKiemPhong(tang, "", "");
                HienThiLenListView(ds);
                OnStatusMessage?.Invoke("Đang xem danh sách phòng Tầng " + tang);
            }
            else if (tag.StartsWith("PHONG:"))
            {
                string maPhong = tag.Substring(6);
                _maPhongDangChon = maPhong;
                Phong p = _phongBus.GetAllPhong().FirstOrDefault(x => x.MaPhong == maPhong);
                if (p != null)
                {
                    HienThiLenListView(new List<Phong> { p });
                    lblPhongDangChon.Text = string.Format("Đang chọn: {0} ({1}) - Trạng thái: [{2}] - Giá ngày: {3:N0} đ",
                        p.TenPhong, p.TenLoai, p.TrangThai, p.DonGiaNgay);
                }
            }
        }

        private void LvDanhSachPhong_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvDanhSachPhong.SelectedItems.Count == 0) return;
            if (lvDanhSachPhong.SelectedItems[0].Tag is Phong p)
            {
                _maPhongDangChon = p.MaPhong;
                lblPhongDangChon.Text = string.Format("Đang chọn: {0} ({1}) - Trạng thái: [{2}] - Nhấn nút bên phải hoặc đúp chuột để xử lý nghiệp vụ!",
                    p.TenPhong, p.TenLoai, p.TrangThai);
            }
        }

        private Color LayMauNenTheoTrangThai(string trangThai)
        {
            if (string.Equals(trangThai, "Trống", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(222, 247, 230); // Xanh lá nhạt
            if (string.Equals(trangThai, "Đang thuê", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(255, 224, 224); // Đỏ hồng nhạt
            if (string.Equals(trangThai, "Đang dọn", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(255, 246, 210); // Vàng nhạt
            return Color.FromArgb(232, 235, 240);     // Xám bảo trì
        }

        private Color LayMauChuTheoTrangThai(string trangThai)
        {
            if (string.Equals(trangThai, "Trống", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(20, 120, 50);
            if (string.Equals(trangThai, "Đang thuê", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(185, 30, 30);
            if (string.Equals(trangThai, "Đang dọn", StringComparison.OrdinalIgnoreCase))
                return Color.FromArgb(175, 115, 0);
            return Color.DimGray;
        }

        private class ComboItem
        {
            public string Value { get; }
            public string Text { get; }
            public ComboItem(string value, string text)
            {
                Value = value;
                Text = text;
            }
            public override string ToString() => Text;
        }
    }
}
