# Falcon Remote Config & A/B Testing Framework

Chào mừng bạn đến với **Falcon Remote Config CMS**\! Framework mạnh mẽ này cung cấp khả năng quản lý và truy cập các **biến cấu hình động** từ máy chủ từ xa, đồng thời hỗ trợ **A/B Testing** linh hoạt. Với framework này, bạn có thể thay đổi hành vi ứng dụng, triển khai các tính năng mới và chạy thử nghiệm mà không cần cập nhật ứng dụng.

-----

## 🌟 Các Tính Năng Nổi Bật

* **Tải cấu hình động**: Tự động tải và cập nhật các biến cấu hình từ máy chủ từ xa.
* **Tích hợp A/B Testing**: Dễ dàng chạy các thử nghiệm A/B bằng cách phân phối các cấu hình khác nhau cho các phân đoạn người dùng cụ thể.
* **Cơ chế dự phòng (Fallback)**: Cung cấp các giá trị mặc định an toàn khi cấu hình từ máy chủ không thể truy cập hoặc khi một biến không được định nghĩa.
* **Truy cập kiểu dữ liệu an toàn**: Truy cập các biến cấu hình với an toàn kiểu dữ liệu thông qua các lớp cấu hình tùy chỉnh.
* **Cập nhật theo sự kiện**: Thông báo cho ứng dụng của bạn khi cấu hình được cập nhật, cho phép phản ứng động.
* **Truy cập đơn giản**: Chủ yếu được thiết kế để truy cập dễ dàng thông qua `Instance` tĩnh, đồng thời vẫn tương thích với dependency injection.

-----

## 🚀 Bắt Đầu

### 1\. Định nghĩa Cấu hình phía Client

Để sử dụng cấu hình động, bạn cần tạo một lớp kế thừa class **`FConfigCms`**. Lớp này sẽ chứa các trường (fields) public phản ánh các cấu hình từ xa đã được định nghĩa trên máy chủ của bạn. Bạn cũng có thể khai báo các giá trị mặc định cho các trường này.

**Lưu ý quan trọng:**

* **Tên trùng khớp**: Tên của các cấu hình từ xa trên máy chủ và tên các trường trong lớp client của bạn **phải giống hệt nhau** (phân biệt chữ hoa/thường).
* **Public Access**: Tất cả các trường cấu hình phải có access modifier là **`public`**.
* **Serializable**: Lớp cấu hình của bạn phải được đánh dấu bằng thuộc tính **`[Serializable]`**.
* **Constructor không tham số**: Lớp phải có một **constructor không tham số**. Điều này rất cần thiết để framework có thể tạo một instance của lớp cấu hình và điền dữ liệu từ máy chủ.
* **Kiểu dữ liệu tương thích**: Kiểu dữ liệu của các trường phía client của bạn phải **tương thích** với kiểu dữ liệu của cấu hình từ xa trên máy chủ (ví dụ: `string` của server sang `string` của client, `int` của server sang `int` hoặc `long` của client). Các giá trị không tương thích từ máy chủ sẽ khiến việc tạo instance thất bại.

<!-- end list -->

```csharp
// Ví dụ: Định nghĩa lớp GameConfig
using System; // Cần cho [Serializable]
// Giả sử IFalconConfig nằm trong namespace này
using Falcon.Modules.Core.RemoteConfigCms; 

[Serializable]
public class GameConfig : FConfigCms
{
    // Cần có một constructor không tham số (được tạo tự động nếu không có constructor nào khác)
    public GameConfig() { }

    // 'config1' (int) từ server
    public int config1 = 10; // Giá trị mặc định nếu server không cung cấp hoặc lỗi

    // 'config2' (string) từ server
    public string config2 = "Default String Value"; // Giá trị mặc định

    // Bạn có thể thêm nhiều trường cấu hình khác tại đây
    public bool enableTutorial = true;
    public float gameSpeedMultiplier = 1.0f;
    
    //sự kiện xảy ra khi có giá trị từ server trả về
    public override void OnData()
    {
            
    }
}
```

