using System;

namespace DeTai18_QuanLyKhachSan.Models
{
    /// <summary>
    /// Thực thể Loại phòng (Master trong quan hệ Master-Detail với Phòng).
    /// </summary>
    public class LoaiPhong : IHienThiThongTin
    {
        public string MaLoai { get; set; }
        public string TenLoai { get; set; }
        public decimal DonGiaNgay { get; set; }
        public decimal DonGiaGio { get; set; }
        public int SoNguoiToiDa { get; set; }
        public string MoTa { get; set; }

        public LoaiPhong()
        {
        }

        public LoaiPhong(string maLoai, string tenLoai, decimal donGiaNgay, decimal donGiaGio, int soNguoiToiDa, string moTa)
        {
            MaLoai = maLoai;
            TenLoai = tenLoai;
            DonGiaNgay = donGiaNgay;
            DonGiaGio = donGiaGio;
            SoNguoiToiDa = soNguoiToiDa;
            MoTa = moTa;
        }

        public string LayMaDinhDanh() => MaLoai;

        public string LayMoTaChiTiet()
        {
            return string.Format("{0} ({1:N0} đ/ngày - Tối đa {2} người)", TenLoai, DonGiaNgay, SoNguoiToiDa);
        }

        public LoaiPhong Clone()
        {
            return new LoaiPhong(MaLoai, TenLoai, DonGiaNgay, DonGiaGio, SoNguoiToiDa, MoTa);
        }
    }

    /// <summary>
    /// Thực thể Phòng khách sạn (Detail thuộc LoaiPhong).
    /// </summary>
    public class Phong : IHienThiThongTin
    {
        public string MaPhong { get; set; }
        public string TenPhong { get; set; }
        public int Tang { get; set; }
        public string MaLoai { get; set; }
        public string TenLoai { get; set; }
        public decimal DonGiaNgay { get; set; }
        public decimal DonGiaGio { get; set; }
        public int SoNguoiToiDa { get; set; }
        public string TrangThai { get; set; } // "Trống", "Đang thuê", "Đang dọn", "Bảo trì"
        public string GhiChu { get; set; }

        public Phong()
        {
            TrangThai = "Trống";
        }

        public Phong(string maPhong, string tenPhong, int tang, string maLoai, string tenLoai,
            decimal donGiaNgay, decimal donGiaGio, int soNguoiToiDa, string trangThai, string ghiChu)
        {
            MaPhong = maPhong;
            TenPhong = tenPhong;
            Tang = tang;
            MaLoai = maLoai;
            TenLoai = tenLoai;
            DonGiaNgay = donGiaNgay;
            DonGiaGio = donGiaGio;
            SoNguoiToiDa = soNguoiToiDa;
            TrangThai = trangThai;
            GhiChu = ghiChu;
        }

        public string LayMaDinhDanh() => MaPhong;

        public string LayMoTaChiTiet()
        {
            return string.Format("{0} - Tầng {1} [{2}] - {3}", TenPhong, Tang, TrangThai, TenLoai);
        }

        public Phong Clone()
        {
            return new Phong(MaPhong, TenPhong, Tang, MaLoai, TenLoai, DonGiaNgay, DonGiaGio, SoNguoiToiDa, TrangThai, GhiChu);
        }
    }
}
