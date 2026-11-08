using System;

namespace DeTai18_QuanLyKhachSan.Models
{
    /// <summary>
    /// Lớp KhachHang kế thừa từ lớp trừu tượng NguoiBase.
    /// </summary>
    public class KhachHang : NguoiBase
    {
        public string MaKH
        {
            get => MaDinhDanh;
            set => MaDinhDanh = value;
        }

        public string DiaChi { get; set; }
        public string LoaiKhach { get; set; } // "Thường" hoặc "VIP"
        public int DiemTichLuy { get; set; }

        public KhachHang()
        {
            LoaiKhach = "Thường";
        }

        public KhachHang(string maKH, string hoTen, string cccd, string soDienThoai, string diaChi, string loaiKhach, int diemTichLuy)
            : base(maKH, hoTen, cccd, soDienThoai)
        {
            DiaChi = diaChi;
            LoaiKhach = string.IsNullOrWhiteSpace(loaiKhach) ? "Thường" : loaiKhach;
            DiemTichLuy = diemTichLuy;
        }

        public override string LayVaiTro()
        {
            return "Khách hàng tiêu chuẩn";
        }

        public override decimal TinhTyLeGiamGia()
        {
            // Khách thường: giảm 2% nếu điểm tích lũy >= 100, ngược lại 0%
            return DiemTichLuy >= 100 ? 0.02m : 0m;
        }

        public KhachHang Clone()
        {
            if (string.Equals(LoaiKhach, "VIP", StringComparison.OrdinalIgnoreCase))
            {
                return new KhachHangVIP(MaKH, HoTen, CCCD, SoDienThoai, DiaChi, DiemTichLuy);
            }
            return new KhachHang(MaKH, HoTen, CCCD, SoDienThoai, DiaChi, LoaiKhach, DiemTichLuy);
        }
    }

    /// <summary>
    /// Lớp KhachHangVIP kế thừa từ KhachHang (Kế thừa nhiều cấp & Đa hình).
    /// Override lại phương thức LayVaiTro() và TinhTyLeGiamGia().
    /// </summary>
    public class KhachHangVIP : KhachHang
    {
        public KhachHangVIP()
        {
            LoaiKhach = "VIP";
        }

        public KhachHangVIP(string maKH, string hoTen, string cccd, string soDienThoai, string diaChi, int diemTichLuy)
            : base(maKH, hoTen, cccd, soDienThoai, diaChi, "VIP", diemTichLuy)
        {
        }

        public override string LayVaiTro()
        {
            return "Khách hàng VIP (Ưu đãi đặc biệt)";
        }

        public override decimal TinhTyLeGiamGia()
        {
            // Đa hình: Khách VIP mặc định giảm 10%, nếu điểm >= 200 giảm 15%
            return DiemTichLuy >= 200 ? 0.15m : 0.10m;
        }
    }

    /// <summary>
    /// Lớp NhanVien kế thừa từ lớp trừu tượng NguoiBase.
    /// </summary>
    public class NhanVien : NguoiBase
    {
        public string MaNV
        {
            get => MaDinhDanh;
            set => MaDinhDanh = value;
        }

        public string TaiKhoan { get; set; }
        public string MatKhau { get; set; }
        public string ChucVu { get; set; }
        public decimal LuongCoBan { get; set; }

        public NhanVien()
        {
        }

        public NhanVien(string maNV, string hoTen, string cccd, string soDienThoai, string taiKhoan, string matKhau, string chucVu, decimal luongCoBan)
            : base(maNV, hoTen, cccd, soDienThoai)
        {
            TaiKhoan = taiKhoan;
            MatKhau = matKhau;
            ChucVu = chucVu;
            LuongCoBan = luongCoBan;
        }

        public override string LayVaiTro()
        {
            return "Nhân viên - " + ChucVu;
        }
    }
}
