# Module Helper @Singletons

## Tổng quan
- **Tên:** `Singletons`
- **Giới thiệu:** Cung cấp một tập hợp các lớp cơ sở (base classes) để triển khai các biến thể khác nhau của Singleton Pattern trong Unity.
- **Các loại Singleton:**
    - `Singleton<T>`: Singleton chuẩn, vòng đời gắn liền với scene hiện tại.
    - `PersistentSingleton<T>`: Singleton tồn tại vĩnh viễn giữa các lần chuyển scene, giữ lại instance đầu tiên được tạo.
    - `PersistentHumbleSingleton<T>`: Singleton tồn tại vĩnh viễn, nhưng sẽ thay thế instance cũ bằng instance mới hơn khi có sự trùng lặp.

## Quick Start

Để tạo một Singleton, hãy tạo một lớp mới kế thừa từ một trong các lớp cơ sở trên.

### Ví dụ với `PersistentSingleton`
```csharp
// GameManager.cs
using Falcon.Helpers.Singleton;

public class GameManager : PersistentSingleton<GameManager> 
{
    public void DoSomething()
    {
        Debug.Log("GameManager is doing something!");
    }
}
```

Để truy cập vào instance từ bất kỳ đâu:
```csharp
// SomeOtherClass.cs
void Start()
{
    // Truy cập instance và gọi một phương thức
    GameManager.Instance.DoSomething();
}
```

## Danh sách đầy đủ API

### `Singleton<T>`
- **Mô tả:** Singleton chỉ tồn tại trong scene hiện tại. Nếu không có instance nào trong scene, một instance mới sẽ được tự động tạo khi được truy cập lần đầu.
- `static T Instance`: Thuộc tính để truy cập instance duy nhất.
- `static bool HasInstance`: Kiểm tra xem instance đã tồn tại chưa.
- `protected virtual void Awake()`: Khởi tạo instance. Cần gọi `base.Awake()` nếu bạn override.
- `protected virtual void OnDestroy()`: Dọn dẹp instance khi đối tượng bị hủy.

### `PersistentSingleton<T>`
- **Mô tả:** Singleton tồn tại vĩnh viễn (`DontDestroyOnLoad`). Nó sẽ giữ lại instance đầu tiên được tạo và phá hủy bất kỳ instance nào khác được tạo sau này.
- `static T Instance`: Truy cập instance duy nhất.
- `static bool HasInstance`: Kiểm tra sự tồn tại của instance.
- `bool AutomaticallyUnparentOnAwake`: Nếu `true`, GameObject sẽ được tách khỏi parent của nó khi `Awake`.

### `PersistentHumbleSingleton<T>`
- **Mô tả:** Tương tự như `PersistentSingleton`, nhưng với logic ngược lại: nó sẽ giữ lại instance *mới nhất* và phá hủy các instance cũ hơn.
- `static T Instance`: Truy cập instance duy nhất.
- `static bool HasInstance`: Kiểm tra sự tồn tại của instance.
- `float InitializationTime`: Thời điểm instance được khởi tạo, dùng để so sánh và quyết định instance nào sẽ bị hủy.

## Lỗi thường gặp

### Xung đột Singleton
Nếu bạn có nhiều đối tượng trong cùng một scene cùng kế thừa từ một lớp Singleton (ví dụ: hai `GameManager` trong scene), các lớp `PersistentSingleton` và `PersistentHumbleSingleton` sẽ tự động xử lý xung đột bằng cách phá hủy một trong các instance. Hãy lưu ý hành vi của từng loại để tránh mất dữ liệu không mong muốn.

### Truy cập `Instance` quá sớm
Việc truy cập `Instance` trong phương thức `Awake` của một script khác có thể xảy ra trước khi Singleton được khởi tạo. Để tránh điều này, hãy truy cập Singleton trong phương thức `Start` hoặc đảm bảo thứ tự thực thi script (Script Execution Order) được thiết lập chính xác trong Project Settings. 