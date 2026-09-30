# Module Account
## Tổng quan
- **Tên:** `Account`
- **Giới thiệu:** Kết nối với Server để lưu trữ thông tin người dùng, phục hồi dữ liệu khi người dùng cài lại game, đăng nhập, đăng xuất, v.v.

## Quick Start
1. ### Cấu hình
Trên thanh Menu của Unity, chọn Falcon -> Modules -> Network Settings, điền các thông số Server Ip, Context, Port tương ứng
2. ### Khởi tạo Network
Trong code của bạn, gọi hàm khởi tạo để kết nối, đăng nhập tới Server như sau:
```csharp
AccountManager.Instance.Init();
```
### Sử dụng
Các module khác sử dụng Module Account thông qua `AccountManager`. Dưới đây là các thao tác cơ bản:

3. ### Để cập nhật thông tin tài khoản lên Server:

```csharp
AccountManager.Instance.UpdateToServer();
```

4. ### Để kiểm tra Client đã đăng nhập thành công hay chưa, dùng biến bool:

```csharp
bool AccountManager.Instance.IsLogin
```

5. ### Để tạo một bản tin CS mà chỉ gửi khi Client đăng nhập thành công 
Cần kế thừa bản tin đó từ lớp CSMessageWaitLoginSuccess, 
khi đó hàm Send() sẽ không gửi ngay bản tin đi mà sẽ được lưu lại, gửi sau khi Client đăng nhập thành công
```csharp
    public class CSTranslateReq : CSMessageWaitLoginSuccess
    {
    }
    
    new CSTranslateReq().Send(); //Sẽ không gửi ngay mà sẽ lưu lại, gửi sau khi Client đăng nhập thành công
```
6. ### Để bắt sự kiện Login
Sử dụng sự kiện OnLoginEvent, tham số success sẽ là true nếu đăng nhập thành công, false nếu không thành công
```csharp
AccountManager.Instance.OnLoginEvent += success => { };
```
* Truy cập thông tin ClientData thông qua `AccountManager.Instance.ClientData`
* Truy cập thông tin GameInfo thông qua `AccountManager.Instance.ClientData.gameInfo`

7. ### Khai báo dữ liệu cho module
- Trước khi sử dụng, đọc kỹ tài liệu README ở module FReflection [→ Xem hướng dẫn](../../../Helpers/FReflection/README.md)
- Các module khác có thể khai báo dữ liệu thêm vào tài khoản người chơi bằng cách kế thừa lớp FGameData, ví dụ
```csharp
    [FGameDataType("game_data_1")]
    public class GameData1 : FGameData<GameData1>
    {
        public int int1 { get; set; }
        public string string1 { get; set; }
        public A a;
    }
```
Dữ liệu sau khi khai báo như trên sẽ được tự động lưu lên db của Server và trả về khi người chơi xoá game đi cài lại. Khi khai báo như vậy, đồng nghĩa với 
việc class là Singleton và việc truy cập vào các thuộc tính sẽ thông qua Instance, ví dụ GameData1.Instance.int1;

8. ### Lưu GameData xuống bộ nhớ điện thoại

```csharp
    GameData1.Instance.Save();
```
9. ### Update dữ liệu của một GameData lên Server
```csharp
    GameData1.Instance.UpdateToServer();
    //hoặc
    GameData1.Instance.UpdateToServer(true); //Nếu muốn nén dữ liệu
```

10. ### Bắt sự kiện update dữ liệu từ Server về cho từng GameData
```csharp
    [FGameDataType("game_data_1")]
    public class GameData1 : FGameData<GameData1>
    {
        public int int1 { get; set; }
        public string string1 { get; set; }
        public A a;
        public override void OnUpdateFromServer()
        {
            //To-do
        }
    }
```

11. ### Bắt sự kiện update dữ liệu từ Server về
```csharp
AccountManager.Instance.OnUpdateFromServer += clientData =>
{
    //To-do
};
```

## lưu ý

- Mỗi Module nên có một lớp kế thừa từ FGameData để khai báo dữ liệu của module đó, và lớp này sẽ được tự động lưu lên Server.

## Lỗi thường gặp
- Cấu hình Network không khớp với cấu hình trên Server, ví dụ: sai IP, sai Context, sai Port.
- Các thuộc tính của CS hoặc SC không được để public
- Class SC KHÔNG có constructor không tham số, mà lại có constructor có tham số
- CHƯA đọc tài liệu README ở module FReflection [→ Xem hướng dẫn](../../../Helpers/FReflection/README.md)




