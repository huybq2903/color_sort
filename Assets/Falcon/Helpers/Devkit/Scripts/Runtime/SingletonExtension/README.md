[English](README.en.md) | **Tiếng Việt**
---

# Module Quản Lý Vòng Đời Unity Singleton

Chào mừng bạn đến với **Module Quản Lý Vòng Đời Unity Singleton**! Module này mở rộng Unity Singleton Framework cốt lõi, cung cấp quản lý vòng đời có cấu trúc cho các Singletons của bạn. Nó cho phép bạn định nghĩa rõ ràng logic khởi tạo, trước khi tiếp tục, sau khi kết thúc và logic **chuyển cảnh** cho Singletons, đảm bảo thứ tự thực thi cụ thể dựa trên các phụ thuộc của chúng.

---

## 🌟 Các Tính Năng Nổi Bật

* **Khởi tạo có cấu trúc (`IInit`):** Định nghĩa logic khởi động bất đồng bộ cho Singletons.
* **Hooks tiền tiếp tục (`IPioneer`):** Thực thi code khi ứng dụng tiếp tục từ trạng thái tạm dừng.
* **Hooks hậu dừng (`ITerminal`):** Thực hiện dọn dẹp hoặc các tác vụ cuối cùng khi ứng dụng tạm dừng hoặc dừng.
* **Hooks chuyển cảnh (`ISceneSingleton`):** Phản ứng với các thay đổi cảnh trong ứng dụng của bạn.
* **Thứ tự thực thi dựa trên phụ thuộc:** Tự động xử lý thứ tự thực thi cho các phương thức vòng đời dựa trên các phụ thuộc của Singleton.

---

## 🚀 Bắt Đầu

