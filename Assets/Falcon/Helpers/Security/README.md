# Module Helper @Security

## Tổng quan
- **Tên:** `Security`
- **Giới thiệu:** Cung cấp các phương thức tiện ích tĩnh để thực hiện các thao tác mã hóa phổ biến như MD5 và SHA1.
- **Tính năng chính:**
    - Mã hóa chuỗi thành chuỗi MD5.
    - Băm chuỗi bằng thuật toán SHA1.
    - Tính toán giá trị MD5 cho một tệp.
    - Tích hợp bộ đệm (cache) cho các kết quả MD5 để tăng hiệu suất.

## Quick Start

### Mã hóa một chuỗi bằng `MD5`
```csharp
using Falcon.Helpers.Security;

string originalString = "hello_world";
string md5Hash = Encryption.MD5(originalString);

Debug.Log($"MD5 của '{originalString}' là: {md5Hash}");
```

### Băm chuỗi bằng `SHA1Hash`
```csharp
using Falcon.Helpers.Security;

string originalString = "lady_lucky_smile";
string sh1Hash = Encryption.SHA1Hash(originalString);

Debug.Log($"SHA1Hash của '{originalString}' là: {sh1Hash}");
```

## Danh sách đầy đủ API

Lớp `Falcon.Helpers.Security.Encryption` cung cấp các phương thức tĩnh sau:

- `string MD5(string inputString)`: Mã hóa chuỗi đầu vào bằng thuật toán MD5 và trả về chuỗi hex 32 ký tự. Kết quả sẽ được cache lại để tăng tốc cho các lần gọi sau với cùng một chuỗi đầu vào.
- `string SHA1Hash(string inputString)`: Băm chuỗi đầu vào bằng thuật toán SHA1 và trả về chuỗi hex.
- `string MD5File(string filename)`: Tính toán và trả về giá trị MD5 của một tệp được chỉ định bởi đường dẫn `filename`.