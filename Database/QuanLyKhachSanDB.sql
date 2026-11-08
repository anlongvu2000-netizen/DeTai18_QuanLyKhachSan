-- =============================================================================
-- ĐỀ TÀI 18: XÂY DỰNG PHẦN MỀM QUẢN LÝ KHÁCH SẠN BẰNG CÔNG NGHỆ .NET
-- SCRIPT CƠ SỞ DỮ LIỆU SQL SERVER (BẢNG, RÀNG BUỘC, DỮ LIỆU MẪU & STORED PROCEDURE)
-- =============================================================================

USE master;
GO

IF DB_ID(N'QuanLyKhachSanDB') IS NOT NULL
BEGIN
    ALTER DATABASE QuanLyKhachSanDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QuanLyKhachSanDB;
END
GO

CREATE DATABASE QuanLyKhachSanDB;
GO

USE QuanLyKhachSanDB;
GO

-- 1. BẢNG LOẠI PHÒNG (MASTER CỦA BẢNG PHÒNG)
CREATE TABLE LoaiPhong (
    MaLoai VARCHAR(20) PRIMARY KEY,
    TenLoai NVARCHAR(100) NOT NULL,
    DonGiaNgay DECIMAL(18, 0) NOT NULL CHECK (DonGiaNgay >= 0),
    DonGiaGio DECIMAL(18, 0) NOT NULL CHECK (DonGiaGio >= 0),
    SoNguoiToiDa INT NOT NULL CHECK (SoNguoiToiDa > 0),
    MoTa NVARCHAR(255) NULL
);
GO

-- 2. BẢNG PHÒNG (DETAIL CỦA BẢNG LOẠI PHÒNG)
CREATE TABLE Phong (
    MaPhong VARCHAR(20) PRIMARY KEY,
    TenPhong NVARCHAR(50) NOT NULL,
    Tang INT NOT NULL CHECK (Tang > 0),
    MaLoai VARCHAR(20) NOT NULL,
    TrangThai NVARCHAR(50) NOT NULL DEFAULT N'Trống', -- N'Trống', N'Đang thuê', N'Đang dọn', N'Bảo trì'
    GhiChu NVARCHAR(255) NULL,
    CONSTRAINT FK_Phong_LoaiPhong FOREIGN KEY (MaLoai) REFERENCES LoaiPhong(MaLoai)
);
GO

-- 3. BẢNG KHÁCH HÀNG (MINH HỌA KẾ THỪA & ĐA HÌNH: KHÁCH THƯỜNG / KHÁCH VIP)
CREATE TABLE KhachHang (
    MaKH VARCHAR(20) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    CCCD VARCHAR(20) NOT NULL UNIQUE,
    SoDienThoai VARCHAR(15) NOT NULL,
    DiaChi NVARCHAR(200) NULL,
    LoaiKhach NVARCHAR(30) NOT NULL DEFAULT N'Thường', -- N'Thường', N'VIP'
    DiemTichLuy INT NOT NULL DEFAULT 0
);
GO

-- 4. BẢNG NHÂN VIÊN (KẾ THỪA TỪ LỚP TRỪU TƯỢNG NguoiBase)
CREATE TABLE NhanVien (
    MaNV VARCHAR(20) PRIMARY KEY,
    HoTen NVARCHAR(100) NOT NULL,
    CCCD VARCHAR(20) NOT NULL,
    SoDienThoai VARCHAR(15) NOT NULL,
    TaiKhoan VARCHAR(50) NOT NULL UNIQUE,
    MatKhau VARCHAR(100) NOT NULL,
    ChucVu NVARCHAR(50) NOT NULL, -- N'Quản lý', N'Lễ tân'
    LuongCoBan DECIMAL(18, 0) NOT NULL DEFAULT 0
);
GO

-- 5. BẢNG DỊCH VỤ KHÁCH SẠN (MINIBAR, GIẶT ỦI, ĂN SÁNG)
CREATE TABLE DichVu (
    MaDV VARCHAR(20) PRIMARY KEY,
    TenDV NVARCHAR(100) NOT NULL,
    DanhMuc NVARCHAR(50) NOT NULL,
    DonViTinh NVARCHAR(30) NOT NULL,
    DonGia DECIMAL(18, 0) NOT NULL CHECK (DonGia >= 0),
    SoLuongTon INT NOT NULL CHECK (SoLuongTon >= 0)
);
GO