### 2\. Khởi tạo Framework

`FConfigControllerCms` (phần cốt lõi của framework này) sẽ tự động khởi tạo trong quá trình khởi động ứng dụng, xử lý việc tìm nạp và quản lý cấu hình.

-----

## 🛠️ Cách Sử Dụng

Chức năng của framework chủ yếu được truy cập thông qua thuộc tính tĩnh **`FConfigControllerCms.Instance`**.

```csharp

// FConfigController (lớp cốt lõi của framework)
// Lớp này sẽ được cung cấp bởi framework và quản lý việc tải cấu hình nội bộ.
public class FConfigControllerCms : MonoSingleton<FConfigControllerCms>
{
    /// <summary>
    /// Kiểm tra trạng thái cập nhật cấu hình từ máy chủ.
    /// </summary>
    public ExecState InitState { get; }

    /// <summary>
    /// Sự kiện được kích hoạt khi hệ thống cập nhật cấu hình thành công từ máy chủ.
    /// </summary>
    public event Action OnUpdateFromNet;

    /// <summary>
    /// Lấy tất cả các cấu hình dưới dạng Dictionary<string, object>.
    /// Bao gồm cả cấu hình A/B test và cấu hình không phải test.
    /// </summary>
    public Dictionary<string, object> Configs { get; }

    /// <summary>
    /// Truy xuất một instance của lớp IFalconConfig với các giá trị được điền từ máy chủ.
    /// </summary>
    /// <typeparam name="T">Lớp cấu hình cần truy xuất (phải implement IFalconConfig và có constructor không tham số).</typeparam>
    /// <returns>Một instance của lớp cấu hình T với các giá trị từ máy chủ.</returns>
    public T Config<T>() where T : IFalconConfig, new()
}
```

### 1\. Truy cập Cấu hình Động

Bạn có thể dễ dàng truy cập các giá trị cấu hình bằng cách sử dụng **`FConfigControllerCms.Instance`**.

```csharp
// Ví dụ: Sử dụng FConfigControllerCms.Instance trong logic trò chơi của bạn
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic; // Cho Dictionary

public class GameLogic : MonoBehaviour // Hoặc bất kỳ component/lớp nào khác
{
    // Bạn có thể truy cập trực tiếp FConfigControllerCms.Instance
    // Hoặc inject nó nếu bạn đang sử dụng hệ thống Dependency Injection.
    // Để đơn giản, chúng ta sẽ sử dụng Instance tĩnh ở đây.

    async void Start()
    {
        Debug.Log("[GameLogic] Đang chờ Falcon Remote Config tải...");

        // Chờ cho đến khi cấu hình tải xong (Succeed, Failed, hoặc Cancelled)
        while (!FConfigControllerCms.Instance.InitState.IsDone())
        {
            await Task.Delay(100); // Đợi một chút trước khi kiểm tra lại
            // Tùy chọn, bao gồm CancellationToken nếu bạn muốn cho phép hủy bỏ
        }

        if (FConfigControllerCms.Instance.InitState.IsSuccess())
        {
            Debug.Log("[GameLogic] Falcon Remote Config đã tải thành công.");
            ApplyDynamicConfigs();
        }
        else
        {
            Debug.LogError($"[GameLogic] Tải Falcon Remote Config thất bại hoặc bị hủy. Trạng thái: {FConfigControllerCms.Instance.InitState}");
        }

        // Đăng ký để nhận thông báo cập nhật cấu hình
        FConfigControllerCms.Instance.OnUpdateFromNet += HandleConfigUpdated;
    }

    private void HandleConfigUpdated()
    {
        Debug.Log("[GameLogic] Falcon Remote Config đã cập nhật! Đang áp dụng thay đổi.");
        ApplyDynamicConfigs();
    }

    private void ApplyDynamicConfigs()
    {
        // Truy xuất một instance của lớp cấu hình tùy chỉnh của bạn
        GameConfig currentConfig = FConfigControllerCms.Instance.Config<GameConfig>();

        Debug.Log($"[Config] config1: {currentConfig.config1}");
        Debug.Log($"[Config] config2: {currentConfig.config2}");
        Debug.Log($"[Config] Bật hướng dẫn: {currentConfig.enableTutorial}");
        Debug.Log($"[Config] Hệ số tốc độ trò chơi: {currentConfig.gameSpeedMultiplier}");

        // Lấy ID chiến dịch A/B Test đang chạy
        string runningABTest = FConfigControllerCms.Instance.RunningAbTesting;
        if (!string.IsNullOrEmpty(runningABTest))
        {
            Debug.Log($"[ABTest] Đang chạy chiến dịch A/B Test: {runningABTest}");
        }

        // Truy cập cấu hình dưới dạng Dictionary nếu cần truy cập linh hoạt hơn
        Debug.Log("--- Cấu hình không phải Test (dưới dạng Dictionary) ---");
        foreach (var entry in FConfigControllerCms.Instance.NonTestConfigs)
        {
            Debug.Log($"- {entry.Key}: {entry.Value}");
        }
        
        Debug.Log("--- Cấu hình Test (dưới dạng Dictionary) ---");
        foreach (var entry in FConfigControllerCms.Instance.TestingConfigs)
        {
            Debug.Log($"- {entry.Key}: {entry.Value}");
        }

        // ... Áp dụng các giá trị này vào logic trò chơi của bạn ...
    }

    void OnDestroy()
    {
        // Hủy đăng ký sự kiện để tránh rò rỉ bộ nhớ
        if (FConfigControllerCms.Instance != null)
        {
            FConfigControllerCms.Instance.OnUpdateFromNet -= HandleConfigUpdated;
        }
    }
}
```

