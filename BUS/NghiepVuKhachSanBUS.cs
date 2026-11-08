using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeTai18_QuanLyKhachSan.DAL;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.BUS
{
    /// <summary>
    /// Lưu phiên đăng nhập của nhân viên hiện hành để liên kết giữa các Form (Multi-Form Session).
    /// </summary>
    public static class PhienDangNhap
    {
        public static NhanVien NhanVienHienTai { get; set; }

        public static string TenHienThi =>
            NhanVienHienTai != null
                ? string.Format("{0} ({1})", NhanVienHienTai.HoTen, NhanVienHienTai.ChucVu)
                : "Chưa đăng nhập";

        public static bool LaQuanLy =>
            NhanVienHienTai != null &&
            string.Equals(NhanVienHienTai.ChucVu, "Quản lý", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Kết quả tính toán chi tiết hóa đơn tạm tính (Phục vụ hiển thị lên Form Thanh toán).
    /// </summary>
    public class KetQuaTinhTienHoaDon
    {
        public decimal TienPhong { get; set; }
        public decimal PhuThu { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TyLeGiamGia { get; set; }
        public decimal SoTienGiamGia { get; set; }
        public decimal TienDatCoc { get; set; }
        public decimal TongCanThanhToan { get; set; }
        public string DienGiaiChiTiet { get; set; }
    }

    /// <summary>
    /// Tầng BUS trung tâm kết nối các nghiệp vụ:
    /// Đăng nhập -> Nhận phòng (Check-In) -> Gọi dịch vụ -> Tính tiền Đa hình -> Thanh toán (Check-Out) -> Báo cáo.
    /// </summary>
    public class NghiepVuKhachSanBUS
    {
        private readonly PhieuThueVaHoaDonDAL _dal = new PhieuThueVaHoaDonDAL();
        private readonly PhongDAL _phongDal = new PhongDAL();
        private readonly KhachHangDAL _khachHangDal = new KhachHangDAL();

        public NhanVien KiemTraDangNhap(string taiKhoan, string matKhau)
        {
            if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(matKhau))
                throw new ArgumentException("Vui lòng nhập đầy đủ Tài khoản và Mật khẩu.");

            return _dal.DangNhap(taiKhoan.Trim(), matKhau.Trim());
        }

        public List<PhieuThuePhong> GetDanhSachPhieuThue(string trangThaiLoc = "")
        {
            return _dal.GetAllPhieuThue(trangThaiLoc);
        }

        public List<ChiTietSuDungDichVu> GetChiTietDichVu(int maPhieuThue)
        {
            return _dal.GetChiTietDichVuByPhieu(maPhieuThue);
        }

        /// <summary>
        /// Thực hiện Nhận phòng (Check-In) thông qua Transaction.
        /// </summary>
        public int NhanPhong(string maKH, string maPhong, DateTime ngayNhan, DateTime ngayTraDuKien, string hinhThucThue, decimal tienDatCoc)
        {
            if (string.IsNullOrWhiteSpace(maKH))
                throw new ArgumentException("Vui lòng chọn khách hàng nhận phòng.");
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("Vui lòng chọn phòng cần nhận.");
            if (ngayTraDuKien <= ngayNhan)
                throw new ArgumentException("Thời gian trả phòng dự kiến phải lớn hơn thời gian nhận phòng.");
            if (tienDatCoc < 0)
                throw new ArgumentException("Tiền đặt cọc không được âm.");

            string maNV = PhienDangNhap.NhanVienHienTai != null ? PhienDangNhap.NhanVienHienTai.MaNV : "NV01";

            PhieuThuePhong pt = new PhieuThuePhong
            {
                MaKH = maKH,
                MaNV = maNV,
                MaPhong = maPhong,
                NgayNhanPhong = ngayNhan,
                NgayTraDuKien = ngayTraDuKien,
                HinhThucThue = string.IsNullOrWhiteSpace(hinhThucThue) ? "Theo ngày" : hinhThucThue,
                TienDatCoc = tienDatCoc,
                TrangThaiPhieu = "Đang thuê"
            };

            return _dal.NhanPhongTransaction(pt);
        }

        /// <summary>
        /// Gọi thêm dịch vụ cho phòng đang thuê thông qua Transaction.
        /// </summary>
        public void GoiDichVuChoPhieuThue(int maPhieuThue, string maDV, int soLuong)
        {
            if (maPhieuThue <= 0)
                throw new ArgumentException("Vui lòng chọn một phiếu thuê phòng đang hoạt động.");
            if (string.IsNullOrWhiteSpace(maDV))
                throw new ArgumentException("Vui lòng chọn dịch vụ cần thêm.");
            if (soLuong <= 0)
                throw new ArgumentException("Số lượng dịch vụ phải lớn hơn 0.");

            _dal.GoiDichVuTransaction(maPhieuThue, maDV, soLuong);
        }

        /// <summary>
        /// Áp dụng ĐA HÌNH (Polymorphism) từ lớp trừu tượng ChinhSachGiaThue và NguoiBase (KhachHang/KhachHangVIP)
        /// để tính toán đầy đủ Tiền phòng, Phụ thu, Tiền dịch vụ và Giảm giá VIP.
        /// </summary>
        public KetQuaTinhTienHoaDon TinhToanHoaDonTamTinh(PhieuThuePhong pt, DateTime thoiDiemTra)
        {
            if (pt == null)
                throw new ArgumentNullException(nameof(pt));

            Phong phong = _phongDal.GetAll().FirstOrDefault(x => string.Equals(x.MaPhong, pt.MaPhong, StringComparison.OrdinalIgnoreCase));
            decimal giaNgay = phong != null ? phong.DonGiaNgay : 450000m;
            decimal giaGio = phong != null ? phong.DonGiaGio : 120000m;

            // 1. Đa hình chọn chiến lược tính giá (Theo ngày vs Theo giờ)
            ChinhSachGiaThue strategy = string.Equals(pt.HinhThucThue, "Theo giờ", StringComparison.OrdinalIgnoreCase)
                ? (ChinhSachGiaThue)new ThueTheoGioStrategy()
                : new ThueTheoNgayStrategy();

            decimal phuThu;
            string dienGiaiPhong;
            decimal tienPhong = strategy.TinhTienPhong(pt.NgayNhanPhong, thoiDiemTra, giaNgay, giaGio, out phuThu, out dienGiaiPhong);

            // 2. Tính tổng tiền dịch vụ từ danh sách Detail (Thực thi Interface ITinhTien)
            List<ChiTietSuDungDichVu> dsDichVu = _dal.GetChiTietDichVuByPhieu(pt.MaPhieuThue);
            decimal tienDichVu = dsDichVu.Sum(x => x.TinhThanhTien());

            // 3. Đa hình tính tỷ lệ giảm giá theo đối tượng Khách hàng (KhachHang vs KhachHangVIP)
            KhachHang kh = _khachHangDal.GetAll().FirstOrDefault(x => string.Equals(x.MaKH, pt.MaKH, StringComparison.OrdinalIgnoreCase))
                           ?? KhachHangDAL.TaoDoiTuongDaHinh(pt.MaKH, pt.TenKhachHang, "000000000", "0900000000", "", pt.LoaiKhach, 0);

            decimal tyLeGiam = kh.TinhTyLeGiamGia();
            decimal soTienGiam = Math.Round((tienPhong + phuThu) * tyLeGiam, 0);

            decimal tongThanhToan = (tienPhong + phuThu + tienDichVu) - soTienGiam - pt.TienDatCoc;
            if (tongThanhToan < 0) tongThanhToan = 0;

            return new KetQuaTinhTienHoaDon
            {
                TienPhong = tienPhong,
                PhuThu = phuThu,
                TienDichVu = tienDichVu,
                TyLeGiamGia = tyLeGiam,
                SoTienGiamGia = soTienGiam,
                TienDatCoc = pt.TienDatCoc,
                TongCanThanhToan = tongThanhToan,
                DienGiaiChiTiet = string.Format("{0} | {1} (Ưu đãi: {2:0}%)", dienGiaiPhong, kh.LayVaiTro(), tyLeGiam * 100m)
            };
        }

        /// <summary>
        /// Xác nhận Thanh toán & Trả phòng bằng SqlTransaction.
        /// </summary>
        public int ThanhToanVaTraPhong(PhieuThuePhong pt, KetQuaTinhTienHoaDon bill, string ghiChu)
        {
            if (pt == null)
                throw new ArgumentNullException(nameof(pt));
            if (string.Equals(pt.TrangThaiPhieu, "Đã thanh toán", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Phiếu thuê này đã được thanh toán trước đó!");

            string maNV = PhienDangNhap.NhanVienHienTai != null ? PhienDangNhap.NhanVienHienTai.MaNV : "NV01";

            HoaDon hd = new HoaDon
            {
                MaPhieuThue = pt.MaPhieuThue,
                TenKhachHang = pt.TenKhachHang,
                TenPhong = pt.TenPhong,
                MaNV = maNV,
                NgayLap = DateTime.Now,
                TienPhong = bill.TienPhong,
                TienDichVu = bill.TienDichVu,
                PhuThu = bill.PhuThu,
                GiamGia = bill.SoTienGiamGia,
                TongThanhToan = bill.TongCanThanhToan,
                GhiChu = string.IsNullOrWhiteSpace(ghiChu) ? bill.DienGiaiChiTiet : ghiChu
            };

            return _dal.ThanhToanTraPhongTransaction(hd, pt.MaPhong, pt.MaKH);
        }

        public DataTable GetBaoCaoDoanhThu(DateTime tuNgay, DateTime denNgay)
        {
            return _dal.GetBaoCaoDoanhThu(tuNgay.Date, denNgay.Date.AddDays(1).AddSeconds(-1));
        }
    }
}
