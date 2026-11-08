# TÀI LIỆU PHÂN TÍCH CHI TIẾT CHỨC NĂNG & TỪNG DÒNG CODE
## ĐỀ TÀI 18: XÂY DỰNG PHẦN MỀM QUẢN LÝ KHÁCH SẠN BẰNG CÔNG NGHỆ .NET

---

## PHẦN 1: TỔNG QUAN KIẾN TRÚC 3 LỚP & SỰ LIÊN KẾT GIỮA CÁC THÀNH PHẦN

Dự án `DeTai18_QuanLyKhachSan` được tổ chức theo đúng chuẩn **Kiến trúc 3 lớp (3-Tier Architecture)** kết hợp đầy đủ các nguyên lý **Lập trình hướng đối tượng (OOP)** và **ADO.NET**:

1. **Tầng `Models` (Entities / DTO & OOP):**
   * `IKhachSanInterfaces.cs`: Chứa 2 Interface `IHienThiThongTin` và `ITinhTien`.
   * `NguoiBase.cs`: Lớp trừu tượng (`abstract class`) đại diện cho con người, có phương thức trừu tượng `LayVaiTro()` và phương thức ảo `virtual TinhTyLeGiamGia()`.
   * `KhachHangVaNhanVien.cs`:
     * `KhachHang : NguoiBase` (Khách thường: giảm 2% khi điểm >= 100).
     * `KhachHangVIP : KhachHang` (**Kế thừa nhiều cấp & Đa hình**: nạp chồng `TinhTyLeGiamGia()` giảm 10% - 15%).
     * `NhanVien : NguoiBase` (Nhân viên quản lý / lễ tân).
   * `ChinhSachGiaThue.cs`: Lớp trừu tượng tính giá phòng `ChinhSachGiaThue` cùng 2 lớp con đa hình `ThueTheoNgayStrategy` và `ThueTheoGioStrategy`.
   * `PhongVaLoaiPhong.cs` & `DichVuVaHoaDon.cs`: Các thực thể `LoaiPhong`, `Phong`, `DichVu`, `PhieuThuePhong`, `ChiTietSuDungDichVu`, `HoaDon`.

2. **Tầng `DAL` (Data Access Layer - Chưa kết nối CSDL & Đã kết nối CSDL ADO.NET):**
   * `DatabaseHelper.cs`: Quản lý chuỗi kết nối `SqlConnection` và cờ `UseInMemoryMode` cho phép chạy song song cả 2 chế độ: **Mô hình 3 lớp Bộ nhớ (Chưa kết nối CSDL)** và **Mô hình 3 lớp SQL Server ADO.NET**.
   * `InMemoryStore.cs`: Kho dữ liệu mẫu trong bộ nhớ RAM.
   * `PhongDAL.cs`: Sử dụng `SqlDataReader` (`ExecuteReader`), `ExecuteScalar()`, `ExecuteNonQuery()` với `SqlParameter`, `Stored Procedure` (`sp_TimKiemPhong`), và `SqlDataAdapter` + `DataSet` + `DataRelation` (`FK_LoaiPhong_Phong`) cho quan hệ **Master-Detail**.
   * `KhachHangVaDichVuDAL.cs`: Sử dụng `SqlDataAdapter`, `DataTable` (Dữ liệu ngắt kết nối) và gọi `Stored Procedure` (`sp_ThemKhachHang`, `sp_CapNhatKhachHang`, `sp_XoaKhachHang`).
   * `PhieuThueVaHoaDonDAL.cs`: Sử dụng **`SqlTransaction` (`BeginTransaction`, `Commit`, `Rollback`)** cho 3 nghiệp vụ Nhận phòng, Gọi dịch vụ trừ kho, và Thanh toán trả phòng.

3. **Tầng `BUS` (Business Logic Layer - Dịch vụ thông tin):**
   * `PhongBUS.cs`, `KhachHangVaDichVuBUS.cs`, `NghiepVuKhachSanBUS.cs`: Kiểm tra hợp lệ dữ liệu đầu vào (Validation), nhóm dữ liệu cho `TreeView`, phối hợp đa hình tính tiền hóa đơn.

