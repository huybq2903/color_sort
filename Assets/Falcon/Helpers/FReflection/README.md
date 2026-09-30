# Module FReflection

## Tổng quan
- **Tên:** `FReflection`
- **Giới thiệu:** Nếu mỗi module dùng Reflection lại phải quét toàn bộ Assembly thì hệ thống sẽ rất nặng.
  Do đó FReflection sinh ra để tối ưu việc này. Các module khác dùng Reflection thông qua FReflection mà không phải quét toàn bộ Assembly.

## Quick Start
- Đặt Assembly cho thư mục code của bạn, chuột phải vào bên trong thư mục, Create -> Scripting -> Assembly Definition.
- Tên Assembly phải bắt đầu bằng Falcon (ví dụ: Falcon.Modules.Core.Network)
- Muốn Class của bạn có thể quét bằng FReflection, hãy kế thừa từ IFReflection, hoặc đặt thuộc tính [FReflection] cho Class đó.
- Lưu ý chỉ kế thừa Class cha từ IFReflection hoặc đặt thuộc tính [FReflection] cho Class cha, không cần phải làm điều này với các Class con.

Ví dụ:
```csharp
public class Hello : IFReflection
{
    // ...
}
```
Hoặc
```csharp
[FReflection]
public class Hello
{
    // ...
}
```

Sau đó sử dụng FReflection để lấy danh sách các Class

```csharp
Type[] types = FReflection.GetTypes();
foreach (Type type in types)
{
    if (type.IsSubclassOf(typeof(Hello))
    {
        //To-do
    }
}
```


## Chi tiết
- FReflection sẽ quét toàn bộ Assembly và lưu lại danh sách các Class có kế thừa IFReflection hoặc có thuộc tính [FReflection]. Do đó module của bạn không cần
quét lại toàn bộ Assembly nữa. Mỗi lần quét toàn bộ Assembly là một lần quét qua khoảng 64k Class, rất nặng.
- Nếu bạn cần lấy danh sách các Class, thì hãy để các Class đó kế thừa một Class cha, và Class cha kế thừa IFReflection, hoặc đặt thuộc tính [FReflection] cho Class cha.

## Lỗi thường gặp
- Không đặt Assembly Definition cho thư mục chứa code của bạn. Và tên của Assembly không bắt đầu bằng Falcon.
- Không kế thừa `IFReflection` hoặc không đặt thuộc tính `[FReflection]` cho Class của bạn.
