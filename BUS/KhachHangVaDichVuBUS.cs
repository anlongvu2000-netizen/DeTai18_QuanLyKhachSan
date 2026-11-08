using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeTai18_QuanLyKhachSan.DAL;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.BUS
{
    /// <summary>
    /// Tầng BUS quản lý Khách hàng (Kiểm tra dữ liệu, Đa hình Khách VIP/Thường, Lọc ngắt kết nối DataTable/DataView).
    /// </summary>
    public class KhachHangBUS
    {
        private readonly KhachHangDAL _dal = new KhachHangDAL();

        public List<KhachHang> GetAll()
        {
            return _dal.GetAll();
        }

        public DataTable GetDataTableKhachHang()
        {
            return _dal.GetDataTableKhachHang();
        }

        public KhachHang TaoDoiTuongKhachHang(string maKH, string hoTen, string cccd, string sdt, string diaChi, string loaiKhach, int diem)
        {
            return KhachHangDAL.TaoDoiTuongDaHinh(maKH, hoTen, cccd, sdt, diaChi, loaiKhach, diem);
        }

        private void ValidateKhachHang(KhachHang kh)
        {
            if (kh == null)
                throw new ArgumentNullException(nameof(kh));
            if (string.IsNullOrWhiteSpace(kh.MaKH))
                throw new ArgumentException("Mã khách hàng không được để trống.");
            if (string.IsNullOrWhiteSpace(kh.HoTen))
                throw new ArgumentException("Họ tên khách hàng không được để trống.");
            if (string.IsNullOrWhiteSpace(kh.CCCD) || kh.CCCD.Trim().Length < 9 || !kh.CCCD.Trim().All(char.IsDigit))
                throw new ArgumentException("Số CCCD/CMND phải gồm từ 9 đến 12 chữ số.");
            if (string.IsNullOrWhiteSpace(kh.SoDienThoai) || kh.SoDienThoai.Trim().Length < 9 || !kh.SoDienThoai.Trim().All(char.IsDigit))
                throw new ArgumentException("Số điện thoại phải là chuỗi chữ số hợp lệ (tối thiểu 9 số).");
            if (kh.DiemTichLuy < 0)
                throw new ArgumentException("Điểm tích lũy không được nhỏ hơn 0.");
        }

        public void ThemKhachHang(KhachHang kh)
        {
            ValidateKhachHang(kh);
            kh.MaKH = kh.MaKH.Trim().ToUpperInvariant();
            kh.HoTen = kh.HoTen.Trim();
            kh.CCCD = kh.CCCD.Trim();
            kh.SoDienThoai = kh.SoDienThoai.Trim();

            if (!_dal.Insert(kh))
                throw new InvalidOperationException("Không thể thêm khách hàng (Mã KH hoặc CCCD đã tồn tại).");
        }

        public void CapNhatKhachHang(KhachHang kh)
        {
            ValidateKhachHang(kh);
            kh.MaKH = kh.MaKH.Trim().ToUpperInvariant();
            kh.HoTen = kh.HoTen.Trim();
            kh.CCCD = kh.CCCD.Trim();
            kh.SoDienThoai = kh.SoDienThoai.Trim();

            if (!_dal.Update(kh))
                throw new InvalidOperationException("Không tìm thấy mã khách hàng cần cập nhật: " + kh.MaKH);
        }

        public void XoaKhachHang(string maKH)
        {
            if (string.IsNullOrWhiteSpace(maKH))
                throw new ArgumentException("Vui lòng chọn mã khách hàng cần xóa.");
            if (!_dal.Delete(maKH.Trim()))
                throw new InvalidOperationException("Không tìm thấy khách hàng để xóa: " + maKH);
        }
    }

    /// <summary>
    /// Tầng BUS quản lý Dịch vụ khách sạn.
    /// </summary>
    public class DichVuBUS
    {
        private readonly DichVuDAL _dal = new DichVuDAL();

        public List<DichVu> GetAll()
        {
            return _dal.GetAll();
        }

        public DataTable GetDataTableDichVu()
        {
            return _dal.GetDataTableDichVu();
        }

        private void ValidateDichVu(DichVu dv)
        {
            if (dv == null)
                throw new ArgumentNullException(nameof(dv));
            if (string.IsNullOrWhiteSpace(dv.MaDV))
                throw new ArgumentException("Mã dịch vụ không được để trống.");
            if (string.IsNullOrWhiteSpace(dv.TenDV))
                throw new ArgumentException("Tên dịch vụ không được để trống.");
            if (dv.DonGia < 0)
                throw new ArgumentException("Đơn giá dịch vụ phải >= 0.");
            if (dv.SoLuongTon < 0)
                throw new ArgumentException("Số lượng tồn kho phải >= 0.");
        }

        public void ThemDichVu(DichVu dv)
        {
            ValidateDichVu(dv);
            dv.MaDV = dv.MaDV.Trim().ToUpperInvariant();
            dv.TenDV = dv.TenDV.Trim();
            if (!_dal.Insert(dv))
                throw new InvalidOperationException("Mã dịch vụ đã tồn tại: " + dv.MaDV);
        }

        public void CapNhatDichVu(DichVu dv)
        {
            ValidateDichVu(dv);
            dv.MaDV = dv.MaDV.Trim().ToUpperInvariant();
            dv.TenDV = dv.TenDV.Trim();
            if (!_dal.Update(dv))
                throw new InvalidOperationException("Không tìm thấy dịch vụ cần cập nhật: " + dv.MaDV);
        }

        public void XoaDichVu(string maDV)
        {
            if (string.IsNullOrWhiteSpace(maDV))
                throw new ArgumentException("Vui lòng chọn mã dịch vụ cần xóa.");
            if (!_dal.Delete(maDV.Trim()))
                throw new InvalidOperationException("Không thể xóa dịch vụ: " + maDV);
        }
    }
}