-- 6. BẢNG PHIẾU THUÊ PHÒNG (MASTER CỦA CHI TIẾT DỊCH VỤ)
CREATE TABLE PhieuThuePhong (
    MaPhieuThue INT IDENTITY(1,1) PRIMARY KEY,
    MaKH VARCHAR(20) NOT NULL,
    MaNV VARCHAR(20) NOT NULL,
    MaPhong VARCHAR(20) NOT NULL,
    NgayNhanPhong DATETIME NOT NULL DEFAULT GETDATE(),
    NgayTraDuKien DATETIME NOT NULL,
    NgayTraThucTe DATETIME NULL,
    HinhThucThue NVARCHAR(30) NOT NULL DEFAULT N'Theo ngày', -- N'Theo ngày', N'Theo giờ'
    TienDatCoc DECIMAL(18, 0) NOT NULL DEFAULT 0,
    TrangThaiPhieu NVARCHAR(30) NOT NULL DEFAULT N'Đang thuê', -- N'Đang thuê', N'Đã thanh toán'
    CONSTRAINT FK_PhieuThue_KhachHang FOREIGN KEY (MaKH) REFERENCES KhachHang(MaKH),
    CONSTRAINT FK_PhieuThue_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV),
    CONSTRAINT FK_PhieuThue_Phong FOREIGN KEY (MaPhong) REFERENCES Phong(MaPhong)
);
GO

-- 7. BẢNG CHI TIẾT SỬ DỤNG DỊCH VỤ (DETAIL CỦA PHIẾU THUÊ PHÒNG)
CREATE TABLE ChiTietSuDungDichVu (
    MaChiTiet INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuThue INT NOT NULL,
    MaDV VARCHAR(20) NOT NULL,
    SoLuong INT NOT NULL CHECK (SoLuong > 0),
    DonGiaLucGoi DECIMAL(18, 0) NOT NULL,
    ThoiGianGoi DATETIME NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_ChiTietDV_PhieuThue FOREIGN KEY (MaPhieuThue) REFERENCES PhieuThuePhong(MaPhieuThue),
    CONSTRAINT FK_ChiTietDV_DichVu FOREIGN KEY (MaDV) REFERENCES DichVu(MaDV)
);
GO

-- 8. BẢNG HÓA ĐƠN THANH TOÁN
CREATE TABLE HoaDon (
    MaHD INT IDENTITY(1,1) PRIMARY KEY,
    MaPhieuThue INT NOT NULL UNIQUE,
    MaNV VARCHAR(20) NOT NULL,
    NgayLap DATETIME NOT NULL DEFAULT GETDATE(),
    TienPhong DECIMAL(18, 0) NOT NULL,
    TienDichVu DECIMAL(18, 0) NOT NULL,
    PhuThu DECIMAL(18, 0) NOT NULL DEFAULT 0,
    GiamGia DECIMAL(18, 0) NOT NULL DEFAULT 0,
    TongThanhToan DECIMAL(18, 0) NOT NULL,
    GhiChu NVARCHAR(255) NULL,
    CONSTRAINT FK_HoaDon_PhieuThue FOREIGN KEY (MaPhieuThue) REFERENCES PhieuThuePhong(MaPhieuThue),
    CONSTRAINT FK_HoaDon_NhanVien FOREIGN KEY (MaNV) REFERENCES NhanVien(MaNV)
);
GO

-- =============================================================================
-- CÁC STORED PROCEDURE (PHỤC VỤ CRUD THAM SỐ HÓA & TÌM KIẾM TRONG ADO.NET)
-- =============================================================================

-- SP 1: Thêm khách hàng mới
CREATE PROCEDURE sp_ThemKhachHang
    @MaKH VARCHAR(20),
    @HoTen NVARCHAR(100),
    @CCCD VARCHAR(20),
    @SoDienThoai VARCHAR(15),
    @DiaChi NVARCHAR(200),
    @LoaiKhach NVARCHAR(30),
    @DiemTichLuy INT
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO KhachHang (MaKH, HoTen, CCCD, SoDienThoai, DiaChi, LoaiKhach, DiemTichLuy)
    VALUES (@MaKH, @HoTen, @CCCD, @SoDienThoai, @DiaChi, @LoaiKhach, @DiemTichLuy);
