using System;

namespace DeTai18_QuanLyKhachSan.Models
{
    /// <summary>
    /// Interface quy định chuẩn hiển thị thông tin tóm tắt cho mọi đối tượng trong khách sạn.
    /// Minh họa kỹ thuật INTERFACE trong lập trình hướng đối tượng (OOP).
    /// </summary>
    public interface IHienThiThongTin
    {
        string LayMaDinhDanh();
        string LayMoTaChiTiet();
    }

    /// <summary>
    /// Interface quy định chuẩn cho các khoản mục có tính thành tiền (Dịch vụ, Phiếu thuê...).
    /// </summary>
    public interface ITinhTien
    {
        decimal TinhThanhTien();
    }
}