4. **Tầng `GUI` (Presentation Layer - Ứng dụng WinForms Đa Form):**
   * `frmDangNhap.cs`: Xác thực tài khoản + `ErrorProvider` + Chọn chế độ DAL.
   * `frmMain.cs`: Form cha MDI (`IsMdiContainer = true`), quản lý các Form con và tự động phát tín hiệu cập nhật dữ liệu (`BroadcastRefresh`) giữa các Form.
   * `frmSoDoPhong.cs`: Điều khiển `TreeView`, `ListView` (chia `ListViewGroup`, tô màu theo trạng thái phòng), lọc bằng `SqlDataReader` và thống kê nhanh bằng `ExecuteScalar()`.
   * `frmQuanLyPhongMasterDetail.cs`: Quan hệ **Master-Detail** trên 2 `DataGridView` bằng `DataSet` + `DataRelation` + `BindingSource`, kèm CRUD tham số hóa.
   * `frmKhachHangVaDichVu.cs`: Lọc dữ liệu ngắt kết nối tức thì bằng `DataView.RowFilter`, CRUD bằng `Stored Procedure` và hiển thị kết quả Đa hình OOP.
   * `frmNhanTraPhongTransaction.cs`: Màn hình nghiệp vụ tổng hợp **Nhận phòng - Gọi dịch vụ - Tính tiền đa hình - Thanh toán trả phòng** bằng `SqlTransaction` và báo cáo doanh thu `sp_ThongKeDoanhThu`.

---

## PHẦN 2: GIẢI THÍCH CHI TIẾT TỪNG DÒNG CODE TRỌNG TÂM

### 1. Kế thừa, Đa hình, Lớp trừu tượng & Interface (`Models/NguoiBase.cs` & `ChinhSachGiaThue.cs`)

```csharp
// Khai báo Interface quy định hợp đồng chung cho các lớp thực체
public interface IHienThiThongTin
{
    string LayMaDinhDanh();     // Trả về mã khóa chính của đối tượng
    string LayMoTaChiTiet();   // Trả về chuỗi mô tả hiển thị lên giao diện
}

// Lớp trừu tượng NguoiBase thực thi Interface IHienThiThongTin
public abstract class NguoiBase : IHienThiThongTin
{
    public string MaDinhDanh { get; set; }
    public string HoTen { get; set; }
    public string CCCD { get; set; }
    public string SoDienThoai { get; set; }

    // Phương thức abstract: Không có thân hàm, bắt buộc lớp con (KhachHang, NhanVien) phải override
    public abstract string LayVaiTro();

    // Phương thức virtual: Có cài đặt mặc định (0%), cho phép lớp con nạp chồng (Đa hình)
    public virtual decimal TinhTyLeGiamGia()
    {
        return 0m;
    }
}
```

* **Tại `KhachHangVIP : KhachHang`:**
  ```csharp
  public class KhachHangVIP : KhachHang
  {
      public override string LayVaiTro()
      {
          return "Khách hàng VIP (Ưu đãi đặc biệt)";
      }

      public override decimal TinhTyLeGiamGia()
      {
          // Đa hình lúc chạy (Runtime Polymorphism):
          // Cùng gọi kh.TinhTyLeGiamGia(), nhưng nếu kh là KhachHangVIP sẽ trả về 10% hoặc 15%
          return DiemTichLuy >= 200 ? 0.15m : 0.10m;
      }
  }
  ```

---

### 2. Đọc dữ liệu bằng `SqlDataReader` & Thực thi `Stored Procedure` (`DAL/PhongDAL.cs`)

