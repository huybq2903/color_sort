# Module @GameData

## Tổng quan
- **Tên:** `GameData`
- **Giới thiệu:** Cung cấp một hệ thống mạnh mẽ và linh hoạt để quản lý dữ liệu người chơi trong game, với trọng tâm là quản lý các "Tài nguyên" (ví dụ: tiền tệ, vật phẩm, năng lượng, v.v.).
- **Các thành phần chính:**
    -   `GameDataCore`: Lớp dữ liệu trung tâm lưu trữ tất cả tài nguyên của người chơi.
    -   `IResource` / `AResource`: Interface và lớp cơ sở để định nghĩa một loại tài nguyên.
    -   `ResourceCollector`: Singleton cung cấp quyền truy cập toàn cục để thao tác và lắng nghe sự kiện của tài nguyên, có thêm logging resource sử dụng BigData.
    -   `GameDataCoreRegistry`: Tự động đăng ký các loại tài nguyên mới để phục vụ việc lưu/tải.
    -   `ResourceConfigDatabase`: ScriptableObject chứa siêu dữ liệu (metadata) cho các tài nguyên.

## Quick Start

### 1. Tạo một loại `Resource` mới
Tạo một lớp kế thừa từ `AResource` và thêm attribute `[ResourceInfo]`.  
📌 Lớp này cần được nằm trong namespace chứa `Falcon.`
```csharp
using Falcon.Modules.Core.GameData.Runtime;

[ResourceInfo("gold")] // ID duy nhất
public class GoldResource : AResource
{
    // ... implement logic ...
    public int Quantity { get; set; }

    public override object Get => Quantity;

    protected override bool AddInternal(int amount, string data) 
    { 
        Quantity += amount; 
        return true; 
    }

    protected override bool RemoveInternal(int amount, string data) 
    { 
        Quantity -= amount; 
        return true; 
    }

    protected override int ResetInternal() { 
        var before = Quantity; 
        Quantity = 0; 
        return before; 
    }
}
```

### 2. Tạo hằng số cho tên `Resource` (Tùy chọn nhưng khuyến khích)
Để tránh sử dụng "magic strings" (ví dụ: `"gold"`) khi truy cập tài nguyên, bạn có thể tự động tạo một lớp tĩnh `ResourceName` chứa tất cả các ID tài nguyên dưới dạng hằng số.

Trong Unity Editor, vào menu `Falcon > Modules > Game Resources > Generate Script Names`.

Hệ thống sẽ tạo tệp `ResourceName.cs` tại `Assets/FalconAssets/Modules/Core/GameData/`. Lớp này sẽ trông như sau:

```csharp
namespace Falcon.Modules.Core.GameData.Runtime
{
    public static class ResourceName
    {
        public const string Gold = "gold";
        // ... other resources
    }
}
```

Việc này giúp code của bạn an toàn và dễ bảo trì hơn.

### 3. Cập nhật cơ sở dữ liệu
Trong Unity Editor, vào menu `Falcon > Modules > Game Resources > Generate Database` để hệ thống config `Resource` mới.  
📌 Việc này chỉ tạo `SO` dùng cho config (image/name/etc), mà không add `Resource` vào một cấu trúc data nào.

### 4.  Sử dụng Injector để cung cấp `Resource` ban đầu cho `GameDataCore`
Để cấp `Resource` đơn lẻ cho `GameDataCore` mà không dùng cấu trúc (lớp data) ngoài, bạn cần tạo và khởi tạo một custom `Injector`.

```csharp
// a. Tạo Injector để định nghĩa resource ban đầu
public class CustomResourceInjector : AResourceInject
{
    public override void Inject(Injection injection)
    {
        // Your_Custom_Resource không thuộc bất cứ 1 lớp data nào.
        // Sẽ được inject vào cấu trúc data của `GameDataCore` qua injector này.
        injection.Invoke(new Your_Custom_Resource());
    }
}

// b. Khởi tạo Injector để chạy tự động khi game bắt đầu
public static class CustomDataRunner
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SceneLoad()
    {
        // Set Injector type
        ResourceInjector.SetInject(new CustomResourceInjector());
    }
}
```

📌 Lưu ý: Các cấu trúc data kế thừa FGameData<> sẽ không tự save hoặc update lên server. Phải gọi manually.

### 5. Thao tác với `Resource`
Qua chính `Resource`
```csharp
GoldResource goldResource;

// Thêm 100 vàng
goldResource.Add(100, "custom_data");

// Lấy giá trị trực tiếp
int goldValue = goldResource.Quantity;

// hoặc qua `Get` (boxing)
int goldValue = (int) goldResource.Get;

// Lắng nghe sự kiện thay đổi
goldResource.OnChanged += (amount, data) =>
{
	Debug.Log($"Vàng đã thay đổi với lượng {amount}");
};
```

Sử dụng `ResourceCollector` để tương tác với tài nguyên.
```csharp
using Falcon.Modules.Core.GameData.Runtime; // Cần thiết để sử dụng ResourceName

// Thêm 100 vàng ở shop (where)
ResourceCollector.Instance.AddResources(ResourceName.Gold, 100, "custom_data", "shop");

// Lấy giá trị
var goldValue = ResourceCollector.Instance.GetResourceValueInCollector("gold");
Debug.Log("Số vàng hiện tại: " + goldValue);

// Lắng nghe sự kiện thay đổi
ResourceCollector.Instance.AddResourceChangeListener((id, amount, data) => {
    if(id == ResourceName.Gold) Debug.Log("Vàng đã thay đổi!");
});
```

