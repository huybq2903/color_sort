
# Unity Base System Module

Chào mừng đến với **Unity Base System Module**\! Module này là một bộ sưu tập các tiện ích nền tảng và các repository dữ liệu cốt lõi, được xây dựng dựa trên [Unity Singleton Framework](https://www.google.com/search?q=link_to_singleton_framework_readme) và [Unity Singleton Lifecycle Management Module](https://www.google.com/search?q=link_to_lifecycle_management_readme). Nó cung cấp các giải pháp mạnh mẽ cho việc quản lý dữ liệu, thao tác file an toàn, thu thập thông tin thiết bị/ứng dụng, và quản lý các chỉ số người chơi quan trọng, giúp đơn giản hóa quá trình phát triển các hệ thống cơ bản trong game.

-----

## 🌟 Tính năng nổi bật

* **Quản lý File an toàn**: Đọc, ghi, nối thêm và xóa file với khả năng **mã hóa AES** tích hợp, bao gồm hỗ trợ cho các file tạm thời.
* **Hệ thống Pool dữ liệu mạnh mẽ**: Cung cấp các wrapper dữ liệu an toàn luồng (`BasicPoolData` và `DelegatePoolData`) để quản lý các mục dữ liệu được lưu trong bộ nhớ đệm và tương tác với một `IDataPool` cơ bản, giảm thiểu truy cập lặp lại và đảm bảo tính nhất quán.
* **Thu thập thông tin đa dạng**: Tự động lấy thông tin chi tiết về **ứng dụng**, **thiết bị** (bao gồm cả ID thiết bị duy nhất trên nhiều nền tảng).
* **Repository dữ liệu người chơi toàn diện**: Quản lý các chỉ số quan trọng của người chơi như dữ liệu **quảng cáo (Ad LTV, lượt xem quảng cáo)**, **thông tin chung** (Account ID, Max Level, Advertising ID), **mua sắm trong ứng dụng (IAP LTV, lượt mua, giao dịch đầu tiên)** và **dữ liệu phiên chơi (thời gian chơi, số ngày hoạt động, ID phiên)**.
* **Hệ thống lưu trữ bền vững linh hoạt**: Cung cấp **hai triển khai `IDataPool`** khác nhau: một dựa trên file mã hóa tùy chỉnh (`FDataPool`) và một tích hợp với thư viện lưu trữ hiện có (`SaveLoadLibPool`), cho phép bạn chọn giải pháp phù hợp nhất.
* **Các dịch vụ tổng hợp**: Cung cấp các dịch vụ facade như `FPlayerInfoService` để hợp nhất quyền truy cập vào tất cả dữ liệu người chơi liên quan, và `PlayerSessionService` để quản lý logic phiên chơi.
* **Lifecycle-Aware**: Các repository, data pool và dịch vụ tích hợp với Singleton Lifecycle Management để thực hiện khởi tạo bất đồng bộ (ví dụ: lấy Advertising ID) và lưu dữ liệu khi ứng dụng dừng.
* **Tùy chỉnh linh hoạt**: Sử dụng các thuộc tính và delegate để điều chỉnh hành vi của các hệ thống.

-----

## 🛠️ Hướng dẫn sử dụng

Module này cung cấp nhiều thành phần để xây dựng các hệ thống cốt lõi của bạn.

### 1\. `FKeyAttribute` (Thuộc tính tùy chỉnh)

`FKeyAttribute` được sử dụng để đánh dấu các trường trong các lớp mà bạn muốn serialize hoặc ánh xạ tới `Dictionary<string, object>`. Nó cho phép bạn tùy chỉnh cách xử lý dữ liệu của trường đó, chẳng hạn như bỏ qua nó, đổi tên khóa, hoặc xóa nếu giá trị là null.
Attribute naỳ chỉ các tác dụng khi được sử dụng thông qua hàm `FKeyService.Encode`.
```csharp
// FKeyAttribute.cs
using System;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Model.Attributes
{
    [AttributeUsage(AttributeTargets.Field)]
    public class FKeyAttribute : Attribute
    {
        public bool Ignore { get; set; } // Bỏ qua trường này khi xử lý
        public string Name { get; set; } // Đặt tên khóa thay thế cho trường
        public bool RemoveIfNull { get; set; } // Xóa khóa nếu giá trị của trường là null
    }
}
```

### 2\. Quản lý File (`IFFile`, `FLocalFile`, `FTempFile` và `FLocalFileRepository`)

Module cung cấp một bộ công cụ mạnh mẽ để thao tác file, bao gồm mã hóa AES tích hợp để bảo mật dữ liệu.

* **`IFFile`**: Interface đại diện cho một file trên hệ thống, với các phương thức để lưu và tải dữ liệu đã mã hóa hoặc thô.
* **`FLocalFile`**: Triển khai `IFFile` cho file local thuần.
* **`FAesSimpleFile`** / **`FFileWrapper`**: Decorator bọc một `IFFile` khác — `FAesSimpleFile` thêm mã hóa AES.
* **`FTempFile`**: Kế thừa `FFileWrapper` và `IDisposable`, được thiết kế cho các file tạm thời và tự động xóa khi đối tượng được `Dispose`.
* **`FLocalFileRepository`**: Là một Singleton (`IMySingleton`) đóng vai trò là nhà máy để tạo các instance `IFFile` / `FTempFile`, quản lý các đường dẫn lưu trữ bền vững (`Application.persistentDataPath`) và tạm thời (`Application.temporaryCachePath`).

<!-- end list -->

```csharp
// IFFile.cs (Trích đoạn minh họa — namespace thật là Falcon.Helpers.Devkit)
namespace Falcon.Helpers.Devkit
{
    public interface IFFile
    {
        // Exists(), Delete() và các phương thức khác
        void Save(object data);   // Serialize object thành JSON, (tùy triển khai) mã hóa và ghi vào file
        T Load<T>();              // Đọc file, giải mã, deserialize JSON thành object
        string LoadRaw();         // Đọc nội dung file mà không giải mã
    }
}

// FLocalFileRepository.cs (Trích đoạn minh họa)
using UnityEngine;

namespace Falcon.Helpers.Devkit
{
    public class FLocalFileRepository : IMySingleton
    {
        private readonly string _persistentDataPath;
        private readonly string _tempPath;

        public FLocalFileRepository()
        {
            _persistentDataPath = Application.persistentDataPath;
            _tempPath = Application.temporaryCachePath;
        }

        public IFFile GetFile(string fileName) { /* ... Trả về IFFile với đường dẫn kết hợp ... */ }
        public FTempFile GetTempFile(string fileName) { /* ... Trả về FTempFile với đường dẫn tạm thời ... */ }
    }
}
```

### 3\. Data Pooling & Caching (`IDataPool`, `BasicPoolData`, `DelegatePoolData`, `FDataPool`)

Hệ thống Data Pool cung cấp một cách đáng tin cậy và hiệu quả để quản lý dữ liệu trong bộ nhớ đệm và lưu trữ bền vững.

* **`IDataPool`**: Interface trung tâm, định nghĩa các hoạt động cho một kho lưu trữ cặp khóa-giá trị an toàn luồng. Nó hỗ trợ các phương thức `GetOrDefault`, `GetOrSet`, `HasKey`, `Save`, `Delete`, và `Compute` (cho phép cập nhật giá trị một cách nguyên tử).

* **`BasicPoolData<T>`**: Một trình bao bọc đơn giản cho một mục dữ liệu được lưu trong bộ nhớ đệm. Nó tương tác với `IDataPool` để tải và lưu giá trị.

* **`DelegatePoolData<T>`**: Một phiên bản nâng cao hơn của `BasicPoolData`, cho phép bạn cung cấp các delegate tùy chỉnh để xác định logic "get" (khi giá trị không có trong bộ nhớ đệm) và "set" (khi giá trị được cập nhật). Điều này cung cấp sự linh hoạt cao cho các trường hợp sử dụng phức tạp.

* **`FDataPool`**: Triển khai `IDataPool` mặc định của Devkit, sử dụng **hệ thống file** (`IFFile`) để lưu trữ dữ liệu. Nó quản lý một `ConcurrentDictionary` trong bộ nhớ và lưu dữ liệu vào một file đã mã hóa (`DataNew`) khi ứng dụng dừng (`OnPostStop`) cùng chu kỳ sync định kỳ. Nó cũng có logic di chuyển dữ liệu từ các file cũ (`Data.txt`).

* Lưu ý: một triển khai thay thế **`SaveLoadLibPool`** (tích hợp `SaveLoadHandler`) nằm ở **module RemoteConfig** (`Assets/Falcon/Modules/Core/RemoteConfig/Scripts/Runtime/Repository/SaveLoadLibPool.cs`), và khi module đó có mặt nó sẽ **thay thế `FDataPool`** thông qua `ISingletonShellSourceDisabler`.

<!-- end list -->

```csharp
// IDataPool.cs (Trích đoạn minh họa)
using System;
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository
{
    public interface IDataPool : IMySingleton
    {
        T GetOrDefault<T>(string key, T defaultValue);
        T GetOrSet<T>(string key, T valueIfNotExist);
        bool HasKey(string key);
        T Compute<T>(string key, Func<T, T> function) where T : class;
        T? Compute<T>(string key, Func<T?, T?> function) where T : struct;
        void Save<T>(string key, T value);
        void Delete(string key);
    #if UNITY_EDITOR
        void Clear();
    #endif
    }
}

// BasicPoolData.cs (Trích đoạn minh họa)
using Falcon.Helpers.Devkit.Core.Scripts.Runtime.Model; // SealedBox

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Model.PoolData
{
    public class BasicPoolData<T>
    {
        private readonly IDataPool _dataPool;
        private SealedBox<T> _box = SealedBox.Empty<T>();

        public BasicPoolData(IDataPool dataPool, string poolKey, T defaultVal) { /* ... */ }
        public T Value { get; set; } // Lấy hoặc đặt giá trị, tương tác với data pool
        public T Compute(Func<T, T> computation) { /* ... */ } // Tính toán và cập nhật an toàn luồng
    }
}

// FDataPool.cs (Trích đoạn minh họa)
using System.Collections.Concurrent;
using System.Collections.Generic;
using Falcon.Helpers.Devkit.SingletonExtension.Scripts.Runtime.Model; // ITerminal

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository
{
    public class FDataPool : IDataPool, ITerminal
    {
        private readonly ConcurrentDictionary<string, string> _cache;
        private readonly IFFile _file;

        public FDataPool(FLocalFileRepository fFileRepository) { /* ... Khởi tạo và xử lý di chuyển dữ liệu cũ ... */ }
        // ... Triển khai các phương thức IDataPool ...
        public void OnPostStop() { _file.Save(new Dictionary<string, string>(_cache)); } // Lưu dữ liệu vào file khi dừng
    }
}
```

### 4\. Các Repository dữ liệu người chơi

Module cung cấp một bộ các interface và lớp triển khai để quản lý các khía cạnh khác nhau của dữ liệu người chơi và ứng dụng. Tất cả các interface này đều kế thừa từ `IMySingleton` (từ Unity Singleton Framework) để đảm bảo một điểm truy cập thống nhất.
Khi cần ghi đè thông tin mặc định của hệ thống, hãy tạo 1 class kế thừa interface tương ứng và đánh attribute [Primary] để ghi đè lên class mặc định.
#### a. Thông tin ứng dụng (`IFAppInfoRepository`)

Cung cấp các thuộc tính để truy cập thông tin cơ bản của ứng dụng (Package Name, Game Name, Platform, App Version).

```csharp
// IFAppInfoRepository.cs
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces
{
    public interface IFAppInfoRepository : IMySingleton
    {
        string PackageName { get; }
        string GameName { get; }
        string Platform { get; }
        string AppVersion { get; }
    }
}
```

#### b. Thông tin thiết bị (`IFDeviceInfoRepository`)

Cung cấp thông tin chi tiết về thiết bị (Device Name, OS, Model, Screen dimensions, GPU, CPU, Language, IDFV, DeviceId). `DeviceId` có một cơ chế phức tạp để tạo ID duy nhất và ổn định trên nhiều nền tảng.

```csharp
// IFDeviceInfoRepository.cs
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces
{
    public interface IFDeviceInfoRepository : IMySingleton
    {
        string DeviceName { get; }
        string DeviceId { get; } // ID thiết bị duy nhất
        // ... các thuộc tính khác ...
    }
}
```

#### c. Dữ liệu quảng cáo người chơi (`IFPlayerAdRepository`)

Theo dõi các chỉ số liên quan đến quảng cáo của người chơi, bao gồm Ad LTV (Lifetime Value) và số lượt xem của từng loại quảng cáo (`AdType`).

```csharp
// IFPlayerAdRepository.cs
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Model.Enums;
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces
{
    public interface IFPlayerAdRepository : IMySingleton
    {
        double? AdLtv { get; }
        int AdCountOf(AdType adType);
    }
}
```

#### d. Dữ liệu chung người chơi (`IFPlayerGeneralRepository`)

Quản lý các thông tin tổng quát về người chơi như Account ID, Max Passed Level, Install Version và Advertising ID. `AdvertisingID` được lấy bất đồng bộ khi khởi tạo.

```csharp
// IFPlayerGeneralRepository.cs
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;
using JetBrains.Annotations;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces
{
    public interface IFPlayerGeneralRepository : IMySingleton
    {
        string AccountID { get; }
        int MaxPassedLevel { get; }
        string InstallVersion { get;}
        [CanBeNull] string AdvertisingID { get; }
    }
}
```

#### e. Dữ liệu mua sắm trong ứng dụng (IAP) người chơi (`IFPlayerIapRepository`)

Theo dõi chi tiết các giao dịch mua trong ứng dụng, bao gồm IAP LTV (sử dụng `InAppData`), tổng số giao dịch, cấp độ và ngày/sản phẩm của giao dịch đầu tiên.

```csharp
// IFPlayerIapRepository.cs
using System;
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Model; // Cho InAppData
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;
using JetBrains.Annotations;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces
{
    public interface IFPlayerIapRepository : IMySingleton
    {
        [CanBeNull] InAppData InAppLtv { get; }
        int InAppCount { get; }
        int? FirstInAppLv { get; }
        DateTime? FirstInAppDate { get; }
        string FirstInAppDateStr { get; }
        string FirstInAppProduct{ get; }
    }
}
```

#### f. Dữ liệu phiên chơi người chơi (`IFPlayerSessionRepository`)

Quản lý các thông tin về phiên chơi và thời gian chơi, như tổng thời gian chơi theo chế độ, timestamp đăng nhập lần đầu, số ngày hoạt động, ID phiên và thời gian đăng nhập gần nhất.

```csharp
// IFPlayerSessionRepository.cs
using System;
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces
{
    public interface IFPlayerSessionRepository : IMySingleton
    {
        long GetModeTotalSec(string gameMode);
        long FirstLogInMillis { get; }
        int ActiveDays { get; }
        int SessionId { get; }
        DateTime? LastLoginInDateTimeLocal { get; }
    }
}
```

#### g. Mở rộng dữ liệu người chơi (`ICustomInfoService`/`ACustomInfoService`)
* Để mở rộng dữ liệu meta của người chơi, hãy tạo các class implement ICustomInfoService. Hệ thống khi cần thiết sẽ kiểm tra các IFCustomInfoRepository để lấy thông tin.
* ICustomInfoService yêu cầu cung cấp các thông tin dưới dạng Dictionary<string, object> với key là tên thông tin meta và value là giá trị thực tế của thông tin.
* ACustomInfoService kế thừa IFCustomInfoRepository sẽ cung cấp 1 tiện ích nhỏ là thay vì khai báo thông tin dưới dạng dictionary thì có thể khai báo dưới dạng các trường của class, dễ đọc hiểu hơn.

```csharp
namespace Falcon.Helpers.Devkit.BaseSystem
{
    public interface IFCustomInfoRepository : IMySingleton
    {
        public Dictionary<string, object> GetInfo();
    }
    
    public abstract class ACustomInfoService : IFCustomInfoRepository
    {
        public virtual Dictionary<string, object> GetInfo()
        {
            return FKeyService.Encode(this);
        }
    }
    
    //Example for using ACustomInfoService
    public class AppsflyerInfo : ACustomInfoService {
        //Có thể kết hợp thêm FKeyAttribute để custom cho thông tin
        
        //appsFlyerId khi lấy thông tin của người chơi sẽ có key là appsflyerId
        [FKey(Name = "appsflyerId")]
        public string appsFlyerId = Appsflyer.getId();  
        
        //loại bỏ thông in appsFlyerAdCampaign đi nếu giá trị là null để tránh rác thông tin
        [FKey(RemoveIdNull = true)]
        public string appsFlyerAdCampaign = Appsflyer.getAdCampaign();  
    }
}
```

### 5\. Mô hình dữ liệu mua sắm trong ứng dụng (`InAppData`)

Lớp có thể serialize này dùng để tổng hợp dữ liệu IAP của người chơi, bao gồm tổng số tiền, giao dịch lớn nhất và số lượng giao dịch theo mã tiền tệ.

```csharp
// InAppData.cs
using System;
using UnityEngine.Scripting; // Cho [Preserve]

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Model
{
    [Serializable]
    public class InAppData
    {
        public int count;
        public string isoCurrencyCode;
        public decimal max;
        public decimal total;

        [Preserve]
        public InAppData() { }
        public InAppData(decimal total, decimal max, int count, string isoCurrencyCode) { /* ... */ }
        public void Update(decimal amount) { /* ... cập nhật tổng, max, count ... */ }
    }
}
```

### 6\. Các dịch vụ (Services)

Module này cũng cung cấp các dịch vụ để tổng hợp và xử lý dữ liệu từ các repository.

#### a. `BaseSystemLogger`

Một logger tùy chỉnh cho module BaseSystem, giúp dễ dàng theo dõi các thông báo của module với màu sắc đặc trưng trong console Unity.

```csharp
// BaseSystemLogger.cs
using Falcon.Helpers.Devkit.Core.Scripts.Runtime.Services.Logs;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service
{
    public class BaseSystemLogger : MyLogger<BaseSystemLogger>
    {
        protected override string GetColor()
        {
            return "#ecbd77"; // Màu cam vàng cho log của BaseSystem
        }
    }
}
```

#### b. `FKeyService`

Một lớp tiện ích tĩnh để chuyển đổi một đối tượng thành `Dictionary<string, object>`, sử dụng `FKeyAttribute` để kiểm soát quá trình ánh xạ. Điều này rất hữu ích cho việc chuẩn bị dữ liệu gửi đi hoặc lưu trữ linh hoạt.

```csharp
// FKeyService.cs
using System.Collections.Generic;
using System.Reflection;
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Model.Attributes;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service
{
    public static class FKeyService
    {
        public static Dictionary<string, object> Encode(object obj)
        {
            var result = new Dictionary<string, object>();
            // ... Logic sử dụng Reflection và FKeyAttribute để xây dựng dictionary ...
            return result;
        }
    }
}
```

#### c. `CustomInfoService`

Một dịch vụ tổng hợp thông tin từ nhiều `IFCustomInfoRepository` (ví dụ: các repository dữ liệu tùy chỉnh mà bạn tự định nghĩa). Nó cho phép thu thập tất cả thông tin tùy chỉnh vào một dictionary duy nhất.

```csharp
// CustomInfoService.cs
using System.Collections.Generic;
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces;
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service
{
    public class CustomInfoService : IMySingleton
    {
        private readonly IFCustomInfoRepository[] _infoRepositories;

        public CustomInfoService(IFCustomInfoRepository[] infoRepositories)
        {
            _infoRepositories = infoRepositories;
        }

        public Dictionary<string, object> GetInfo()
        {
            var result = new Dictionary<string, object>();
            foreach (var repository in _infoRepositories)
            {
                result.AddAll(repository.GetInfo()); // Giả định AddAll là một extension method
            }
            return result;
        }
    }
}
```

#### d. `IFPlayerSessionService` và `PlayerSessionService`

* **`IFPlayerSessionService`**: Định nghĩa hợp đồng cho dịch vụ quản lý phiên người chơi. Nó cung cấp các thông tin chi tiết về phiên như thời gian bắt đầu, tổng thời gian chơi, ngày đăng nhập, ID phiên, và các chỉ số giữ chân (retention).
* **`FPlayerSessionServiceExtensions`**: Các phương thức mở rộng tiện lợi để chuyển đổi các giá trị `DateTime` cục bộ của dịch vụ phiên sang UTC.
* **`PlayerSessionService`**: Triển khai `IFPlayerSessionService`. Nó tương tác với `IFPlayerSessionRepository` để lưu trữ dữ liệu phiên và tính toán các chỉ số động như tổng thời gian chơi và giữ chân. Lớp này cũng tích hợp với Lifecycle Management (`ITerminal`, `IPioneer`) để cập nhật dữ liệu phiên khi ứng dụng tạm dừng hoặc dừng.

<!-- end list -->

```csharp
// IFPlayerSessionService.cs (Trích đoạn minh họa)
using System;
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service.Interface
{
    public interface IFPlayerSessionService : IMySingleton
    {
        DateTime SessionStartTime { get; }
        TimeSpan TotalPlayTime { get; }
        long FirstLogInMillis { get;}
        int ActiveDays { get;}
        int SessionId { get;}
        string SessionUid { get; } // ID duy nhất cho phiên ứng dụng hiện tại
        DateTime LastLoginInDateTimeLocal { get; }
        // ... các thuộc tính khác như Retention, RetentionChanged, TimeSinceLastPause ...
    }
}

// PlayerSessionService.cs (Trích đoạn minh họa)
using System;
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces;
using Falcon.Helpers.Devkit.SingletonExtension.Scripts.Runtime.Model; // ITerminal, IPioneer

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service
{
    public class PlayerSessionService : ITerminal, IPioneer, IFPlayerSessionService
    {
        private readonly IFPlayerSessionRepository _sessionRepository;

        public PlayerSessionService(IFPlayerSessionRepository sessionRepository) { /* ... Khởi tạo và tính toán trạng thái ban đầu ... */ }
        public DateTime SessionStartTime { get; }
        public TimeSpan TotalPlayTime { /* ... Tính toán từ repository và thời gian hiện tại ... */ }
        // ... Triển khai các thuộc tính khác của IFPlayerSessionService ...

        public void OnPreContinue() { /* ... Cập nhật thời gian tạm dừng ... */ }
        public void OnPostStop() { /* ... Lưu tổng thời gian chơi vào repository ... */ }
    }
}
```

#### e. `FPlayerInfoService` (Facade Service)

Đây là một **dịch vụ facade** trung tâm, cung cấp một điểm truy cập hợp nhất và thuận tiện cho tất cả các repository và dịch vụ liên quan đến dữ liệu người chơi. Thay vì phải inject từng repository riêng lẻ vào các lớp khác, bạn có thể inject `FPlayerInfoService` và truy cập tất cả dữ liệu người chơi từ một nơi duy nhất.

```csharp
// FPlayerInfoService.cs
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Repository.Info.Interfaces;
using Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service.Interface;
using Falcon.Helpers.Devkit.Singleton.Scripts.Runtime.Model.Interfaces;

namespace Falcon.Helpers.Devkit.BaseSystem.Scripts.Runtime.Service
{
    public class FPlayerInfoService : MySingleton<FPlayerInfoService>
    {
        public FPlayerInfoService(IFPlayerAdRepository ad, IFPlayerGeneralRepository general,
            IFPlayerIapRepository iap, CustomInfoService custom, IFPlayerSessionService session)
        {
            Ad = ad;
            General = general;
            Iap = iap;
            Custom = custom;
            Session = session;
        }

        public IFPlayerAdRepository Ad { get; }
        public IFPlayerGeneralRepository General { get; }
        public IFPlayerIapRepository Iap { get; }
        public CustomInfoService Custom { get; }
        public IFPlayerSessionService Session { get; }
    }
}
```

-----

## 💡 Cách thức hoạt động (Tổng quan)

Module **Base System** tận dụng sức mạnh của **Unity Singleton Framework** để tự động khởi tạo, quản lý vòng đời và inject các dependency vào các repository và dịch vụ dữ liệu cần thiết. Nó sử dụng **Unity Singleton Lifecycle Management Module** để thực hiện các tác vụ khởi tạo phức tạp (như lấy Advertising ID) và các tác vụ lưu dữ liệu khi ứng dụng dừng (`OnPostStop`), đảm bảo dữ liệu được duy trì một cách chính xác.

Dữ liệu người chơi được quản lý thông qua các lớp `BasicPoolData` và `DelegatePoolData`, cho phép caching an toàn luồng và tương tác hiệu quả với `IDataPool` (được triển khai bởi `FDataPool` hoặc `SaveLoadLibPool`). Các repository này được thiết kế để chịu trách nhiệm duy nhất cho các loại dữ liệu cụ thể, tuân thủ nguyên tắc SOLID.

Quản lý file được xử lý bởi `FLocalFileRepository`, cung cấp một lớp trừu tượng cho các thao tác file cơ bản và tích hợp mã hóa để bảo mật dữ liệu.

Các dịch vụ như `PlayerSessionService` và `FPlayerInfoService` đóng vai trò là lớp nghiệp vụ và facade, tổng hợp dữ liệu từ các repository và cung cấp một API rõ ràng, dễ sử dụng cho phần còn lại của ứng dụng.

-----

## ✅ Lợi ích khi sử dụng Module này

* **Đơn giản hóa quản lý dữ liệu**: Tự động hóa việc lưu trữ, tải và quản lý các chỉ số người chơi quan trọng.
* **Dữ liệu an toàn và đáng tin cậy**: Mã hóa file tích hợp và cơ chế pool dữ liệu an toàn luồng giúp bảo vệ tính toàn vẹn của dữ liệu.
* **Thu thập thông tin chính xác**: Cung cấp các repository được tối ưu hóa để lấy thông tin ứng dụng và thiết bị một cách đáng tin cậy trên nhiều nền tảng.
* **Code sạch và cấu trúc rõ ràng**: Các interface, lớp triển khai và dịch vụ phân tách rõ ràng trách nhiệm, giúp mã nguồn dễ đọc, dễ bảo trì và dễ mở rộng.
* **Tích hợp dễ dàng**: Dựa trên các module Singleton hiện có, việc tích hợp vào dự án của bạn trở nên liền mạch.

-----
