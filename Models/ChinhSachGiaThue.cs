using System;

namespace DeTai18_QuanLyKhachSan.Models
{
    /// <summary>
    /// Lớp trừu tượng (Abstract Class) định nghĩa chính sách tính tiền thuê phòng.
    /// Minh họa ĐA HÌNH (Polymorphism) trong nghiệp vụ tính tiền khách sạn:
    /// Thuê theo ngày và Thuê theo giờ có công thức tính tiền & phụ thu hoàn toàn khác nhau.
    /// </summary>
    public abstract class ChinhSachGiaThue
    {
        public abstract string TenHinhThuc { get; }

        /// <summary>
        /// Phương thức trừu tượng tính tiền phòng và phụ thu dựa trên thời gian nhận - trả.
        /// </summary>
        public abstract decimal TinhTienPhong(
            DateTime ngayNhan,
            DateTime ngayTra,
            decimal donGiaNgay,
            decimal donGiaGio,
            out decimal phuThu,
            out string dienGiaiChiTiet);
    }

    /// <summary>
    /// Lớp con kế thừa ChinhSachGiaThue: Tính tiền thuê phòng THEO NGÀY ĐÊM.
    /// </summary>
    public class ThueTheoNgayStrategy : ChinhSachGiaThue
    {
        public override string TenHinhThuc => "Theo ngày";

        public override decimal TinhTienPhong(
            DateTime ngayNhan,
            DateTime ngayTra,
            decimal donGiaNgay,
            decimal donGiaGio,
            out decimal phuThu,
            out string dienGiaiChiTiet)
        {
            TimeSpan thoiGianO = ngayTra - ngayNhan;
            int soNgay = (int)Math.Ceiling(thoiGianO.TotalHours / 24.0);
            if (soNgay < 1) soNgay = 1;

            phuThu = 0m;
            // Nếu trả phòng muộn sau 12h trưa của ngày trả và vượt quá số ngày chẵn > 2 tiếng thì tính phụ thu 30%
            double gioLe = thoiGianO.TotalHours - ((soNgay - 1) * 24.0);
            if (soNgay > 1 && gioLe > 2 && gioLe <= 6)
            {
                phuThu = Math.Round(donGiaNgay * 0.30m, 0);
            }

            decimal tienPhongChinh = soNgay * donGiaNgay;
            dienGiaiChiTiet = string.Format("Thuê theo ngày: {0} ngày x {1:N0} đ/ngày", soNgay, donGiaNgay);
            return tienPhongChinh;
        }
    }

    /// <summary>
    /// Lớp con kế thừa ChinhSachGiaThue: Tính tiền thuê phòng THEO GIỜ.
    /// </summary>
    public class ThueTheoGioStrategy : ChinhSachGiaThue
    {
        public override string TenHinhThuc => "Theo giờ";

        public override decimal TinhTienPhong(
            DateTime ngayNhan,
            DateTime ngayTra,
            decimal donGiaNgay,
            decimal donGiaGio,
            out decimal phuThu,
            out string dienGiaiChiTiet)
        {
            TimeSpan thoiGianO = ngayTra - ngayNhan;
            int soGio = (int)Math.Ceiling(thoiGianO.TotalHours);
            if (soGio < 1) soGio = 1;

            phuThu = 0m;
            // Giờ đầu tính 100% đơn giá giờ, từ giờ thứ 2 trở đi mỗi giờ tính 70% đơn giá giờ
            decimal tienPhongChinh;
            if (soGio == 1)
            {
                tienPhongChinh = donGiaGio;
            }
            else
            {
                tienPhongChinh = donGiaGio + (soGio - 1) * Math.Round(donGiaGio * 0.70m, 0);
            }

            // Nếu tiền thuê theo giờ vượt quá giá 1 ngày thì quy đổi trần về giá ngày
            if (donGiaNgay > 0 && tienPhongChinh > donGiaNgay)
            {
                phuThu = Math.Round(donGiaGio * 0.20m, 0);
            }

            dienGiaiChiTiet = string.Format("Thuê theo giờ: {0} giờ (Giờ đầu {1:N0} đ)", soGio, donGiaGio);
            return tienPhongChinh;
        }
    }
}