### 6. (Tùy chọn) Theo dõi `Resource` từ bên ngoài trong một cấu trúc (lớp) data đã có.
Nếu `Resource` nằm trong một cấu trúc dữ liệu (lớp) khác so với `GameDataCore`, bạn vẫn có thể để `ResourceCollector` theo dõi nó.
```csharp
// Resource custom: `MyCustomResource`
[ResourceInfo("my_resource")]
public MyCustomResource : AResource
{
    // logic resource
}

// 1 cấu trúc dữ liệu bên ngoài khác `GameDataCore`
[FGameDataType("data_x")]
public class DataX : FGameData<DataX>
{
    // `Resource` MyCustomResource thuộc về `DataX`
    public MyCustomResource myResource;
}

// Add dataX.myResource và DataX instance cho ResourceCollector tracking
var dataX = DataX.Instance;
ResourceCollector.Instance.AddToResourcesMap<MyCustomResource>(dataX.myResource, dataX);
```

### 7. Save/Update to Server
Có thể dùng `ResourceCollector` để Save/Update Server thông qua các API sau:

```csharp
// Save/Update sever cho 1 FGameData dựa trên resource của nó, ở đây là 'gold', thì sẽ là 'GameDataCore'
ResourceCollector.Instance.SaveAndUpdateServerOfResourceData("gold");

// Save/Update sever cho toàn bộ mapped FGameData tracking (include 'GameDataCore')
ResourceCollector.Instance.SaveResourcesAndUpdateToServer();
```

### 8. Callback Update From Server cho `ResourceCollector`
Trong `ResourceCollector` có callback `onUpdateFromServerCallback` khi data của nó được update từ server. Hãy sử dụng callback này khi cần thiết, như: update UIs, etc...

```csharp
ResourceCollector.onUpdateFromServerCallback += () => {
    Debug.Log("ResourceCollector updated from server!");
};
```

## Danh sách đầy đủ API

### `ResourceCollector` (Singleton)
-   `GetResourceInCollector<R>(string resourceId)`: Lấy resource theo id, trả về một đối tượng mới nếu không tìm thấy.
-   `GetResourceInCollector(string resourceId)`: Lấy resource theo id, trả về `DefaultResource` nếu không tìm thấy.
-   `GetResourceValueInCollector(string resourceId)`: Lấy giá trị của resource.
-   `ResourceAdd(string resourceId, int amount, string data, string where = "where")`: Thêm một lượng vào resource và ghi log.
-   `ResourcesAdd((string resourceId, int amount, string data)[] resources, string where = "where")`: Thêm một lượng vào nhiều resource và ghi log.
-   `ResourceRemove(string resourceId, int amount, string data, string where = "where")`: Trừ một lượng khỏi resource và ghi log.
-   `ResourceSet(string resourceId, int value, string data, string where = "where")`: Thiết lập giá trị cho resource và ghi log.
-   `ResourceReset(string resourceId, string where = "where")`: Reset một resource và ghi log.
-   `AddResourceChangeListener(Action<string, int, string> callback)`: Thêm một listener để lắng nghe sự kiện thay đổi của resource.
-   `RemoveResourceChangeListener(Action<string, int, string> callback)`: Gỡ một listener lắng nghe sự kiện thay đổi của resource.
-   `SaveAndUpdateServerOfResourceData(string resourceId)`: Lưu và cập nhật dữ liệu của một resource cụ thể lên server.
-   `SaveResourcesAndUpdateToServer()`: Lưu và cập nhật dữ liệu của tất cả resource lên server.
-   `GetResourceConfigById(string resourceId)`: Lấy cấu hình của resource theo id.

### `AResource` (Abstract class)
-   `OnChanged`: `Action<int, string>` - delegate được gọi khi giá trị thay đổi.
-   `Get`: `object` - property trả về giá trị hiện tại của tài nguyên.
-   `AddInternal(int amount, string data)`: Logic thêm tài nguyên (cần override).
-   `RemoveInternal(int amount, string data)`: Logic trừ tài nguyên (cần override).
-   `ResetInternal()`: Logic reset tài nguyên (cần override).

### Giao tiếp qua Events
-   **Event:** `GameDataConst.EVENT_GET_RESOURCE`
-   **Mục đích:** Lấy giá trị của một `Resource` một cách phi tập trung.
-   **Sử dụng:** `GameRequest<string, object>.Request(GameDataConst.EVENT_RESOURCE_GET, "gold");`

## Chi tiết

### Cấu hình Siêu dữ liệu (Metadata)
Để chỉnh sửa thông tin cho tài nguyên (ví dụ: đặt tên hiển thị, thêm icon):
-   Mở cơ sở dữ liệu bằng cách vào menu `Falcon/Modules/Game Resources/Open Database`.
-   Thao tác này sẽ chọn tệp `SO_ResourceConfigDatabase.asset`.
-   Chọn tệp và cấu hình các trường dữ liệu trong cửa sổ Inspector. 

## Lỗi thường gặp
### 1. Quên không `Save data` / `Update lên server`
Hãy call manually qua API.

### 2. Không gán vào `AddToResourcesMap` của `ResourceCollector`
Nếu bạn dùng `ResourceCollector` để thao tác với `Resource` của cấu trúc data bên ngoài, thì những `Resource` không gán vào Map của `ResourceCollector` sẽ không được tracking và manipulate.

### 3. `Resource` không inject vào `GameDataCore`
Nếu bạn có 1 `Resource` đơn lẻ, bạn nên gán nó vào `GameDataCore` để có thể save/update. Để gán vào, ta dùng custom `Injector` như ví dụ ở trên. Nên nhớ, hãy gọi `ResourceCollector` ở Start để chắc rằng ko xảy ra lỗi thứ tự call (nếu có).