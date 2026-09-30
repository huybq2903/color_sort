# Module @SaveLoad

## Tổng quan
- **Tên:** `SaveLoad`
- **Giới thiệu:** Module SaveLoad cung cấp giao diện đơn giản để lưu và tải dữ liệu game. Nó hoạt động như một trình bao bọc (wrapper) cho thư viện `BayatGames.SaveGamePro`, đơn giản hóa các thao tác phổ biến và tích hợp mã hóa khóa tự động.
- **Tính năng chính:**
    - **API đơn giản:** Các phương thức tĩnh dễ sử dụng cho `Save`, `Load`, `ExistsKey`, và `DeleteKey`.
    - **Mã hóa tự động:** Tất cả các khóa (key) được mã hóa tự động bằng MD5.
    - **Giá trị mặc định:** Dễ dàng cung cấp giá trị mặc định khi tải dữ liệu.
    - **Tạo khóa:** Tiện ích tạo khóa duy nhất từ các đối tượng.

## Quick Start

### 1. Cấu hình
Module sử dụng các hằng số Pass được định nghĩa trong `SaveLoadConfigs.cs`. Để chỉnh sửa các giá trị này, hãy sử dụng cửa sổ editor tại menu:
`Falcon > Modules > SaveLoad > Update Configs`.

### 2. Lưu dữ liệu
Sử dụng phương thức `Save` để lưu bất kỳ loại dữ liệu nào.
```csharp
// Lưu một đối tượng tùy chỉnh
MyCustomData data = new MyCustomData 
{ 
    score = 100, 
    playerName = "Falcon" 
};

SaveLoadHandler.Save("player_data", data);

// Lưu các kiểu dữ liệu cơ bản
SaveLoadHandler.Save("current_level", 5);
```

### 3. Tải dữ liệu
Sử dụng phương thức `Load` để truy xuất dữ liệu, có thể cung cấp giá trị mặc định.
```csharp
// Tải một đối tượng, trả về null nếu không tồn tại
MyCustomData playerData = SaveLoadHandler.Load<MyCustomData>("player_data");

// Tải một giá trị int, trả về 1 nếu không tồn tại
int currentLevel = SaveLoadHandler.Load<int>("current_level", 1); 
```

## Danh sách đầy đủ API

Tất cả các phương thức đều là tĩnh (static) trong lớp `SaveLoadHandler`.

- `Save<T>(string key, T value)`: Lưu một giá trị với một khóa được chỉ định.
- `Load<T>(string key, T defaultValue = default)`: Tải một giá trị theo khóa. Trả về `defaultValue` nếu không tìm thấy khóa.
- `ExistsKey(string key)`: Kiểm tra xem một khóa có tồn tại hay không.
- `DeleteKey(string key)`: Xóa một cặp khóa-giá trị.
- `GenerateKeyBy(params object[] objects)`: Tạo một chuỗi khóa duy nhất từ một hoặc nhiều đối tượng.

## Chi tiết

### Phụ thuộc
- **BayatGames.SaveGamePro**: Module này phụ thuộc vào asset SaveGamePro từ BayatGames để thực hiện các thao tác lưu/tải ở tầng thấp.


## Lỗi thường gặp

### Tải dữ liệu không tồn tại
Khi gọi `Load<T>(key)` cho một khóa không tồn tại mà không cung cấp giá trị mặc định, phương thức sẽ trả về giá trị `default` của kiểu `T` (ví dụ: `null` cho các lớp, `0` cho `int`). Điều này có thể gây ra lỗi `NullReferenceException` nếu không được xử lý đúng cách. Luôn cân nhắc cung cấp giá trị mặc định hợp lý.
```csharp
// An toàn hơn: Cung cấp giá trị mặc định
int level = SaveLoadHandler.Load<int>("level", 1); 
```