Để sử dụng module này, bạn chỉ cần đảm bảo đã cài đặt [Unity Singleton Framework](https://www.google.com/search?q=https://github.com/your-repo/your-singleton-framework%23unity-singleton-framework) cốt lõi trong dự án của mình.

---

## 🛠️ Cách Sử Dụng

Module này giới thiệu ba giao diện chính mà các Singletons của bạn (cả dựa trên MonoBehaviour và C# thuần) có thể triển khai để móc nối vào các sự kiện vòng đời ứng dụng cụ thể.

### 1. Khởi Tạo Singletons với `IInit`

Bất kỳ Singleton nào triển khai `IInit` sẽ có phương thức `Init(CancellationToken)` được gọi tự động khi Unity khởi động.

* **Mục đích:** Lý tưởng cho các tác vụ khởi tạo bất đồng bộ, tải dữ liệu, hoặc thiết lập SDK bên ngoài.
* **Thứ tự thực thi:** Phương thức `Init` được gọi theo thứ tự **từ dưới lên (bottom-up)**. Tức là nếu Singleton A phụ thuộc vào B, `Init` của B sẽ được gọi và hoàn tất trước khi `Init` của A được gọi.

```csharp
// Ví dụ: SaveService (C# thuần) và GameDataService (MonoBehaviour)
// Logger đơn giản để ghi log.
public class SaveService : IMySingleton, IInit
{
    [Inject] private Logger _logger;
    public async Task Init(CancellationToken cancellationToken = default) { /* ... */ }
}
public class GameDataService : MonoSingleton, IInit, IPostInjectSingleton
{
    [Inject] private SaveService _saveService;
    [Inject] private Logger _logger;
    public async Task Init(CancellationToken cancellationToken = default) { /* ... */ }
    public void OnPostInject() { /* ... */ }
}
```

---

### 2. Xử Lý Tiếp Tục Ứng Dụng với `IPioneer`

Các Singletons triển khai `IPioneer` sẽ có phương thức `OnPreContinue()` được gọi khi người dùng quay lại trò chơi sau khi ứng dụng bị tạm dừng.

* **Mục đích:** Lý tưởng để khởi tạo lại SDK bên ngoài, làm mới dữ liệu hoặc tiếp tục trạng thái trò chơi.
* **Thứ tự thực thi:** Phương thức `OnPreContinue()` được gọi theo thứ tự **từ dưới lên (bottom-up)**, đảm bảo các hệ thống cấp thấp sẵn sàng trước khi hệ thống cấp cao hơn tiếp tục.

```csharp
// Ví dụ: SaveService và GameDataService được cập nhật để triển khai IPioneer
public class SaveService : IMySingleton, IInit, IPioneer
{
    [Inject] private Logger _logger;
    public void OnPreContinue() { _logger.Log("[SaveService.OnPreContinue] App is resuming."); }
}
public class GameDataService : MonoSingleton, IInit, IPioneer, IPostInjectSingleton
{
    [Inject] private SaveService _saveService;
    [Inject] private Logger _logger;
    public void OnPreContinue() { _logger.Log("[GameDataService.OnPreContinue] App is resuming."); }
}
```

---

### 3. Xử Lý Kết Thúc Ứng Dụng với `ITerminal`

Các Singletons triển khai `ITerminal` sẽ có phương thức `OnPostStop()` được gọi khi ứng dụng tạm dừng hoặc dừng.

* **Mục đích:** Lý tưởng để lưu trạng thái trò chơi, đẩy dữ liệu phân tích, hoặc thực hiện các thao tác dọn dẹp trước khi ứng dụng thoát.
* **Thứ tự thực thi:** Phương thức `OnPostStop()` được gọi theo thứ tự **từ trên xuống (top-down)**. Tức là nếu Singleton A phụ thuộc vào B, `OnPostStop()` của A sẽ được gọi *trước* B.

```csharp
// Ví dụ: SaveService và GameDataService được cập nhật để triển khai ITerminal
public class SaveService : IMySingleton, IInit, IPioneer, ITerminal
{
    [Inject] private Logger _logger;
    public void OnPostStop() { _logger.Log("[SaveService.OnPostStop] Finalizing save operations."); }
}
public class GameDataService : MonoSingleton, IInit, IPioneer, ITerminal, IPostInjectSingleton
{
    [Inject] private SaveService _saveService;
    [Inject] private Logger _logger;
    public void OnPostStop() { _logger.Log("[GameDataService.OnPostStop] Saving current game state."); }
}
```

---

### 4. Phản Ứng với Thay Đổi Cảnh với `ISceneSingleton`

Các Singletons triển khai `ISceneSingleton` sẽ có phương thức `OnNewScene(Scene oldScene, Scene newScene)` được gọi bất cứ khi nào có chuyển cảnh.

* **Mục đích:** Lý tưởng cho thiết lập/dọn dẹp hoặc tải/dỡ dữ liệu cụ thể theo cảnh.
* **Thứ tự thực thi:** Phương thức `OnNewScene()` được gọi theo thứ tự **từ dưới lên (bottom-up)**.

```csharp
// Ví dụ: AudioManager và UIController phản ứng với chuyển cảnh
public class AudioManager : MonoSingleton, ISceneSingleton
{
    [Inject] private Logger _logger;
    public void OnNewScene(Scene oldScene, Scene newScene) { _logger.Log($"[AudioManager.OnNewScene] Scene changed to '{newScene.name}'."); }
}
public class UIController : MonoSingleton, ISceneSingleton, IPostInjectSingleton
{
    [Inject] private AudioManager _audioManager;
    [Inject] private Logger _logger;
    public void OnNewScene(Scene oldScene, Scene newScene) { _logger.Log($"[UIController.OnNewScene] Updating UI for new scene."); }
    public void OnPostInject() { /* ... */ }
}
```

---

## Cách Hoạt Động (Tổng Quan)

Module này tích hợp với các sự kiện vòng đời của Unity. Nó xác định tất cả các Singletons đã đăng ký implement các giao diện `IInit`, `IPioneer`, `ITerminal`, hoặc `ISceneSingleton`, sau đó điều phối việc thực thi các phương thức tương ứng của chúng tại thời điểm thích hợp, tôn trọng thứ tự phụ thuộc đã được chỉ định.

---

## ✅ Lợi Ích

* **Quản lý vòng đời có cấu trúc:** Cung cấp các điểm vào và ra rõ ràng cho logic Singleton.
* **Đảm bảo thứ tự thực thi:** Đảm bảo các quy trình khởi tạo, dọn dẹp và xử lý cảnh chạy đúng thứ tự dựa trên phụ thuộc, tránh lỗi.
* **Giảm kết nối thủ công:** Không cần tự đăng ký các sự kiện vòng đời của Unity.
* **Cải thiện khả năng bảo trì:** Tập trung các mối quan tâm về vòng đời trong chính các Singletons.
* **Hỗ trợ khởi tạo bất đồng bộ:** Giao diện `IInit` cho phép các tác vụ khởi động không chặn, cải thiện độ phản hồi của trò chơi.

---