END
GO

-- SP 2: Cập nhật thông tin khách hàng
CREATE PROCEDURE sp_CapNhatKhachHang
    @MaKH VARCHAR(20),
    @HoTen NVARCHAR(100),
    @CCCD VARCHAR(20),
    @SoDienThoai VARCHAR(15),
    @DiaChi NVARCHAR(200),
    @LoaiKhach NVARCHAR(30),
    @DiemTichLuy INT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE KhachHang
    SET HoTen = @HoTen,
        CCCD = @CCCD,
        SoDienThoai = @SoDienThoai,
        DiaChi = @DiaChi,
        LoaiKhach = @LoaiKhach,
        DiemTichLuy = @DiemTichLuy
    WHERE MaKH = @MaKH;
END
GO

-- SP 3: Xóa khách hàng
CREATE PROCEDURE sp_XoaKhachHang
    @MaKH VARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM KhachHang WHERE MaKH = @MaKH;
END
GO

-- SP 4: Tìm kiếm phòng theo điều kiện (Tầng, Loại phòng, Trạng thái)
CREATE PROCEDURE sp_TimKiemPhong
    @Tang INT = 0,
    @MaLoai VARCHAR(20) = '',
    @TrangThai NVARCHAR(50) = N''
AS
BEGIN
    SET NOCOUNT ON;
    SELECT p.MaPhong, p.TenPhong, p.Tang, p.MaLoai, lp.TenLoai,
           lp.DonGiaNgay, lp.DonGiaGio, lp.SoNguoiToiDa, p.TrangThai, p.GhiChu
    FROM Phong p
    INNER JOIN LoaiPhong lp ON p.MaLoai = lp.MaLoai
    WHERE (@Tang = 0 OR p.Tang = @Tang)
      AND (@MaLoai = '' OR p.MaLoai = @MaLoai)
      AND (@TrangThai = N'' OR p.TrangThai = @TrangThai)
    ORDER BY p.Tang, p.MaPhong;
END
GO

-- SP 5: Thống kê doanh thu hóa đơn theo khoảng thời gian
CREATE PROCEDURE sp_ThongKeDoanhThu
    @TuNgay DATETIME,
    @DenNgay DATETIME
AS
BEGIN
    SET NOCOUNT ON;
    SELECT hd.MaHD, hd.MaPhieuThue, kh.HoTen AS TenKhachHang, p.TenPhong,
           hd.NgayLap, hd.TienPhong, hd.TienDichVu, hd.PhuThu, hd.GiamGia, hd.TongThanhToan
    FROM HoaDon hd
    INNER JOIN PhieuThuePhong pt ON hd.MaPhieuThue = pt.MaPhieuThue
    INNER JOIN KhachHang kh ON pt.MaKH = kh.MaKH
    INNER JOIN Phong p ON pt.MaPhong = p.MaPhong
    WHERE hd.NgayLap BETWEEN @TuNgay AND @DenNgay
    ORDER BY hd.NgayLap DESC;
END
GO

-- =============================================================================
-- DỮ LIỆU MẪU BAN ĐẦU
-- =============================================================================

INSERT INTO LoaiPhong (MaLoai, TenLoai, DonGiaNgay, DonGiaGio, SoNguoiToiDa, MoTa) VALUES
('LP_STD', N'Phòng Tiêu Chuẩn (Standard)', 450000, 120000, 2, N'1 giường đôi, đầy đủ tiện nghi cơ bản'),
('LP_DLX', N'Phòng Cao Cấp (Deluxe)', 750000, 180000, 3, N'View ban công thoáng mát, bồn tắm nằm'),
('LP_VIP', N'Phòng Tổng Thống (VIP Suite)', 1400000, 320000, 4, N'Phòng khách riêng, minibar miễn phí trà/cà phê');

