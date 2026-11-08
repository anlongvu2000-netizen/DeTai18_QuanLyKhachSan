using System;

namespace DeTai18_QuanLyKhachSan.Models
{
    /// <summary>
    /// Lớp trừu tượng (Abstract Class) biểu diễn thông tin chung của một Con người (Khách hàng, Nhân viên).
    /// Minh họa: LỚP TRỪU TƯỢNG, ĐÓNG GÓI, KẾ THỪA & ĐA HÌNH.
    /// </summary>
    public abstract class NguoiBase : IHienThiThongTin
    {
        public string MaDinhDanh { get; set; }
        public string HoTen { get; set; }
        public string CCCD { get; set; }
        public string SoDienThoai { get; set; }

        protected NguoiBase()
        {
        }

        protected NguoiBase(string maDinhDanh, string hoTen, string cccd, string soDienThoai)
        {
            MaDinhDanh = maDinhDanh;
            HoTen = hoTen;
            CCCD = cccd;
            SoDienThoai = soDienThoai;
        }

        // Phương thức trừu tượng bắt buộc các lớp con (KhachHang, NhanVien) phải nạp chồng (override)
        public abstract string LayVaiTro();

        // Phương thức ảo (virtual) cho phép đa hình khi tính tỷ lệ ưu đãi/giảm giá
        public virtual decimal TinhTyLeGiamGia()
        {
            return 0m;
        }

        // Thực thi Interface IHienThiThongTin
        public string LayMaDinhDanh()
        {
            return MaDinhDanh;
        }

        public virtual string LayMoTaChiTiet()
        {
            return string.Format("[{0}] {1} - SĐT: {2} ({3})", MaDinhDanh, HoTen, SoDienThoai, LayVaiTro());
        }
    }
}