```csharp
using (SqlConnection conn = DatabaseHelper.GetConnection()) // Khởi tạo kết nối tới SQL Server
using (SqlCommand cmd = new SqlCommand("sp_TimKiemPhong", conn)) // Khởi tạo đối tượng lệnh gọi SP
{
    cmd.CommandType = CommandType.StoredProcedure; // Chỉ định thực thi Stored Procedure thay vì câu SQL thường
    cmd.Parameters.AddWithValue("@Tang", tang); // Truyền tham số @Tang chống SQL Injection
    cmd.Parameters.AddWithValue("@MaLoai", maLoai ?? ""); // Truyền tham số @MaLoai
    cmd.Parameters.AddWithValue("@TrangThai", trangThai ?? ""); // Truyền tham số @TrangThai

    conn.Open(); // Mở kết nối vật lý tới SQL Server
    using (SqlDataReader reader = cmd.ExecuteReader()) // Thực thi lệnh đọc tuần tự một chiều (Connected Mode)
    {
        while (reader.Read()) // Di chuyển con trỏ qua từng dòng kết quả trả về
        {
            Phong p = new Phong
            {
                MaPhong = reader["MaPhong"].ToString(), // Đọc cột MaPhong
                TenPhong = reader["TenPhong"].ToString(), // Đọc cột TenPhong
                Tang = Convert.ToInt32(reader["Tang"]), // Ép kiểu cột Tang sang số nguyên
                DonGiaNgay = Convert.ToDecimal(reader["DonGiaNgay"]), // Ép kiểu giá phòng sang Decimal
                TrangThai = reader["TrangThai"].ToString()
            };
            result.Add(p); // Đưa đối tượng Phong vào danh sách kết quả
        }
    } // Tự động đóng SqlDataReader khi ra khỏi khối using
} // Tự động đóng và giải phóng SqlConnection
```

---

### 3. Đếm nhanh bằng `ExecuteScalar()` (`DAL/PhongDAL.cs`)

```csharp
string sql = "SELECT COUNT(*) FROM Phong WHERE TrangThai = @TrangThai";
using (SqlConnection conn = DatabaseHelper.GetConnection())
using (SqlCommand cmd = new SqlCommand(sql, conn))
{
    cmd.Parameters.AddWithValue("@TrangThai", trangThai); // Gắn tham số trạng thái cần đếm
    conn.Open();
    object scalarVal = cmd.ExecuteScalar(); // Trả về ô đầu tiên của dòng đầu tiên (tối ưu hiệu năng cho hàm COUNT/SUM)
    return scalarVal != null && scalarVal != DBNull.Value ? Convert.ToInt32(scalarVal) : 0;
}
```

---

### 4. Dữ liệu ngắt kết nối (`SqlDataAdapter`, `DataSet`, `DataRelation`) & Master-Detail (`DAL/PhongDAL.cs` & `GUI/frmQuanLyPhongMasterDetail.cs`)

* **Tại tầng DAL (`PhongDAL.GetMasterDetailLoaiPhongVaPhong`):**
  ```csharp
  DataSet ds = new DataSet("KhachSanMasterDetailDS"); // Tạo DataSet lưu trữ nhiều bảng trong bộ nhớ (Disconnected Mode)
  using (SqlConnection conn = DatabaseHelper.GetConnection())
  {
      SqlDataAdapter daLoai = new SqlDataAdapter("SELECT * FROM LoaiPhong", conn);
      SqlDataAdapter daPhong = new SqlDataAdapter("SELECT * FROM Phong", conn);

      daLoai.Fill(ds, "LoaiPhong"); // Tự động mở kết nối, tải bảng Master (LoaiPhong) vào DataSet rồi đóng kết nối
      daPhong.Fill(ds, "Phong");    // Tải bảng Detail (Phong) vào DataSet
  }

  // Thiết lập quan hệ cha-con (DataRelation) giữa cột MaLoai của 2 bảng trong DataSet
  DataColumn parentCol = ds.Tables["LoaiPhong"].Columns["MaLoai"];
  DataColumn childCol = ds.Tables["Phong"].Columns["MaLoai"];
  DataRelation relation = new DataRelation("FK_LoaiPhong_Phong", parentCol, childCol, false);
  ds.Relations.Add(relation);
  ```