INSERT INTO Phong (MaPhong, TenPhong, Tang, MaLoai, TrangThai, GhiChu) VALUES
('P101', N'Phòng 101', 1, 'LP_STD', N'Đang thuê', N'Khách nhận phòng sáng nay'),
('P102', N'Phòng 102', 1, 'LP_STD', N'Trống', N'Sẵn sàng đón khách'),
('P103', N'Phòng 103', 1, 'LP_DLX', N'Trống', N'Gần sảnh thang máy'),
('P201', N'Phòng 201', 2, 'LP_STD', N'Trống', N'Yên tĩnh'),
('P202', N'Phòng 202', 2, 'LP_DLX', N'Đang thuê', N'Đoàn khách công tác'),
('P203', N'Phòng 203', 2, 'LP_DLX', N'Đang dọn', N'Khách vừa trả phòng'),
('P301', N'Phòng 301', 3, 'LP_VIP', N'Trống', N'View toàn cảnh thành phố'),
('P302', N'Phòng 302', 3, 'LP_VIP', N'Bảo trì', N'Đang bảo dưỡng điều hòa');

INSERT INTO KhachHang (MaKH, HoTen, CCCD, SoDienThoai, DiaChi, LoaiKhach, DiemTichLuy) VALUES
('KH01', N'Nguyễn Văn An', '001099012345', '0901234567', N'Hà Nội', N'VIP', 150),
('KH02', N'Trần Thị Bích Ngọc', '001098054321', '0912345678', N'Đà Nẵng', N'Thường', 20),
('KH03', N'Lê Hoàng Nam', '079095011223', '0987654321', N'TP. Hồ Chí Minh', N'VIP', 220),
('KH04', N'Phạm Minh Tuấn', '036097099887', '0933445566', N'Hải Phòng', N'Thường', 10);

INSERT INTO NhanVien (MaNV, HoTen, CCCD, SoDienThoai, TaiKhoan, MatKhau, ChucVu, LuongCoBan) VALUES
('NV01', N'Hoàng Quản Trị', '001090000111', '0999888777', 'admin', '123', N'Quản lý', 15000000),
('NV02', N'Mai Lễ Tân', '001095000222', '0977666555', 'letan', '123', N'Lễ tân', 8500000);

INSERT INTO DichVu (MaDV, TenDV, DanhMuc, DonViTinh, DonGia, SoLuongTon) VALUES
('DV01', N'Nước khoáng Lavie 500ml', N'Đồ uống & Minibar', N'Chai', 15000, 120),
('DV02', N'Bò húc RedBull', N'Đồ uống & Minibar', N'Lon', 25000, 80),
('DV03', N'Mì ly hải sản', N'Ẩm thực & Ăn sáng', N'Ly', 30000, 60),
('DV04', N'Buffet sáng tiêu chuẩn', N'Ẩm thực & Ăn sáng', N'Suất', 120000, 100),
('DV05', N'Giặt ủi nhanh lấy ngay', N'Giặt ủi & Tiện ích', N'Kg', 50000, 500),
('DV06', N'Thuê xe máy theo ngày', N'Giặt ủi & Tiện ích', N'Ngày', 150000, 15);

-- Phiếu thuê mẫu đang hoạt động cho P101 và P202
INSERT INTO PhieuThuePhong (MaKH, MaNV, MaPhong, NgayNhanPhong, NgayTraDuKien, NgayTraThucTe, HinhThucThue, TienDatCoc, TrangThaiPhieu)
VALUES
('KH01', 'NV02', 'P101', DATEADD(DAY, -2, GETDATE()), DATEADD(DAY, 1, GETDATE()), NULL, N'Theo ngày', 200000, N'Đang thuê'),
('KH02', 'NV02', 'P202', DATEADD(DAY, -1, GETDATE()), DATEADD(DAY, 2, GETDATE()), NULL, N'Theo ngày', 300000, N'Đang thuê');

INSERT INTO ChiTietSuDungDichVu (MaPhieuThue, MaDV, SoLuong, DonGiaLucGoi, ThoiGianGoi)
VALUES
(1, 'DV01', 2, 15000, DATEADD(HOUR, -10, GETDATE())),
(1, 'DV04', 2, 120000, DATEADD(HOUR, -5, GETDATE())),
(2, 'DV02', 3, 25000, DATEADD(HOUR, -3, GETDATE())),
(2, 'DV05', 2, 50000, DATEADD(HOUR, -2, GETDATE()));
GO