-----

## 💡 Cách Hoạt Động (Tổng Quan)

1.  **Tải cấu hình**: Khi `FConfigControllerCms` khởi tạo (thường là khi ứng dụng khởi động), nó tìm nạp cấu hình từ máy chủ từ xa. Trạng thái tải được theo dõi qua thuộc tính **`InitState`**.
2.  **Phân tích & Lưu trữ**: Framework phân tích phản hồi JSON của máy chủ, điền dữ liệu vào các instance của lớp `IFalconConfigCms` mà bạn cung cấp (`Config<T>()`) và các dictionary nội bộ (`Configs`). Các cơ chế caching có thể được triển khai để đảm bảo tính khả dụng ngay cả khi ngoại tuyến.
3.  **Cập nhật động**: Framework có thể định kỳ kiểm tra các bản cập nhật từ máy chủ. Khi phát hiện thay đổi, sự kiện **`OnUpdateFromNet`** được kích hoạt, cho phép ứng dụng của bạn phản ứng động.

-----

## ✅ Lợi Ích

* **Triển khai nhanh chóng**: Thay đổi hành vi và nội dung ứng dụng mà không cần gửi bản dựng mới lên các cửa hàng ứng dụng.
* **Thử nghiệm linh hoạt**: Tiến hành các thử nghiệm A/B, xác thực các tính năng mới và tối ưu hóa trải nghiệm người dùng trong thời gian thực.
* **Kiểm soát tập trung**: Quản lý tất cả cấu hình và thử nghiệm từ một nền tảng máy chủ duy nhất.
* **Giảm thiểu rủi ro**: Triển khai an toàn các tính năng mới cho một phân đoạn nhỏ người dùng trước khi phát hành rộng rãi.
* **Tối ưu hóa liên tục**: Liên tục cải thiện ứng dụng của bạn dựa trên dữ liệu thử nghiệm thực tế.
* **Quản lý trạng thái rõ ràng**: Thuộc tính **`InitState`** và sự kiện **`OnUpdateFromNet`** cung cấp phản hồi rõ ràng về quá trình tải và cập nhật cấu hình.
-----