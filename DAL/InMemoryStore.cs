using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.DAL
{
    /// <summary>
    /// Kho dữ liệu trong bộ nhớ (Minh họa học phần: MÔ HÌNH 3 LỚP CHƯA KẾT NỐI CSDL).
    /// Đồng thời đóng vai trò dữ liệu dự phòng khi máy chạy chưa bật dịch vụ SQL Server.
    /// </summary>
    public static class InMemoryStore
    {
        public static List<LoaiPhong> LoaiPhongs { get; private set; }
        public static List<Phong> Phongs { get; private set; }
        public static List<KhachHang> KhachHangs { get; private set; }
        public static List<NhanVien> NhanViens { get; private set; }
        public static List<DichVu> DichVus { get; private set; }
        public static List<PhieuThuePhong> PhieuThues { get; private set; }
        public static List<ChiTietSuDungDichVu> ChiTietDichVus { get; private set; }
        public static List<HoaDon> HoaDons { get; private set; }

        static InMemoryStore()
        {
            ResetSampleData();
        }

        public static void ResetSampleData()
        {
            LoaiPhongs = new List<LoaiPhong>
            {
                new LoaiPhong("LP_STD", "Phòng Tiêu Chuẩn (Standard)", 450000m, 120000m, 2, "1 giường đôi, đầy đủ tiện nghi cơ bản"),
                new LoaiPhong("LP_DLX", "Phòng Cao Cấp (Deluxe)", 750000m, 180000m, 3, "View ban công thoáng mát, bồn tắm nằm"),
                new LoaiPhong("LP_VIP", "Phòng Tổng Thống (VIP Suite)", 1400000m, 320000m, 4, "Phòng khách riêng, minibar miễn phí trà/cà phê")
            };

            Phongs = new List<Phong>
            {
                new Phong("P101", "Phòng 101", 1, "LP_STD", "Phòng Tiêu Chuẩn (Standard)", 450000m, 120000m, 2, "Đang thuê", "Khách nhận phòng sáng nay"),
                new Phong("P102", "Phòng 102", 1, "LP_STD", "Phòng Tiêu Chuẩn (Standard)", 450000m, 120000m, 2, "Trống", "Sẵn sàng đón khách"),
                new Phong("P103", "Phòng 103", 1, "LP_DLX", "Phòng Cao Cấp (Deluxe)", 750000m, 180000m, 3, "Trống", "Gần sảnh thang máy"),
                new Phong("P201", "Phòng 201", 2, "LP_STD", "Phòng Tiêu Chuẩn (Standard)", 450000m, 120000m, 2, "Trống", "Yên tĩnh"),
                new Phong("P202", "Phòng 202", 2, "LP_DLX", "Phòng Cao Cấp (Deluxe)", 750000m, 180000m, 3, "Đang thuê", "Đoàn khách công tác"),
                new Phong("P203", "Phòng 203", 2, "LP_DLX", "Phòng Cao Cấp (Deluxe)", 750000m, 180000m, 3, "Đang dọn", "Khách vừa trả phòng"),
                new Phong("P301", "Phòng 301", 3, "LP_VIP", "Phòng Tổng Thống (VIP Suite)", 1400000m, 320000m, 4, "Trống", "View toàn cảnh thành phố"),
                new Phong("P302", "Phòng 302", 3, "LP_VIP", "Phòng Tổng Thống (VIP Suite)", 1400000m, 320000m, 4, "Bảo trì", "Đang bảo dưỡng điều hòa")
            };

            KhachHangs = new List<KhachHang>
            {
                new KhachHangVIP("KH01", "Nguyễn Văn An", "001099012345", "0901234567", "Hà Nội", 150),
                new KhachHang("KH02", "Trần Thị Bích Ngọc", "001098054321", "0912345678", "Đà Nẵng", "Thường", 20),
                new KhachHangVIP("KH03", "Lê Hoàng Nam", "079095011223", "0987654321", "TP. Hồ Chí Minh", 220),
                new KhachHang("KH04", "Phạm Minh Tuấn", "036097099887", "0933445566", "Hải Phòng", "Thường", 10)
            };

            NhanViens = new List<NhanVien>
            {
                new NhanVien("NV01", "Hoàng Quản Trị", "001090000111", "0999888777", "admin", "123", "Quản lý", 15000000m),
                new NhanVien("NV02", "Mai Lễ Tân", "001095000222", "0977666555", "letan", "123", "Lễ tân", 8500000m)
            };

            DichVus = new List<DichVu>
            {
                new DichVu("DV01", "Nước khoáng Lavie 500ml", "Đồ uống & Minibar", "Chai", 15000m, 120),
                new DichVu("DV02", "Bò húc RedBull", "Đồ uống & Minibar", "Lon", 25000m, 80),
                new DichVu("DV03", "Mì ly hải sản", "Ẩm thực & Ăn sáng", "Ly", 30000m, 60),
                new DichVu("DV04", "Buffet sáng tiêu chuẩn", "Ẩm thực & Ăn sáng", "Suất", 120000m, 100),
                new DichVu("DV05", "Giặt ủi nhanh lấy ngay", "Giặt ủi & Tiện ích", "Kg", 50000m, 500),
                new DichVu("DV06", "Thuê xe máy theo ngày", "Giặt ủi & Tiện ích", "Ngày", 150000m, 15)
            };

            PhieuThues = new List<PhieuThuePhong>
            {
                new PhieuThuePhong
                {
                    MaPhieuThue = 1,
                    MaKH = "KH01",
                    TenKhachHang = "Nguyễn Văn An",
                    LoaiKhach = "VIP",
                    MaNV = "NV02",
                    MaPhong = "P101",
                    TenPhong = "Phòng 101",
                    NgayNhanPhong = DateTime.Now.AddDays(-2),
                    NgayTraDuKien = DateTime.Now.AddDays(1),
                    NgayTraThucTe = null,
                    HinhThucThue = "Theo ngày",
                    TienDatCoc = 200000m,
                    TrangThaiPhieu = "Đang thuê"
                },
                new PhieuThuePhong
                {
                    MaPhieuThue = 2,
                    MaKH = "KH02",
                    TenKhachHang = "Trần Thị Bích Ngọc",
                    LoaiKhach = "Thường",
                    MaNV = "NV02",
                    MaPhong = "P202",
                    TenPhong = "Phòng 202",
                    NgayNhanPhong = DateTime.Now.AddDays(-1),
                    NgayTraDuKien = DateTime.Now.AddDays(2),
                    NgayTraThucTe = null,
                    HinhThucThue = "Theo ngày",
                    TienDatCoc = 300000m,
                    TrangThaiPhieu = "Đang thuê"
                }
            };

            ChiTietDichVus = new List<ChiTietSuDungDichVu>
            {
                new ChiTietSuDungDichVu
                {
                    MaChiTiet = 1,
                    MaPhieuThue = 1,
                    MaDV = "DV01",
                    TenDV = "Nước khoáng Lavie 500ml",
                    DonViTinh = "Chai",
                    SoLuong = 2,
                    DonGiaLucGoi = 15000m,
                    ThoiGianGoi = DateTime.Now.AddHours(-10)
                },
                new ChiTietSuDungDichVu
                {
                    MaChiTiet = 2,
                    MaPhieuThue = 1,
                    MaDV = "DV04",
                    TenDV = "Buffet sáng tiêu chuẩn",
                    DonViTinh = "Suất",
                    SoLuong = 2,
                    DonGiaLucGoi = 120000m,
                    ThoiGianGoi = DateTime.Now.AddHours(-5)
                },
                new ChiTietSuDungDichVu
                {
                    MaChiTiet = 3,
                    MaPhieuThue = 2,
                    MaDV = "DV02",
                    TenDV = "Bò húc RedBull",
                    DonViTinh = "Lon",
                    SoLuong = 3,
                    DonGiaLucGoi = 25000m,
                    ThoiGianGoi = DateTime.Now.AddHours(-3)
                },
                new ChiTietSuDungDichVu
                {
                    MaChiTiet = 4,
                    MaPhieuThue = 2,
                    MaDV = "DV05",
                    TenDV = "Giặt ủi nhanh lấy ngay",
                    DonViTinh = "Kg",
                    SoLuong = 2,
                    DonGiaLucGoi = 50000m,
                    ThoiGianGoi = DateTime.Now.AddHours(-2)
                }
            };

            HoaDons = new List<HoaDon>
            {
                new HoaDon
                {
                    MaHD = 1,
                    MaPhieuThue = 100,
                    TenKhachHang = "Lê Hoàng Nam",
                    TenPhong = "Phòng 301",
                    MaNV = "NV01",
                    NgayLap = DateTime.Now.AddDays(-3),
                    TienPhong = 1400000m,
                    TienDichVu = 240000m,
                    PhuThu = 0m,
                    GiamGia = 140000m,
                    TongThanhToan = 1500000m,
                    GhiChu = "Khách VIP giảm 10% tiền phòng"
                }
            };
        }

        public static void SyncRoomTypeInfo(Phong p)
        {
            LoaiPhong lp = LoaiPhongs.FirstOrDefault(x => string.Equals(x.MaLoai, p.MaLoai, StringComparison.OrdinalIgnoreCase));
            if (lp != null)
            {
                p.TenLoai = lp.TenLoai;
                p.DonGiaNgay = lp.DonGiaNgay;
                p.DonGiaGio = lp.DonGiaGio;
                p.SoNguoiToiDa = lp.SoNguoiToiDa;
            }
        }
    }
}
