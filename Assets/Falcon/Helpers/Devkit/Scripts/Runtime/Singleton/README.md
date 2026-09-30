[English](README.en.md) | **Tiếng Việt**
---

# Unity Singleton Framework

Chào mừng bạn đến với **Unity Singleton Framework**! Thư viện này được thiết kế để đơn giản hóa việc quản lý Singleton và triển khai **Dependency Injection (DI)** trong các dự án Unity của bạn. Nó tự động tạo instance và giải quyết các phụ thuộc, giúp bạn tập trung vào logic game thay vì viết mã lặp đi lặp lại. Kết quả là mã nguồn sạch hơn, dễ bảo trì hơn và có khả năng mở rộng cao cho các thành phần cốt lõi của game bạn.

---

## 🌟 Các Tính Năng Nổi Bật

* **Tự động tạo và quản lý Instance**: Singletons được tạo tự động khi cần, đảm bảo chỉ có một instance duy nhất.
* **Dependency Injection linh hoạt**:
    * Tự động inject các Singletons khác vào nhau bằng **Attributes** hoặc **Constructors**.
    * Hỗ trợ inject **các bộ sưu tập (Arrays/Lists)** các Singletons cùng loại (thực hiện cùng interface hoặc kế thừa từ cùng abstract class).
* **Hỗ trợ cả MonoBehaviour và Plain C\# Classes**: Linh hoạt tích hợp Singletons vào mọi phần kiến trúc của bạn.
* **Khởi tạo sau khi Inject**: Cung cấp cơ chế để thực thi logic khởi tạo ngay sau khi tất cả các phụ thuộc đã được inject hoàn chỉnh.
* **Giải quyết xung đột phụ thuộc**: Cung cấp cơ chế để chỉ định Singleton nào sẽ được sử dụng khi có nhiều hơn một class có thể đáp ứng cùng một phụ thuộc.

---

## 🚀 Bắt Đầu

### 1. Cài Đặt

(Hướng dẫn cài đặt sẽ được thêm vào đây, ví dụ: Clone repository, import package qua UPM, v.v.)

### 2. Thiết Lập Cơ Bản

Bạn cần một **GameObject trung tâm** trong Scene để lưu trữ và quản lý các instance `MonoSingleton` của mình (ví dụ: tạo một GameObject rỗng tên `_Singletons`).

---

## 🛠️ Cách Sử Dụng

Thư viện phân biệt hai loại Singleton chính:

### 1. MonoBehaviour Singletons (Components)

Dành cho các Singletons cần hoạt động như một `Component` trong Unity (ví dụ: cần `Update`, `Coroutine`).

* **Yêu cầu**: Kế thừa từ `MonoSingleton`.
* **Dependency Injection**: Sử dụng attribute **`[Inject]`** trên các trường cần phụ thuộc.
* **Khởi tạo sau Inject**: Thực hiện `IPostInjectSingleton` và dùng `OnPostInject()` cho logic khởi tạo.

**Ví dụ**: `SoundManager` (quản lý âm thanh) và `GameEventsHandler` (cần `SoundManager` để phát âm thanh).

```csharp
// SoundManager.cs
public class SoundManager : MonoSingleton { /* ... */ }

// GameEventsHandler.cs
public class GameEventsHandler : MonoSingleton, IPostInjectSingleton
{
    [Inject] private SoundManager _soundManager; // Tự động inject
    public void OnPostInject() { /* ... */ _soundManager.PlayClickSound(); }
}
```

### 2. Normal C\# Singletons (Plain C\# Objects)

Dành cho các Singletons là các class C# thông thường, không cần chức năng của MonoBehaviour.

* **Yêu cầu**: Thực hiện interface `IMySingleton`.
* **Dependency Injection**: Phụ thuộc được cung cấp qua **constructor** của class.
* **Nhiều Constructor**: Đánh dấu constructor chính bằng attribute **`[SingletonConstructor]`**.

