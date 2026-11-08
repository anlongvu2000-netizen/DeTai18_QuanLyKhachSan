using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using DeTai18_QuanLyKhachSan.Models;

namespace DeTai18_QuanLyKhachSan.DAL
{
    /// <summary>
    /// Tầng DAL quản lý Loại phòng & Phòng.
    /// Minh họa đầy đủ các kỹ thuật ADO.NET:
    /// 1. Đọc, tìm kiếm, lọc dữ liệu bằng SqlDataReader (ExecuteReader)
    /// 2. Thực thi Stored Procedure (sp_TimKiemPhong) với SqlParameter
    /// 3. Thực thi ExecuteScalar() đếm số lượng phòng theo trạng thái
    /// 4. Thực thi CRUD tham số hóa bằng ExecuteNonQuery()
    /// 5. Nạp DataSet + DataRelation phục vụ hiển thị Master-Detail (LoaiPhong -> Phong)
    /// </summary>
    public class PhongDAL : IRepository<Phong>
    {
        public List<LoaiPhong> GetAllLoaiPhong()
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                return InMemoryStore.LoaiPhongs.Select(x => x.Clone()).ToList();
            }

            List<LoaiPhong> list = new List<LoaiPhong>();
            string sql = "SELECT MaLoai, TenLoai, DonGiaNgay, DonGiaGio, SoNguoiToiDa, MoTa FROM LoaiPhong ORDER BY DonGiaNgay";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new LoaiPhong(
                            reader["MaLoai"].ToString(),
                            reader["TenLoai"].ToString(),
                            Convert.ToDecimal(reader["DonGiaNgay"]),
                            Convert.ToDecimal(reader["DonGiaGio"]),
                            Convert.ToInt32(reader["SoNguoiToiDa"]),
                            reader["MoTa"] == DBNull.Value ? "" : reader["MoTa"].ToString()
                        ));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Đọc toàn bộ danh sách Phòng bằng SqlDataReader (Chế độ kết nối - Connected Mode).
        /// </summary>
        public List<Phong> GetAll()
        {
            return TimKiemPhongBangDataReader(0, "", "");
        }

        /// <summary>
        /// Đọc, tìm kiếm và lọc dữ liệu phòng bằng SqlDataReader kết hợp Stored Procedure sp_TimKiemPhong.
        /// </summary>
        public List<Phong> TimKiemPhongBangDataReader(int tang, string maLoai, string trangThai)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                IEnumerable<Phong> query = InMemoryStore.Phongs.Select(x => x.Clone());
                if (tang > 0)
                    query = query.Where(x => x.Tang == tang);
                if (!string.IsNullOrWhiteSpace(maLoai))
                    query = query.Where(x => string.Equals(x.MaLoai, maLoai, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(trangThai))
                    query = query.Where(x => string.Equals(x.TrangThai, trangThai, StringComparison.OrdinalIgnoreCase));
                return query.OrderBy(x => x.Tang).ThenBy(x => x.MaPhong).ToList();
            }

            List<Phong> result = new List<Phong>();
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand("sp_TimKiemPhong", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Tang", tang);
                cmd.Parameters.AddWithValue("@MaLoai", maLoai ?? "");
                cmd.Parameters.AddWithValue("@TrangThai", trangThai ?? "");

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        Phong p = new Phong
                        {
                            MaPhong = reader["MaPhong"].ToString(),
                            TenPhong = reader["TenPhong"].ToString(),
                            Tang = Convert.ToInt32(reader["Tang"]),
                            MaLoai = reader["MaLoai"].ToString(),
                            TenLoai = reader["TenLoai"].ToString(),
                            DonGiaNgay = Convert.ToDecimal(reader["DonGiaNgay"]),
                            DonGiaGio = Convert.ToDecimal(reader["DonGiaGio"]),
                            SoNguoiToiDa = Convert.ToInt32(reader["SoNguoiToiDa"]),
                            TrangThai = reader["TrangThai"].ToString(),
                            GhiChu = reader["GhiChu"] == DBNull.Value ? "" : reader["GhiChu"].ToString()
                        };
                        result.Add(p);
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Sử dụng ExecuteScalar() để đếm nhanh số lượng phòng theo trạng thái.
        /// </summary>
        public int DemSoPhongTheoTrangThai(string trangThai)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                if (string.IsNullOrWhiteSpace(trangThai))
                    return InMemoryStore.Phongs.Count;
                return InMemoryStore.Phongs.Count(x => string.Equals(x.TrangThai, trangThai, StringComparison.OrdinalIgnoreCase));
            }

            string sql = string.IsNullOrWhiteSpace(trangThai)
                ? "SELECT COUNT(*) FROM Phong"
                : "SELECT COUNT(*) FROM Phong WHERE TrangThai = @TrangThai";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(trangThai))
                {
                    cmd.Parameters.AddWithValue("@TrangThai", trangThai);
                }
                conn.Open();
                object scalarVal = cmd.ExecuteScalar();
                return scalarVal != null && scalarVal != DBNull.Value ? Convert.ToInt32(scalarVal) : 0;
            }
        }

        /// <summary>
        /// Nạp DataSet gồm 2 bảng LoaiPhong (Master) và Phong (Detail) kèm DataRelation.
        /// Minh họa kỹ thuật: DATAADAPTER, DATASET, DATATABLE & MASTER-DETAIL.
        /// </summary>
        public DataSet GetMasterDetailLoaiPhongVaPhong()
        {
            DataSet ds = new DataSet("KhachSanMasterDetailDS");

            if (DatabaseHelper.UseInMemoryMode)
            {
                DataTable dtLoai = new DataTable("LoaiPhong");
                dtLoai.Columns.Add("MaLoai", typeof(string));
                dtLoai.Columns.Add("TenLoai", typeof(string));
                dtLoai.Columns.Add("DonGiaNgay", typeof(decimal));
                dtLoai.Columns.Add("DonGiaGio", typeof(decimal));
                dtLoai.Columns.Add("SoNguoiToiDa", typeof(int));
                dtLoai.Columns.Add("MoTa", typeof(string));

                foreach (LoaiPhong lp in InMemoryStore.LoaiPhongs)
                {
                    dtLoai.Rows.Add(lp.MaLoai, lp.TenLoai, lp.DonGiaNgay, lp.DonGiaGio, lp.SoNguoiToiDa, lp.MoTa);
                }

                DataTable dtPhong = new DataTable("Phong");
                dtPhong.Columns.Add("MaPhong", typeof(string));
                dtPhong.Columns.Add("TenPhong", typeof(string));
                dtPhong.Columns.Add("Tang", typeof(int));
                dtPhong.Columns.Add("MaLoai", typeof(string));
                dtPhong.Columns.Add("TrangThai", typeof(string));
                dtPhong.Columns.Add("GhiChu", typeof(string));

                foreach (Phong p in InMemoryStore.Phongs)
                {
                    dtPhong.Rows.Add(p.MaPhong, p.TenPhong, p.Tang, p.MaLoai, p.TrangThai, p.GhiChu);
                }

                ds.Tables.Add(dtLoai);
                ds.Tables.Add(dtPhong);
            }
            else
            {
                using (SqlConnection conn = DatabaseHelper.GetConnection())
                {
                    SqlDataAdapter daLoai = new SqlDataAdapter(
                        "SELECT MaLoai, TenLoai, DonGiaNgay, DonGiaGio, SoNguoiToiDa, MoTa FROM LoaiPhong", conn);
                    SqlDataAdapter daPhong = new SqlDataAdapter(
                        "SELECT MaPhong, TenPhong, Tang, MaLoai, TrangThai, GhiChu FROM Phong", conn);

                    daLoai.Fill(ds, "LoaiPhong");
                    daPhong.Fill(ds, "Phong");
                }
            }

            // Tạo mối quan hệ Master - Detail giữa bảng LoaiPhong và bảng Phong thông qua cột MaLoai
            DataColumn parentCol = ds.Tables["LoaiPhong"].Columns["MaLoai"];
            DataColumn childCol = ds.Tables["Phong"].Columns["MaLoai"];
            DataRelation relation = new DataRelation("FK_LoaiPhong_Phong", parentCol, childCol, false);
            ds.Relations.Add(relation);

            return ds;
        }

        /// <summary>
        /// Thêm mới một phòng bằng SqlCommand + SqlParameter + ExecuteNonQuery().
        /// </summary>
        public bool Insert(Phong p)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                if (InMemoryStore.Phongs.Any(x => string.Equals(x.MaPhong, p.MaPhong, StringComparison.OrdinalIgnoreCase)))
                    return false;
                Phong clone = p.Clone();
                InMemoryStore.SyncRoomTypeInfo(clone);
                InMemoryStore.Phongs.Add(clone);
                return true;
            }

            string sql = @"INSERT INTO Phong (MaPhong, TenPhong, Tang, MaLoai, TrangThai, GhiChu)
                           VALUES (@MaPhong, @TenPhong, @Tang, @MaLoai, @TrangThai, @GhiChu)";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@MaPhong", SqlDbType.VarChar, 20).Value = p.MaPhong;
                cmd.Parameters.Add("@TenPhong", SqlDbType.NVarChar, 50).Value = p.TenPhong;
                cmd.Parameters.Add("@Tang", SqlDbType.Int).Value = p.Tang;
                cmd.Parameters.Add("@MaLoai", SqlDbType.VarChar, 20).Value = p.MaLoai;
                cmd.Parameters.Add("@TrangThai", SqlDbType.NVarChar, 50).Value = p.TrangThai;
                cmd.Parameters.Add("@GhiChu", SqlDbType.NVarChar, 255).Value = (object)p.GhiChu ?? DBNull.Value;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Cập nhật thông tin phòng bằng SqlCommand + SqlParameter + ExecuteNonQuery().
        /// </summary>
        public bool Update(Phong p)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.Phongs.FindIndex(x => string.Equals(x.MaPhong, p.MaPhong, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                Phong clone = p.Clone();
                InMemoryStore.SyncRoomTypeInfo(clone);
                InMemoryStore.Phongs[idx] = clone;
                return true;
            }

            string sql = @"UPDATE Phong
                           SET TenPhong = @TenPhong,
                               Tang = @Tang,
                               MaLoai = @MaLoai,
                               TrangThai = @TrangThai,
                               GhiChu = @GhiChu
                           WHERE MaPhong = @MaPhong";

            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@MaPhong", SqlDbType.VarChar, 20).Value = p.MaPhong;
                cmd.Parameters.Add("@TenPhong", SqlDbType.NVarChar, 50).Value = p.TenPhong;
                cmd.Parameters.Add("@Tang", SqlDbType.Int).Value = p.Tang;
                cmd.Parameters.Add("@MaLoai", SqlDbType.VarChar, 20).Value = p.MaLoai;
                cmd.Parameters.Add("@TrangThai", SqlDbType.NVarChar, 50).Value = p.TrangThai;
                cmd.Parameters.Add("@GhiChu", SqlDbType.NVarChar, 255).Value = (object)p.GhiChu ?? DBNull.Value;

                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        /// <summary>
        /// Xóa phòng theo mã bằng SqlCommand + SqlParameter + ExecuteNonQuery().
        /// </summary>
        public bool Delete(string maPhong)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.Phongs.FindIndex(x => string.Equals(x.MaPhong, maPhong, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                InMemoryStore.Phongs.RemoveAt(idx);
                return true;
            }

            string sql = "DELETE FROM Phong WHERE MaPhong = @MaPhong";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.Add("@MaPhong", SqlDbType.VarChar, 20).Value = maPhong;
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool InsertLoaiPhong(LoaiPhong lp)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                if (InMemoryStore.LoaiPhongs.Any(x => string.Equals(x.MaLoai, lp.MaLoai, StringComparison.OrdinalIgnoreCase)))
                    return false;
                InMemoryStore.LoaiPhongs.Add(lp.Clone());
                return true;
            }

            string sql = @"INSERT INTO LoaiPhong (MaLoai, TenLoai, DonGiaNgay, DonGiaGio, SoNguoiToiDa, MoTa)
                           VALUES (@MaLoai, @TenLoai, @DonGiaNgay, @DonGiaGio, @SoNguoiToiDa, @MoTa)";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaLoai", lp.MaLoai);
                cmd.Parameters.AddWithValue("@TenLoai", lp.TenLoai);
                cmd.Parameters.AddWithValue("@DonGiaNgay", lp.DonGiaNgay);
                cmd.Parameters.AddWithValue("@DonGiaGio", lp.DonGiaGio);
                cmd.Parameters.AddWithValue("@SoNguoiToiDa", lp.SoNguoiToiDa);
                cmd.Parameters.AddWithValue("@MoTa", (object)lp.MoTa ?? DBNull.Value);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }

        public bool UpdateLoaiPhong(LoaiPhong lp)
        {
            if (DatabaseHelper.UseInMemoryMode)
            {
                int idx = InMemoryStore.LoaiPhongs.FindIndex(x => string.Equals(x.MaLoai, lp.MaLoai, StringComparison.OrdinalIgnoreCase));
                if (idx < 0) return false;
                InMemoryStore.LoaiPhongs[idx] = lp.Clone();
                foreach (Phong p in InMemoryStore.Phongs.Where(x => string.Equals(x.MaLoai, lp.MaLoai, StringComparison.OrdinalIgnoreCase)))
                {
                    InMemoryStore.SyncRoomTypeInfo(p);
                }
                return true;
            }

            string sql = @"UPDATE LoaiPhong
                           SET TenLoai = @TenLoai,
                               DonGiaNgay = @DonGiaNgay,
                               DonGiaGio = @DonGiaGio,
                               SoNguoiToiDa = @SoNguoiToiDa,
                               MoTa = @MoTa
                           WHERE MaLoai = @MaLoai";
            using (SqlConnection conn = DatabaseHelper.GetConnection())
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@MaLoai", lp.MaLoai);
                cmd.Parameters.AddWithValue("@TenLoai", lp.TenLoai);
                cmd.Parameters.AddWithValue("@DonGiaNgay", lp.DonGiaNgay);
                cmd.Parameters.AddWithValue("@DonGiaGio", lp.DonGiaGio);
                cmd.Parameters.AddWithValue("@SoNguoiToiDa", lp.SoNguoiToiDa);
                cmd.Parameters.AddWithValue("@MoTa", (object)lp.MoTa ?? DBNull.Value);
                conn.Open();
                return cmd.ExecuteNonQuery() > 0;
            }
        }
    }
}