* **Tại tầng GUI (`frmQuanLyPhongMasterDetail.TaiLaiDuLieu`):**
  ```csharp
  // BindingSource cha trỏ vào bảng Master "LoaiPhong"
  _bsMasterLoaiPhong = new BindingSource
  {
      DataSource = _dsMasterDetail,
      DataMember = "LoaiPhong"
  };

  // BindingSource con trỏ vào _bsMasterLoaiPhong thông qua tên quan hệ "FK_LoaiPhong_Phong"
  // Nhờ cơ chế này, khi người dùng click chọn bất kỳ Loại phòng nào ở lưới trên,
  // lưới dưới tự động lọc chỉ hiển thị các Phòng thuộc loại đó mà không cần viết vòng lặp lọc thủ công!
  _bsDetailPhong = new BindingSource
  {
      DataSource = _bsMasterLoaiPhong,
      DataMember = "FK_LoaiPhong_Phong"
  };

  dgvMasterLoaiPhong.DataSource = _bsMasterLoaiPhong;
  dgvDetailPhong.DataSource = _bsDetailPhong;
  ```

---

### 5. Lọc dữ liệu ngắt kết nối bằng `DataView.RowFilter` (`GUI/frmKhachHangVaDichVu.cs`)

```csharp
// Khởi tạo DataView bọc quanh DataTable _dtKhachHang đã nạp từ SqlDataAdapter
_dvKhachHang = new DataView(_dtKhachHang);
dgvKhachHang.DataSource = _dvKhachHang;

// Khi gõ từ khóa vào ô tìm kiếm hoặc chọn ComboBox hạng khách:
string filter = string.Format("(MaKH LIKE '%{0}%' OR HoTen LIKE '%{0}%' OR CCCD LIKE '%{0}%')", kw);
if (!string.IsNullOrEmpty(loai))
{
    filter += string.Format(" AND LoaiKhach = '{0}'", loai);
}
// Lọc trực tiếp trên bộ nhớ RAM tức thì mà không cần gửi câu lệnh SELECT xuống SQL Server
_dvKhachHang.RowFilter = filter;
```

---

### 6. Giao dịch `SqlTransaction` (`Commit` & `Rollback`) trong Nhận/Trả Phòng (`DAL/PhieuThueVaHoaDonDAL.cs`)

```csharp
using (SqlConnection conn = DatabaseHelper.GetConnection())
{
    conn.Open();
    // Bắt đầu một giao dịch (Transaction) gắn với kết nối hiện tại
    using (SqlTransaction tran = conn.BeginTransaction())
    {
        try
        {
            // Lệnh 1: Thêm Hóa đơn thanh toán (gắn tran vào SqlCommand)
            using (SqlCommand cmdHD = new SqlCommand(sqlInsertHD, conn, tran))
            {
                // ... Gán các SqlParameter ...
                newMaHD = Convert.ToInt32(cmdHD.ExecuteScalar());
            }

            // Lệnh 2: Cập nhật trạng thái Phiếu thuê -> 'Đã thanh toán'
            using (SqlCommand cmdPT = new SqlCommand(sqlUpdatePT, conn, tran))
            {
                cmdPT.Parameters.AddWithValue("@MaPhieuThue", hd.MaPhieuThue);
                cmdPT.ExecuteNonQuery();
            }

            // Lệnh 3: Giải phóng trạng thái Phòng -> 'Trống'
            using (SqlCommand cmdPhong = new SqlCommand(sqlUpdatePhong, conn, tran))
            {
                cmdPhong.Parameters.AddWithValue("@MaPhong", maPhong);
                cmdPhong.ExecuteNonQuery();
            }

            // Lệnh 4: Cộng 10 điểm tích lũy cho Khách hàng
            using (SqlCommand cmdKH = new SqlCommand(sqlUpdateKH, conn, tran))
            {
                cmdKH.Parameters.AddWithValue("@MaKH", maKH);
                cmdKH.ExecuteNonQuery();
            }

            // Khi cả 4 lệnh đều thành công 100% -> Xác nhận lưu vĩnh viễn xuống CSDL
            tran.Commit();
            return newMaHD;
        }
        catch
        {
            // Nếu bất kỳ lệnh nào trong 4 lệnh trên gặp sự cố -> Khôi phục toàn bộ về trạng thái ban đầu!
            tran.Rollback();
            throw;
        }
    }
}
```
