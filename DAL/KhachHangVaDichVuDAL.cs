using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.DAL
{
    /// <summary>
    /// Tầng DAL quản lý Khách hàng & Dịch vụ khách sạn.
    /// Minh họa:
    /// 1. Chế độ Ngắt kết nối (Disconnected Mode): SqlDataAdapter, DataTable, DataView
    /// 2. Thực thi CRUD bằng Stored Procedure (sp_ThemKhachHang, sp_CapNhatKhachHang, sp_XoaKhachHang)
    /// 3. Khởi tạo Đa hình đối tượng KhachHang / KhachHangVIP từ dữ liệu đọc được
    /// </summary>
    public class KhachHangDAL : IRepository<KhachHang>
    {
        public static KhachHang TaoDoiTuongDaHinh(string maKH, string hoTen, string cccd, string sdt, string diaChi, string loaiKhach, int diem)
        {
            if (string.Equals(loaiKhach, "VIP", StringComparison.OrdinalIgnoreCase))
            {
                return new KhachHangVIP(maKH, hoTen, cccd, sdt, diaChi, diem);
            }
            return new KhachHang(maKH, hoTen, cccd, sdt, diaChi, loaiKhach, diem);
        }

        public List<KhachHang> GetAll()
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                return InMemoryStore.KhachHangs.Select(x => x.Clone()).ToList();
            }

            List<KhachHang> list = new List<KhachHang>();
            string sql = "SELECT MaKH, HoTen, CCCD, SoDienThoai, DiaChi, LoaiKhach, DiemTichLuy FROM KhachHang ORDER BY MaKH";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(TaoDoiTuongDaHinh(
                            reader["MaKH"].ToString(),
                            reader["HoTen"].ToString(),
                            reader["CCCD"].ToString(),
                            reader["SoDienThoai"].ToString(),
                            reader["DiaChi"] == DBNull.Value ? "" : reader["DiaChi"].ToString(),
                            reader["LoaiKhach"].ToString(),
                            Convert.ToInt32(reader["DiemTichLuy"])
                        ));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Nạp danh sách khách hàng về DataTable bằng SqlDataAdapter (Dữ liệu ngắt kết nối - Disconnected Mode).
        /// Cho phép tầng GUI lọc trực tiếp bằng DataView.RowFilter mà không cần truy vấn lại CSDL.
        /// </summary>
        public DataTable GetDataTableKhachHang()
        {
            DataTable dt = new DataTable("KhachHang");
            if (DatabaseHelper.UseInMemoryMode)
            {
                dt.Columns.Add("MaKH", typeof(string));
                dt.Columns.Add("HoTen", typeof(string));
                dt.Columns.Add("CCCD", typeof(string));
                dt.Columns.Add("SoDienThoai", typeof(string));
                dt.Columns.Add("DiaChi", typeof(string));
                dt.Columns.Add("LoaiKhach", typeof(string));
                dt.Columns.Add("DiemTichLuy", typeof(int));
                dt.Columns.Add("VaiTroDaHinh", typeof(string));
                dt.Columns.Add("UuDaiPhanTram", typeof(decimal));

                foreach (KhachHang kh in InMemoryStore.KhachHangs)
                {
                    dt.Rows.Add(
                        kh.MaKH,
                        kh.HoTen,
                        kh.CCCD,
                        kh.SoDienThoai,
                        kh.DiaChi,
                        kh.LoaiKhach,
                        kh.DiemTichLuy,
                        kh.LayVaiTro(),
                        kh.TinhTyLeGiamGia() * 100m
                    );
                }
                return dt;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlDataAdapter adapter = new SqlDataAdapter(
                "SELECT MaKH, HoTen, CCCD, SoDienThoai, DiaChi, LoaiKhach, DiemTichLuy FROM KhachHang ORDER BY MaKH", conn))
            {
                adapter.Fill(dt);
            }

            dt.Columns.Add("VaiTroDaHinh", typeof(string));
            dt.Columns.Add("UuDaiPhanTram", typeof(decimal));
            foreach (DataRow row in dt.Rows)
            {
                KhachHang obj = TaoDoiTuongDaHinh(
                    row["MaKH"].ToString(),
                    row["HoTen"].ToString(),
                    row["CCCD"].ToString(),
                    row["SoDienThoai"].ToString(),
                    row["DiaChi"].ToString(),
                    row["LoaiKhach"].ToString(),
                    Convert.ToInt32(row["DiemTichLuy"])
                );
                row["VaiTroDaHinh"] = obj.LayVaiTro();
                row["UuDaiPhanTram"] = obj.TinhTyLeGiamGia() * 100m;
            }
            return dt;
        }

        /// <summary>
        /// Thêm khách hàng bằng Stored Procedure sp_ThemKhachHang và SqlParameter.
        /// </summary>
        public bool Insert(KhachHang kh)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                if (InMemoryStore.KhachHangs.Any(x => string.Equals(x.MaKH, kh.MaKH, StringComparison.OrdinalIgnoreCase)))
                    return false;
                InMemoryStore.KhachHangs.Add(kh.Clone());
                return true;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_ThemKhachHang", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@MaKH", SqlDbType.VarChar, 20).Value = kh.MaKH;
                cmd.Parameters.Add("@HoTen", SqlDbType.NVarChar, 100).Value = kh.HoTen;
                cmd.Parameters.Add("@CCCD", SqlDbType.VarChar, 20).Value = kh.CCCD;
                cmd.Parameters.Add("@SoDienThoai", SqlDbType.VarChar, 15).Value = kh.SoDienThoai;
                cmd.Parameters.Add("@DiaChi", SqlDbType.NVarChar, 200).Value = (object)kh.DiaChi ?? DBNull.Value;
                cmd.Parameters.Add("@LoaiKhach", SqlDbType.NVarChar, 30).Value = kh.LoaiKhach;
                cmd.Parameters.Add("@DiemTichLuy", SqlDbType.Int).Value = kh.DiemTichLuy;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Cập nhật khách hàng bằng Stored Procedure sp_CapNhatKhachHang.
        /// </summary>
        public bool Update(KhachHang kh)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.KhachHangs.FindIndex(x => string.Equals(x.MaKH, kh.MaKH, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                InMemoryStore.KhachHangs[idx] = kh.Clone();
                return true;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_CapNhatKhachHang", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@MaKH", SqlDbType.VarChar, 20).Value = kh.MaKH;
                cmd.Parameters.Add("@HoTen", SqlDbType.NVarChar, 100).Value = kh.HoTen;
                cmd.Parameters.Add("@CCCD", SqlDbType.VarChar, 20).Value = kh.CCCD;
                cmd.Parameters.Add("@SoDienThoai", SqlDbType.VarChar, 15).Value = kh.SoDienThoai;
                cmd.Parameters.Add("@DiaChi", SqlDbType.NVarChar, 200).Value = (object)kh.DiaChi ?? DBNull.Value;
                cmd.Parameters.Add("@LoaiKhach", SqlDbType.NVarChar, 30).Value = kh.LoaiKhach;
                cmd.Parameters.Add("@DiemTichLuy", SqlDbType.Int).Value = kh.DiemTichLuy;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Xóa khách hàng bằng Stored Procedure sp_XoaKhachHang.
        /// </summary>
        public bool Delete(string maKH)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.KhachHangs.FindIndex(x => string.Equals(x.MaKH, maKH, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                InMemoryStore.KhachHangs.RemoveAt(idx);
                return true;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_XoaKhachHang", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("@MaKH", SqlDbType.VarChar, 20).Value = maKH;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }

    /// <summary>
    /// Tầng DAL quản lý Dịch vụ khách sạn (Minibar, Ăn sáng, Giặt ủi).
    /// </summary>
    public class DichVuDAL : IRepository<DichVu>
    {
        public List<DichVu> GetAll()
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                return InMemoryStore.DichVus.Select(x => x.Clone()).ToList();
            }

            List<DichVu> list = new List<DichVu>();
            string sql = "SELECT MaDV, TenDV, DanhMuc, DonViTinh, DonGia, SoLuongTon FROM DichVu ORDER BY DanhMuc, TenDV";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new DichVu(
                            reader["MaDV"].ToString(),
                            reader["TenDV"].ToString(),
                            reader["DanhMuc"].ToString(),
                            reader["DonViTinh"].ToString(),
                            Convert.ToDecimal(reader["DonGia"]),
                            Convert.ToInt32(reader["SoLuongTon"])
                        ));
                    }
                }
            }
            return list;
        }

        public DataTable GetDataTableDichVu()
        {
            DataTable dt = new DataTable("DichVu");
            if (DatabaseHelper.UseInMemoryMode)
            {
                dt.Columns.Add("MaDV", typeof(string));
                dt.Columns.Add("TenDV", typeof(string));
                dt.Columns.Add("DanhMuc", typeof(string));
                dt.Columns.Add("DonViTinh", typeof(string));
                dt.Columns.Add("DonGia", typeof(decimal));
                dt.Columns.Add("SoLuongTon", typeof(int));
                dt.Columns.Add("GiaTriTonKho", typeof(decimal));

                foreach (DichVu dv in InMemoryStore.DichVus)
                {
                    dt.Rows.Add(dv.MaDV, dv.TenDV, dv.DanhMuc, dv.DonViTinh, dv.DonGia, dv.SoLuongTon, dv.GiaTriTonKho);
                }
                return dt;
            }

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlDataAdapter da = new SqlDataAdapter(
                "SELECT MaDV, TenDV, DanhMuc, DonViTinh, DonGia, SoLuongTon, (DonGia * SoLuongTon) AS GiaTriTonKho FROM DichVu", conn))
            {
                da.Fill(dt);
            }
            return dt;
        }

        public bool Insert(DichVu dv)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                if (InMemoryStore.DichVus.Any(x => string.Equals(x.MaDV, dv.MaDV, StringComparison.OrdinalIgnoreCase)))
                    return false;
                InMemoryStore.DichVus.Add(dv.Clone());
                return true;
            }

            string sql = @"INSERT INTO DichVu (MaDV, TenDV, DanhMuc, DonViTinh, DonGia, SoLuongTon)
                           VALUES (@MaDV, @TenDV, @DanhMuc, @DonViTinh, @DonGia, @SoLuongTon)";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaDV", dv.MaDV);
                cmd.Parameters.AddWithValue("@TenDV", dv.TenDV);
                cmd.Parameters.AddWithValue("@DanhMuc", dv.DanhMuc);
                cmd.Parameters.AddWithValue("@DonViTinh", dv.DonViTinh);
                cmd.Parameters.AddWithValue("@DonGia", dv.DonGia);
                cmd.Parameters.AddWithValue("@SoLuongTon", dv.SoLuongTon);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Update(DichVu dv)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.DichVus.FindIndex(x => string.Equals(x.MaDV, dv.MaDV, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                InMemoryStore.DichVus[idx] = dv.Clone();
                return true;
            }

            string sql = @"UPDATE DichVu
                           SET TenDV = @TenDV,
                               DanhMuc = @DanhMuc,
                               DonViTinh = @DonViTinh,
                               DonGia = @DonGia,
                               SoLuongTon = @SoLuongTon
                           WHERE MaDV = @MaDV";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaDV", dv.MaDV);
                cmd.Parameters.AddWithValue("@TenDV", dv.TenDV);
                cmd.Parameters.AddWithValue("@DanhMuc", dv.DanhMuc);
                cmd.Parameters.AddWithValue("@DonViTinh", dv.DonViTinh);
                cmd.Parameters.AddWithValue("@DonGia", dv.DonGia);
                cmd.Parameters.AddWithValue("@SoLuongTon", dv.SoLuongTon);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool Delete(string maDV)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.DichVus.FindIndex(x => string.Equals(x.MaDV, maDV, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                InMemoryStore.DichVus.RemoveAt(idx);
                return true;
            }

            string sql = "DELETE FROM DichVu WHERE MaDV = @MaDV";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaDV", maDV);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