**Ví dụ**: `Logger` (ghi log) và `ConfigManager` (cần `Logger`).

```csharp
// Logger.cs
public class Logger : IMySingleton { /* ... */ }

// ConfigManager.cs
public class ConfigManager : IMySingleton
{
    private readonly Logger _logger;
    [SingletonConstructor] // Constructor chính để inject Logger
    public ConfigManager(Logger logger) { _logger = logger; }
}
```

### 3. Inject Các Bộ Sưu Tập Singleton

Bạn có thể inject một `Array` hoặc `List<T>` chứa tất cả các Singletons (dù là MonoBehaviour hay Plain C#) thực hiện một interface hoặc kế thừa một abstract class cụ thể.

**Ví dụ**: Inject `List<IWeapon>` vào `WeaponRegistry`.

```csharp
public interface IWeapon { /* ... */ }
public class Rifle : MonoSingleton, IWeapon { /* ... */ }
public class Pistol : IMySingleton, IWeapon { /* ... */ }

public class WeaponRegistry : MonoSingleton, IPostInjectSingleton
{
    [Inject] private List<IWeapon> _allWeapons; // Tự động inject danh sách vũ khí
    public void OnPostInject() { /* ... */ }
}
```

### 4. Truy Cập Instance Tĩnh Trực Tiếp (Hạn chế sử dụng)

Framework khuyến khích DI, nhưng bạn vẫn có thể truy cập Instance tĩnh qua thuộc tính `Instance` (ví dụ: `AdLogService.Instance`). **Hạn chế sử dụng** để duy trì code sạch và dễ kiểm thử.

### 5. Giải Quyết Xung Đột Phụ Thuộc

Khi có nhiều Singleton cùng implement một interface/abstract class, dùng attribute **`[Primary]`** trên class bạn muốn chọn làm mặc định. Có thể dùng `Priority` để ưu tiên (số cao hơn được ưu tiên).

**Ví dụ**: Khi yêu cầu `ILogger`, `ConsoleLogger` sẽ được chọn vì có `[Primary(priority: 10)]`.

```csharp
public interface ILogger { void Log(string message); }

[Primary(priority: 10)]
public class ConsoleLogger : IMySingleton, ILogger { /* ... */ }

public class FileLogger : IMySingleton, ILogger { /* ... */ } // Không có [Primary] hoặc Priority thấp hơn

public class DataProcessor : IMySingleton
{
    [Inject] private ILogger _logger; // Sẽ inject ConsoleLogger
    [SingletonConstructor] public DataProcessor(ILogger logger) { _logger = logger; }
}
```

---

## 💡 Cách Hoạt Động (Tổng Quan)

Framework hoạt động bằng cách quét các assemblies của ứng dụng để tự động nhận diện các class kế thừa `MonoSingleton` hoặc implement `IMySingleton`.

1.  **Đăng ký Tự động**: Quét và đăng ký các loại Singleton vào một Container nội bộ.
2.  **Giải quyết Phụ thuộc**: Khi một Singleton được yêu cầu, framework sẽ kiểm tra constructor (`[SingletonConstructor]`) hoặc các trường `[Inject]` để tự động tạo hoặc cung cấp các instance phụ thuộc.
3.  **Quản lý Vòng đời**:
    * `MonoSingleton` được thêm vào GameObject quản lý trung tâm.
    * `IMySingleton` được framework quản lý hoàn toàn.

---

## ✅ Lợi Ích

* **Code Sạch & Cấu trúc Rõ ràng**: Loại bỏ các lệnh gọi `Singleton.Instance` rải rác.
* **Dễ kiểm thử**: DI giúp dễ dàng mock hoặc thay thế các phụ thuộc khi unit test.
* **Giảm Mã Lặp lại**: Không cần viết code thủ công để khởi tạo và quản lý Singleton.
* **Khả năng Mở rộng Linh hoạt**: Dễ dàng thêm các loại Singleton và phụ thuộc mới.

---