using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeTai18_QuanLyKhachSan.DAL;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.BUS
{
    /// <summary>
    /// Tầng BUS (Business Logic Layer) xử lý nghiệp vụ Phòng & Loại phòng.
    /// Liên kết trực tiếp giữa tầng GUI (frmSoDoPhong, frmQuanLyPhongMasterDetail) và PhongDAL.
    /// </summary>
    public class PhongBUS
    {
        private readonly PhongDAL _dal = new PhongDAL();

        public List<LoaiPhong> GetAllLoaiPhong()
        {
            return _dal.GetAllLoaiPhong();
        }

        public List<Phong> GetAllPhong()
        {
            return _dal.GetAll();
        }

        public List<Phong> TimKiemPhong(int tang, string maLoai, string trangThai)
        {
            return _dal.TimKiemPhongBangDataReader(tang, maLoai, trangThai);
        }

        public int DemSoPhongTheoTrangThai(string trangThai)
        {
            return _dal.DemSoPhongTheoTrangThai(trangThai);
        }

        public DataSet GetMasterDetailDataSet()
        {
            return _dal.GetMasterDetailLoaiPhongVaPhong();
        }

        /// <summary>
        /// Nhóm danh sách phòng theo Tầng để hiển thị lên điều khiển TreeView.
        /// </summary>
        public Dictionary<int, List<Phong>> GetPhongNhomTheoTang()
        {
            List<Phong> all = _dal.GetAll();
            return all
                .GroupBy(p => p.Tang)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.OrderBy(p => p.MaPhong).ToList());
        }

        private void ValidatePhong(Phong p)
        {
            if (p == null)
                throw new ArgumentNullException(nameof(p));
            if (string.IsNullOrWhiteSpace(p.MaPhong))
                throw new ArgumentException("Mã phòng không được để trống.");
            if (string.IsNullOrWhiteSpace(p.TenPhong))
                throw new ArgumentException("Tên phòng không được để trống.");
            if (p.Tang <= 0 || p.Tang > 50)
                throw new ArgumentException("Số tầng phải nằm trong khoảng từ 1 đến 50.");
            if (string.IsNullOrWhiteSpace(p.MaLoai))
                throw new ArgumentException("Vui lòng chọn loại phòng.");
        }

        public void ThemPhong(Phong p)
        {
            ValidatePhong(p);
            p.MaPhong = p.MaPhong.Trim().ToUpperInvariant();
            p.TenPhong = p.TenPhong.Trim();

            if (!_dal.Insert(p))
            {
                throw new InvalidOperationException("Không thể thêm phòng (Mã phòng có thể đã tồn tại: " + p.MaPhong + ").");
            }
        }

        public void CapNhatPhong(Phong p)
        {
            ValidatePhong(p);
            p.MaPhong = p.MaPhong.Trim().ToUpperInvariant();
            p.TenPhong = p.TenPhong.Trim();

            if (!_dal.Update(p))
            {
                throw new InvalidOperationException("Không tìm thấy mã phòng cần cập nhật: " + p.MaPhong);
            }
        }

        public void XoaPhong(string maPhong)
        {
            if (string.IsNullOrWhiteSpace(maPhong))
                throw new ArgumentException("Vui lòng chọn mã phòng cần xóa.");

            if (!_dal.Delete(maPhong.Trim()))
            {
                throw new InvalidOperationException("Không thể xóa phòng: " + maPhong);
            }
        }

        public void ThemLoaiPhong(LoaiPhong lp)
        {
            if (lp == null)
                throw new ArgumentNullException(nameof(lp));
            if (string.IsNullOrWhiteSpace(lp.MaLoai) || string.IsNullOrWhiteSpace(lp.TenLoai))
                throw new ArgumentException("Mã loại phòng và Tên loại phòng không được để trống.");
            if (lp.DonGiaNgay <= 0 || lp.DonGiaGio <= 0)
                throw new ArgumentException("Đơn giá ngày và đơn giá giờ phải lớn hơn 0.");

            lp.MaLoai = lp.MaLoai.Trim().ToUpperInvariant();
            lp.TenLoai = lp.TenLoai.Trim();
            if (!_dal.InsertLoaiPhong(lp))
                throw new InvalidOperationException("Mã loại phòng đã tồn tại: " + lp.MaLoai);
        }

        public void CapNhatLoaiPhong(LoaiPhong lp)
        {
            if (lp == null)
                throw new ArgumentNullException(nameof(lp));
            if (string.IsNullOrWhiteSpace(lp.MaLoai) || string.IsNullOrWhiteSpace(lp.TenLoai))
                throw new ArgumentException("Mã loại phòng và Tên loại phòng không được để trống.");
            if (lp.DonGiaNgay <= 0 || lp.DonGiaGio <= 0)
                throw new ArgumentException("Đơn giá ngày và đơn giá giờ phải lớn hơn 0.");

            if (!_dal.UpdateLoaiPhong(lp))
                throw new InvalidOperationException("Không tìm thấy mã loại phòng: " + lp.MaLoai);
        }
    }
}
