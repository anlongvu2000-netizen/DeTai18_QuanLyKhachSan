using System;

namespace DeTai18_QuanLyKhachSan.Models
{
    /// <summary>
    /// Thực thể Dịch vụ khách sạn (Minibar, Ăn sáng, Giặt ủi).
    /// Thực thi Interface IHienThiThongTin và ITinhTien.
    /// </summary>
    public class DichVu : IHienThiThongTin, ITinhTien
    {
        public string MaDV { get; set; }
        public string TenDV { get; set; }
        public string DanhMuc { get; set; }
        public string DonViTinh { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuongTon { get; set; }

        public decimal GiaTriTonKho => DonGia * SoLuongTon;

        public DichVu()
        {
        }

        public DichVu(string maDV, string tenDV, string danhMuc, string donViTinh, decimal donGia, int soLuongTon)
        {
            MaDV = maDV;
            TenDV = tenDV;
            DanhMuc = danhMuc;
            DonViTinh = donViTinh;
            DonGia = donGia;
            SoLuongTon = soLuongTon;
        }

        public string LayMaDinhDanh() => MaDV;

        public string LayMoTaChiTiet()
        {
            return string.Format("{0} ({1:N0} đ/{2}) - Tồn: {3}", TenDV, DonGia, DonViTinh, SoLuongTon);
        }

        public decimal TinhThanhTien()
        {
            return GiaTriTonKho;
        }

        public DichVu Clone()
        {
            return new DichVu(MaDV, TenDV, DanhMuc, DonViTinh, DonGia, SoLuongTon);
        }
    }

    /// <summary>
    /// Thực thể Phiếu thuê phòng (Master của ChiTietSuDungDichVu).
    /// </summary>
    public class PhieuThuePhong
    {
        public int MaPhieuThue { get; set; }
        public string MaKH { get; set; }
        public string TenKhachHang { get; set; }
        public string LoaiKhach { get; set; }
        public string MaNV { get; set; }
        public string MaPhong { get; set; }
        public string TenPhong { get; set; }
        public DateTime NgayNhanPhong { get; set; }
        public DateTime NgayTraDuKien { get; set; }
        public DateTime? NgayTraThucTe { get; set; }
        public string HinhThucThue { get; set; } // "Theo ngày", "Theo giờ"
        public decimal TienDatCoc { get; set; }
        public string TrangThaiPhieu { get; set; } // "Đang thuê", "Đã thanh toán"
    }

    /// <summary>
    /// Thực thể Chi tiết sử dụng dịch vụ của 1 Phiếu thuê (Detail).
    /// Thực thi Interface ITinhTien.
    /// </summary>
    public class ChiTietSuDungDichVu : ITinhTien
    {
        public int MaChiTiet { get; set; }
        public int MaPhieuThue { get; set; }
        public string MaDV { get; set; }
        public string TenDV { get; set; }
        public string DonViTinh { get; set; }
        public int SoLuong { get; set; }
        public decimal DonGiaLucGoi { get; set; }
        public DateTime ThoiGianGoi { get; set; }

        public decimal ThanhTien => TinhThanhTien();

        public decimal TinhThanhTien()
        {
            return SoLuong * DonGiaLucGoi;
        }
    }

    /// <summary>
    /// Thực thể Hóa đơn thanh toán (Kết quả sau khi Check-out).
    /// </summary>
    public class HoaDon : ITinhTien
    {
        public int MaHD { get; set; }
        public int MaPhieuThue { get; set; }
        public string TenKhachHang { get; set; }
        public string TenPhong { get; set; }
        public string MaNV { get; set; }
        public DateTime NgayLap { get; set; }
        public decimal TienPhong { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal PhuThu { get; set; }
        public decimal GiamGia { get; set; }
        public decimal TongThanhToan { get; set; }
        public string GhiChu { get; set; }

        public decimal TinhThanhTien()
        {
            return TongThanhToan;
        }
    }
}
