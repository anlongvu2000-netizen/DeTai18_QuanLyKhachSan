using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.DAL
{
    /// <summary>
    /// Tầng DAL xử lý Xác thực Nhân viên, Đặt/Nhận phòng, Gọi dịch vụ và Thanh toán Hóa đơn.
    /// Minh họa trọng tâm:
    /// 1. MASTER - DETAIL giữa PhieuThuePhong (Master) và ChiTietSuDungDichVu (Detail)
    /// 2. GIAO DỊCH (SqlTransaction - BeginTransaction, Commit, Rollback) đảm bảo toàn vẹn dữ liệu
    /// 3. Gọi Stored Procedure sp_ThongKeDoanhThu và ExecuteScalar
    /// </summary>
    public class PhieuThueVaHoaDonDAL
    {
        public NhanVien DangNhap(string taiKhoan, string matKhau)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                return InMemoryStore.NhanViens.FirstOrDefault(x =>
                    string.Equals(x.TaiKhoan, taiKhoan, StringComparison.OrdinalIgnoreCase) &&
                    x.MatKhau == matKhau);
            }

            string sql = @"SELECT MaNV, HoTen, CCCD, SoDienThoai, TaiKhoan, MatKhau, ChucVu, LuongCoBan
                           FROM NhanVien
                           WHERE TaiKhoan = @TaiKhoan AND MatKhau = @MatKhau";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TaiKhoan", taiKhoan);
                cmd.Parameters.AddWithValue("@MatKhau", matKhau);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new NhanVien(
                            reader["MaNV"].ToString(),
                            reader["HoTen"].ToString(),
                            reader["CCCD"].ToString(),
                            reader["SoDienThoai"].ToString(),
                            reader["TaiKhoan"].ToString(),
                            reader["MatKhau"].ToString(),
                            reader["ChucVu"].ToString(),
                            Convert.ToDecimal(reader["LuongCoBan"])
                        );
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Lấy danh sách tất cả Phiếu thuê phòng (Master).
        /// </summary>
        public List<PhieuThuePhong> GetAllPhieuThue(string trangThaiLoc = "")
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                IEnumerable<PhieuThuePhong> q = InMemoryStore.PhieuThues;
                if (!string.IsNullOrWhiteSpace(trangThaiLoc))
                {
                    q = q.Where(x => string.Equals(x.TrangThaiPhieu, trangThaiLoc, StringComparison.OrdinalIgnoreCase));
                }
                return q.OrderByDescending(x => x.MaPhieuThue).ToList();
            }

            List<PhieuThuePhong> list = new List<PhieuThuePhong>();
            string sql = @"SELECT pt.MaPhieuThue, pt.MaKH, kh.HoTen AS TenKhachHang, kh.LoaiKhach,
                                  pt.MaNV, pt.MaPhong, p.TenPhong, pt.NgayNhanPhong, pt.NgayTraDuKien,
                                  pt.NgayTraThucTe, pt.HinhThucThue, pt.TienDatCoc, pt.TrangThaiPhieu
                           FROM PhieuThuePhong pt
                           INNER JOIN KhachHang kh ON pt.MaKH = kh.MaKH
                           INNER JOIN Phong p ON pt.MaPhong = p.MaPhong
                           WHERE (@TrangThai = N'' OR pt.TrangThaiPhieu = @TrangThai)
                           ORDER BY pt.MaPhieuThue DESC";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TrangThai", trangThaiLoc ?? "");
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new PhieuThuePhong
                        {
                            MaPhieuThue = Convert.ToInt32(reader["MaPhieuThue"]),
                            MaKH = reader["MaKH"].ToString(),
                            TenKhachHang = reader["TenKhachHang"].ToString(),
                            LoaiKhach = reader["LoaiKhach"].ToString(),
                            MaNV = reader["MaNV"].ToString(),
                            MaPhong = reader["MaPhong"].ToString(),
                            TenPhong = reader["TenPhong"].ToString(),
                            NgayNhanPhong = Convert.ToDateTime(reader["NgayNhanPhong"]),
                            NgayTraDuKien = Convert.ToDateTime(reader["NgayTraDuKien"]),
                            NgayTraThucTe = reader["NgayTraThucTe"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["NgayTraThucTe"]),
                            HinhThucThue = reader["HinhThucThue"].ToString(),
                            TienDatCoc = Convert.ToDecimal(reader["TienDatCoc"]),
                            TrangThaiPhieu = reader["TrangThaiPhieu"].ToString()
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Lấy danh sách Chi tiết dịch vụ (Detail) theo Mã phiếu thuê (Master).
        /// </summary>
        public List<ChiTietSuDungDichVu> GetChiTietDichVuByPhieu(int maPhieuThue)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                return InMemoryStore.ChiTietDichVus
                    .Where(x => x.MaPhieuThue == maPhieuThue)
                    .OrderByDescending(x => x.ThoiGianGoi)
                    .ToList();
            }

            List<ChiTietSuDungDichVu> list = new List<ChiTietSuDungDichVu>();
            string sql = @"SELECT ct.MaChiTiet, ct.MaPhieuThue, ct.MaDV, dv.TenDV, dv.DonViTinh,
                                  ct.SoLuong, ct.DonGiaLucGoi, ct.ThoiGianGoi
                           FROM ChiTietSuDungDichVu ct
                           INNER JOIN DichVu dv ON ct.MaDV = dv.MaDV
                           WHERE ct.MaPhieuThue = @MaPhieuThue
                           ORDER BY ct.ThoiGianGoi DESC";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaPhieuThue", maPhieuThue);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new ChiTietSuDungDichVu
                        {
                            MaChiTiet = Convert.ToInt32(reader["MaChiTiet"]),
                            MaPhieuThue = Convert.ToInt32(reader["MaPhieuThue"]),
                            MaDV = reader["MaDV"].ToString(),
                            TenDV = reader["TenDV"].ToString(),
                            DonViTinh = reader["DonViTinh"].ToString(),
                            SoLuong = Convert.ToInt32(reader["SoLuong"]),
                            DonGiaLucGoi = Convert.ToDecimal(reader["DonGiaLucGoi"]),
                            ThoiGianGoi = Convert.ToDateTime(reader["ThoiGianGoi"])
                        });
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// GIAO DỊCH 1 (SqlTransaction): Nhận phòng (Check-In).
        /// Thực hiện đồng thời 2 lệnh trong 1 Transaction:
        /// 1. INSERT phiếu thuê mới vào bảng PhieuThuePhong
        /// 2. UPDATE trạng thái phòng trong bảng Phong thành N'Đang thuê'
        /// Nếu bất kỳ bước nào lỗi -> Rollback toàn bộ!
        /// </summary>
        public int NhanPhongTransaction(PhieuThuePhong pt)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                Phong room = InMemoryStore.Phongs.FirstOrDefault(x => string.Equals(x.MaPhong, pt.MaPhong, StringComparison.OrdinalIgnoreCase));
                if (room == null)
                    throw new InvalidOperationException("Không tìm thấy phòng: " + pt.MaPhong);
                if (!string.Equals(room.TrangThai, "Trống", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Phòng " + pt.MaPhong + " hiện không ở trạng thái Trống!");

                KhachHang kh = InMemoryStore.KhachHangs.FirstOrDefault(x => string.Equals(x.MaKH, pt.MaKH, StringComparison.OrdinalIgnoreCase));
                int newId = InMemoryStore.PhieuThues.Count == 0 ? 1 : InMemoryStore.PhieuThues.Max(x => x.MaPhieuThue) + 1;
                pt.MaPhieuThue = newId;
                pt.TenKhachHang = kh != null ? kh.HoTen : pt.MaKH;
                pt.LoaiKhach = kh != null ? kh.LoaiKhach : "Thường";
                pt.TenPhong = room.TenPhong;
                pt.TrangThaiPhieu = "Đang thuê";

                InMemoryStore.PhieuThues.Add(pt);
                room.TrangThai = "Đang thuê";
                return newId;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Bước 1: Kiểm tra phòng có đang Trống không
                        string sqlCheck = "SELECT TrangThai FROM Phong WHERE MaPhong = @MaPhong";
                        using (SqlCommand cmdCheck = new SqlCommand(sqlCheck, conn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@MaPhong", pt.MaPhong);
                            object statusObj = cmdCheck.ExecuteScalar();
                            if (statusObj == null || statusObj.ToString() != "Trống")
                            {
                                throw new InvalidOperationException("Phòng " + pt.MaPhong + " không ở trạng thái Trống, không thể nhận phòng!");
                            }
                        }

                        // Bước 2: Thêm bản ghi PhieuThuePhong và lấy mã tự tăng SCOPE_IDENTITY()
                        string sqlInsertPhieu = @"
                            INSERT INTO PhieuThuePhong (MaKH, MaNV, MaPhong, NgayNhanPhong, NgayTraDuKien, HinhThucThue, TienDatCoc, TrangThaiPhieu)
                            VALUES (@MaKH, @MaNV, @MaPhong, @NgayNhanPhong, @NgayTraDuKien, @HinhThucThue, @TienDatCoc, N'Đang thuê');
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int newMaPhieu;
                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsertPhieu, conn, tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@MaKH", pt.MaKH);
                            cmdInsert.Parameters.AddWithValue("@MaNV", pt.MaNV);
                            cmdInsert.Parameters.AddWithValue("@MaPhong", pt.MaPhong);
                            cmdInsert.Parameters.AddWithValue("@NgayNhanPhong", pt.NgayNhanPhong);
                            cmdInsert.Parameters.AddWithValue("@NgayTraDuKien", pt.NgayTraDuKien);
                            cmdInsert.Parameters.AddWithValue("@HinhThucThue", pt.HinhThucThue);
                            cmdInsert.Parameters.AddWithValue("@TienDatCoc", pt.TienDatCoc);

                            newMaPhieu = Convert.ToInt32(cmdInsert.ExecuteScalar());
                        }

                        // Bước 3: Cập nhật trạng thái Phòng -> N'Đang thuê'
                        string sqlUpdateRoom = "UPDATE Phong SET TrangThai = N'Đang thuê' WHERE MaPhong = @MaPhong";
                        using (SqlCommand cmdRoom = new SqlCommand(sqlUpdateRoom, conn, tran))
                        {
                            cmdRoom.Parameters.AddWithValue("@MaPhong", pt.MaPhong);
                            cmdRoom.ExecuteNonQuery();
                        }

                        // Hoàn tất giao dịch
                        tran.Commit();
                        return newMaPhieu;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// GIAO DỊCH 2 (SqlTransaction): Gọi dịch vụ cho phòng đang thuê & Trừ tồn kho dịch vụ.
        /// </summary>
        public void GoiDichVuTransaction(int maPhieuThue, string maDV, int soLuong)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                DichVu dv = InMemoryStore.DichVus.FirstOrDefault(x => string.Equals(x.MaDV, maDV, StringComparison.OrdinalIgnoreCase));
                if (dv == null)
                    throw new InvalidOperationException("Không tìm thấy dịch vụ: " + maDV);
                if (dv.SoLuongTon < soLuong)
                    throw new InvalidOperationException("Số lượng tồn kho của dịch vụ '" + dv.TenDV + "' không đủ (Còn: " + dv.SoLuongTon + ").");

                dv.SoLuongTon -= soLuong;
                int nextId = InMemoryStore.ChiTietDichVus.Count == 0 ? 1 : InMemoryStore.ChiTietDichVus.Max(x => x.MaChiTiet) + 1;
                InMemoryStore.ChiTietDichVus.Add(new ChiTietSuDungDichVu
                {
                    MaChiTiet = nextId,
                    MaPhieuThue = maPhieuThue,
                    MaDV = dv.MaDV,
                    TenDV = dv.TenDV,
                    DonViTinh = dv.DonViTinh,
                    SoLuong = soLuong,
                    DonGiaLucGoi = dv.DonGia,
                    ThoiGianGoi = DateTime.Now
                });
                return;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // Bước 1: Kiểm tra tồn kho và lấy đơn giá hiện hành
                        decimal donGia;
                        int soLuongTon;
                        string sqlCheckDV = "SELECT DonGia, SoLuongTon FROM DichVu WHERE MaDV = @MaDV";
                        using (SqlCommand cmdCheck = new SqlCommand(sqlCheckDV, conn, tran))
                        {
                            cmdCheck.Parameters.AddWithValue("@MaDV", maDV);
                            using (SqlDataReader reader = cmdCheck.ExecuteReader())
                            {
                                if (!reader.Read())
                                    throw new InvalidOperationException("Không tìm thấy mã dịch vụ: " + maDV);
                                donGia = Convert.ToDecimal(reader["DonGia"]);
                                soLuongTon = Convert.ToInt32(reader["SoLuongTon"]);
                            }
                        }

                        if (soLuongTon < soLuong)
                        {
                            throw new InvalidOperationException("Tồn kho dịch vụ không đủ! Tồn hiện tại: " + soLuongTon);
                        }

                        // Bước 2: Thêm vào bảng ChiTietSuDungDichVu
                        string sqlInsertCT = @"
                            INSERT INTO ChiTietSuDungDichVu (MaPhieuThue, MaDV, SoLuong, DonGiaLucGoi, ThoiGianGoi)
                            VALUES (@MaPhieuThue, @MaDV, @SoLuong, @DonGiaLucGoi, GETDATE())";
                        using (SqlCommand cmdInsert = new SqlCommand(sqlInsertCT, conn, tran))
                        {
                            cmdInsert.Parameters.AddWithValue("@MaPhieuThue", maPhieuThue);
                            cmdInsert.Parameters.AddWithValue("@MaDV", maDV);
                            cmdInsert.Parameters.AddWithValue("@SoLuong", soLuong);
                            cmdInsert.Parameters.AddWithValue("@DonGiaLucGoi", donGia);
                            cmdInsert.ExecuteNonQuery();
                        }

                        // Bước 3: Trừ số lượng tồn kho trong bảng DichVu
                        string sqlUpdateStock = "UPDATE DichVu SET SoLuongTon = SoLuongTon - @SoLuong WHERE MaDV = @MaDV";
                        using (SqlCommand cmdStock = new SqlCommand(sqlUpdateStock, conn, tran))
                        {
                            cmdStock.Parameters.AddWithValue("@SoLuong", soLuong);
                            cmdStock.Parameters.AddWithValue("@MaDV", maDV);
                            cmdStock.ExecuteNonQuery();
                        }

                        tran.Commit();
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// GIAO DỊCH 3 (SqlTransaction): Thanh toán & Trả phòng (Check-Out).
        /// Thực hiện 4 tác vụ liên kết trong 1 Transaction duy nhất:
        /// 1. INSERT hóa đơn vào bảng HoaDon
        /// 2. UPDATE PhieuThuePhong (TrangThaiPhieu = N'Đã thanh toán', NgayTraThucTe = GETDATE())
        /// 3. UPDATE Phong (TrangThai = N'Trống')
        /// 4. UPDATE KhachHang (Cộng thêm 10 điểm tích lũy)
        /// </summary>
        public int ThanhToanTraPhongTransaction(HoaDon hd, string maPhong, string maKH)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                PhieuThuePhong pt = InMemoryStore.PhieuThues.FirstOrDefault(x => x.MaPhieuThue == hd.MaPhieuThue);
                if (pt == null)
                    throw new InvalidOperationException("Không tìm thấy phiếu thuê cần thanh toán.");

                pt.TrangThaiPhieu = "Đã thanh toán";
                pt.NgayTraThucTe = DateTime.Now;

                Phong room = InMemoryStore.Phongs.FirstOrDefault(x => string.Equals(x.MaPhong, maPhong, StringComparison.OrdinalIgnoreCase));
                if (room != null)
                {
                    room.TrangThai = "Trống";
                }

                KhachHang kh = InMemoryStore.KhachHangs.FirstOrDefault(x => string.Equals(x.MaKH, maKH, StringComparison.OrdinalIgnoreCase));
                if (kh != null)
                {
                    kh.DiemTichLuy += 10;
                }

                int newMaHD = InMemoryStore.HoaDons.Count == 0 ? 1 : InMemoryStore.HoaDons.Max(x => x.MaHD) + 1;
                hd.MaHD = newMaHD;
                hd.NgayLap = DateTime.Now;
                InMemoryStore.HoaDons.Add(hd);
                return newMaHD;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            {
                conn.Open();
                using (SqlTransaction tran = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Tạo hóa đơn mới
                        string sqlInsertHD = @"
                            INSERT INTO HoaDon (MaPhieuThue, MaNV, NgayLap, TienPhong, TienDichVu, PhuThu, GiamGia, TongThanhToan, GhiChu)
                            VALUES (@MaPhieuThue, @MaNV, GETDATE(), @TienPhong, @TienDichVu, @PhuThu, @GiamGia, @TongThanhToan, @GhiChu);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        int newMaHD;
                        using (SqlCommand cmdHD = new SqlCommand(sqlInsertHD, conn, tran))
                        {
                            cmdHD.Parameters.AddWithValue("@MaPhieuThue", hd.MaPhieuThue);
                            cmdHD.Parameters.AddWithValue("@MaNV", hd.MaNV);
                            cmdHD.Parameters.AddWithValue("@TienPhong", hd.TienPhong);
                            cmdHD.Parameters.AddWithValue("@TienDichVu", hd.TienDichVu);
                            cmdHD.Parameters.AddWithValue("@PhuThu", hd.PhuThu);
                            cmdHD.Parameters.AddWithValue("@GiamGia", hd.GiamGia);
                            cmdHD.Parameters.AddWithValue("@TongThanhToan", hd.TongThanhToan);
                            cmdHD.Parameters.AddWithValue("@GhiChu", (object)hd.GhiChu ?? DBNull.Value);

                            newMaHD = Convert.ToInt32(cmdHD.ExecuteScalar());
                        }

                        // 2. Cập nhật Phiếu thuê -> Đã thanh toán
                        string sqlUpdatePT = @"
                            UPDATE PhieuThuePhong
                            SET TrangThaiPhieu = N'Đã thanh toán',
                                NgayTraThucTe = GETDATE()
                            WHERE MaPhieuThue = @MaPhieuThue";
                        using (SqlCommand cmdPT = new SqlCommand(sqlUpdatePT, conn, tran))
                        {
                            cmdPT.Parameters.AddWithValue("@MaPhieuThue", hd.MaPhieuThue);
                            cmdPT.ExecuteNonQuery();
                        }

                        // 3. Giải phóng Phòng -> Trống
                        string sqlUpdatePhong = "UPDATE Phong SET TrangThai = N'Trống' WHERE MaPhong = @MaPhong";
                        using (SqlCommand cmdPhong = new SqlCommand(sqlUpdatePhong, conn, tran))
                        {
                            cmdPhong.Parameters.AddWithValue("@MaPhong", maPhong);
                            cmdPhong.ExecuteNonQuery();
                        }

                        // 4. Cộng 10 điểm tích lũy cho Khách hàng
                        string sqlUpdateKH = "UPDATE KhachHang SET DiemTichLuy = DiemTichLuy + 10 WHERE MaKH = @MaKH";
                        using (SqlCommand cmdKH = new SqlCommand(sqlUpdateKH, conn, tran))
                        {
                            cmdKH.Parameters.AddWithValue("@MaKH", maKH);
                            cmdKH.ExecuteNonQuery();
                        }

                        tran.Commit();
                        return newMaHD;
                    }
                    catch
                    {
                        tran.Rollback();
                        throw;
                    }
                }
            }
        }

        /// <summary>
        /// Thống kê danh sách Hóa đơn theo khoảng ngày bằng Stored Procedure sp_ThongKeDoanhThu.
        /// </summary>
        public DataTable GetBaoCaoDoanhThu(DateTime tuNgay, DateTime denNgay)
        {
            DataTable dt = new DataTable("BaoCaoDoanhThu");
            if (DatabaseHelper.UseInMemoryMode)
            {
                dt.Columns.Add("MaHD", typeof(int));
                dt.Columns.Add("MaPhieuThue", typeof(int));
                dt.Columns.Add("TenKhachHang", typeof(string));
                dt.Columns.Add("TenPhong", typeof(string));
                dt.Columns.Add("NgayLap", typeof(DateTime));
                dt.Columns.Add("TienPhong", typeof(decimal));
                dt.Columns.Add("TienDichVu", typeof(decimal));
                dt.Columns.Add("PhuThu", typeof(decimal));
                dt.Columns.Add("GiamGia", typeof(decimal));
                dt.Columns.Add("TongThanhToan", typeof(decimal));

                foreach (HoaDon hd in InMemoryStore.HoaDons.Where(x => x.NgayLap >= tuNgay && x.NgayLap <= denNgay).OrderByDescending(x => x.NgayLap))
                {
                    dt.Rows.Add(hd.MaHD, hd.MaPhieuThue, hd.TenKhachHang, hd.TenPhong, hd.NgayLap,
                        hd.TienPhong, hd.TienDichVu, hd.PhuThu, hd.GiamGia, hd.TongThanhToan);
                }
                return dt;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_ThongKeDoanhThu", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TuNgay", tuNgay);
                cmd.Parameters.AddWithValue("@DenNgay", denNgay);
                using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                {
                    da.Fill(dt);
                }
            }
            return dt;
        }
    }
}
