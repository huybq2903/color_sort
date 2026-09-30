
# Module Core for Unity Development

---

**Author**: leehuyyhoangg
**Email**: hoanglh@falcongames.com
**Company**: Falcon Games
**Date**: 2025-06-12

---

## 🚀 Overview

Module **Core** là một thư viện tập hợp các cấu trúc dữ liệu cơ bản, các tiện ích xử lý đồng thời, các phương thức mở rộng tiện lợi, và các công cụ chung cần thiết cho quá trình phát triển game Unity. Nó được thiết kế để cung cấp một nền tảng vững chắc, giúp cải thiện hiệu suất, tính an toàn luồng, và sự rõ ràng của mã nguồn.

---

## 🔒 Concurrent Utilities (Tiện ích Đồng thời)

Phần này của module cung cấp các công cụ để quản lý các hoạt động và cấu trúc dữ liệu trong môi trường đa luồng một cách an toàn và hiệu quả.

### `AtomicRef<T>`

`AtomicRef<T>` là một tham chiếu tới một giá trị kiểu `T` có thể được truy cập và sửa đổi một cách **nguyên tử (thread-safe)** bằng cách sử dụng một khóa nội bộ. Nó lý tưởng để quản lý một giá trị đơn lẻ, có thể thay đổi và được chia sẻ trong môi trường đa luồng.

* **Tính năng chính**:
  * **Truy cập Atomic**: Cả đọc và ghi giá trị thông qua thuộc tính `Value` đều được bảo vệ bằng khóa.
  * **Cập nhật có điều kiện**: Các phương thức như `SetIfNotEq()`, `Compute()`, `ComputeIfEqual()`, và `ComputeIfDifferent()` cho phép cập nhật giá trị dựa trên các điều kiện so sánh, đảm bảo tính nhất quán.
* **Lưu ý**: Hạn chế các hoạt động kéo dài hoặc gọi mã bên ngoài trong các delegate (`Func` hoặc `Action`) bên trong khóa để tránh giảm hiệu suất hoặc deadlock.

### `MyConcurrentDeque<T>` (Double-Ended Queue)

Một triển khai **deque (double-ended queue)** an toàn luồng, cho phép thêm và xóa các phần tử từ cả hai đầu của bộ sưu tập một cách an toàn trong môi trường đa luồng.

* **Được xây dựng trên `LinkedList<T>`**: Sử dụng `LinkedList<T>` làm cấu trúc dữ liệu cơ bản.
* **An toàn luồng**: Tất cả các thao tác đều được bảo vệ bởi một đối tượng `MyRwLock` nội bộ.
* **Tính năng Deque**: `EnqueueFirst()`, `EnqueueLast()`, `DequeueFirst()`, `DequeueLast()`, `PeekFirst()`, `PeekLast()`, cùng các biến thể `Try...`.
* **Quản lý bộ sưu tập**: `Clear()`, `Contains()`, `CopyTo()`, `Remove()`, và `DrainAll()` (lấy tất cả các phần tử và xóa deque).

### `MyConcurrentQueue<T>` (Queue)

Một triển khai **queue (hàng đợi)** an toàn luồng, tuân theo nguyên tắc "Vào trước ra trước" (FIFO - First-In, First-Out).

* **Được xây dựng trên `LinkedList<T>`**: Tương tự như Deque, sử dụng `LinkedList<T>` làm cơ sở.
* **An toàn luồng**: Mọi thao tác đều được bảo vệ bởi `MyRwLock` nội bộ.
* **Tính năng Queue**: `Enqueue()`, `Dequeue()`, `Peek()`, cùng các biến thể `Try...` và `EnqueueAll()`.
* **Quản lý bộ sưu tập**: `Clear()`, `Contains()`, `ContainsAll()`, `CopyTo()`, `Remove()`, `DrainAll()`, và `Drain(int size)` (lấy một số lượng phần tử cụ thể).

### `MyRwLock` (Custom Reader-Writer Lock)

Một lớp bao bọc (wrapper) xung quanh `System.Threading.ReaderWriterLockSlim`, cung cấp một API đơn giản và tiện lợi hơn để quản lý các khóa đọc/ghi an toàn luồng.

* **Mục đích**: Tối ưu hóa hiệu suất bằng cách cho phép nhiều luồng đọc đồng thời truy cập vào một tài nguyên, nhưng chỉ cho phép một luồng ghi tại một thời điểm.
* **API đơn giản**: Cung cấp các phương thức `LockRead()`, `LockWrite()`, `TryLockRead()`, `TryLockWrite()` nhận vào các `Action` hoặc `Func<T>` delegates, tự động xử lý việc vào/thoát khóa.

---

## 📦 Data Structures (Cấu trúc Dữ liệu)

Các cấu trúc dữ liệu cơ bản nhưng mạnh mẽ để quản lý giá trị và trạng thái.

### `ExecState` (Enum) và `ExecStateExtensions`

* **`ExecState`**: Định nghĩa các trạng thái thực thi có thể có của một hoạt động: `NotStarted`, `Processing`, `Succeed`, `Failed`, `Cancelled`.
* **`ExecStateExtensions`**: Các phương thức mở rộng tiện ích cho `ExecState`:
  * `CanStart()`: Kiểm tra xem hoạt động có thể bắt đầu hoặc khởi động lại không.
  * `IsSuccess()`: Kiểm tra xem hoạt động có thành công không.
  * `IsDone()`: Kiểm tra xem hoạt động đã hoàn thành chưa (bất kể kết quả).

### `LazyVal<T>`

Một lớp an toàn luồng để **khởi tạo lười (lazy initialization)** một giá trị. Giá trị chỉ được tính toán khi nó được truy cập lần đầu tiên và có thể được đặt lại để buộc tính toán lại.

* **Mục đích**: Tối ưu hóa hiệu suất bằng cách trì hoãn việc tính toán một giá trị cho đến khi thực sự cần thiết.
* **An toàn luồng**: Sử dụng mẫu double-checked locking để đảm bảo tính toán giá trị chính xác một lần duy nhất cho mỗi chu kỳ khởi tạo.
* **`Reset()`**: Loại bỏ giá trị đã tính toán hiện tại, buộc `_supplier` phải được gọi lại ở lần truy cập tiếp theo.

### `SealedBox<T>` (Struct) và `SealedBox` (Static Class)

`SealedBox<T>` là một `readonly struct` **bất biến**, hoạt động như một container cho một giá trị **có thể có hoặc không có**. Nó cung cấp một cách rõ ràng và an toàn để xử lý sự hiện diện hay vắng mặt của một giá trị mà không dựa vào `null`.

* **Ưu điểm**: Hiệu suất cao nhờ là `readonly struct`.
* **Tính năng chính**:
  * `HasValue`: Kiểm tra sự hiện diện của giá trị.
  * `Value`: Lấy giá trị (ném lỗi nếu rỗng).
  * `TryGetValue(out T value)`: Phương thức an toàn để lấy giá trị.
  * Hỗ trợ so sánh bằng (`Equals`, `==`, `!=`).
* **`SealedBox` (Static Class)**: Các phương thức tiện ích để tạo và thao tác với `SealedBox<T>`, bao gồm `Empty()`, `Of()`, `OfNullable()`, `Map()`, và `AsNullable()`.

---

## ⏱️ Timing Utilities (Tiện ích Đo thời gian)

### `Timer`

Một lớp đơn giản để đo lường thời gian trôi qua với độ chính xác cao bằng cách sử dụng `System.Diagnostics.Stopwatch`.

* **Mục đích**: Dễ dàng đo thời gian thực hiện của các tác vụ hoặc tổng thời gian chạy của một quy trình.
* **Tính năng**:
  * Hai bộ đếm thời gian: `_totalStopwatch` (đo tổng thời gian) và `_taskStopwatch` (đo thời gian cho tác vụ riêng lẻ, có thể đặt lại).
  * Các phương thức `Reset()`, `Stop()`, `StopMillis()`, `StopAndReset()`, `StopAndResetMillis()`, `LogAndReset(string taskName)`, `LogTotal(string taskName)`, `TotalMillis()`, `TotalTime()`.

### `MyTime` (Static Class)

`MyTime` là một lớp tĩnh cung cấp các thuộc tính và phương thức tiện ích để lấy thời gian hiện tại ở các định dạng khác nhau và chuyển đổi ngày tháng.

* **`CurrentTimeMillis`**: Trả về thời gian hiện tại tính bằng mili giây từ kỷ nguyên Unix (Epoch).
* **`CurrentTimeSec`**: Trả về thời gian hiện tại tính bằng giây từ kỷ nguyên Unix.
* **`CurrentTimeNano`**: Cố gắng trả về thời gian hiện tại tính bằng nano giây (sử dụng `Stopwatch.GetTimestamp()` để có độ chính xác cao).
* **`DateToString(DateTime dateTime)`**: Chuyển đổi đối tượng `DateTime` thành chuỗi ngày tháng định dạng "yyyy-MM-dd".
* **`StringToDate(string dateStr)`**: Chuyển đổi chuỗi ngày tháng sang đối tượng `DateTime`.
* **`DateSinceEpoch(long millisSec)`**: Tính số ngày đã trôi qua kể từ kỷ nguyên Unix dựa trên mili giây.

---

## 🛠️ Extension Methods (Các phương thức mở rộng)

Module này bao gồm nhiều lớp tĩnh cung cấp các phương thức mở rộng tiện ích cho các kiểu dữ liệu và đối tượng phổ biến.

### `ArrayExtension`

Mở rộng `Array` với các tiện ích cơ bản:

* **`SubArray<T>(this T[] data, int index, int length)`**: Trích xuất một mảng con.
* **`Concat<T>(this T[] x, T[] y)`**: Ghép hai mảng.

### `ConcurrentDictionaryExtensions`

Mở rộng `ConcurrentDictionary` để cung cấp các thao tác mạnh mẽ hơn:

* **`Compute<TK, TV>(this ConcurrentDictionary<TK, TV> dict, TK key, Func<TV, TV> computation)`**: Cập nhật hoặc thêm giá trị bằng cách áp dụng một hàm tính toán, hỗ trợ cả kiểu tham chiếu và kiểu giá trị có thể nullable. Nếu hàm trả về `null` (hoặc `null` cho kiểu `struct?`), mục đó sẽ bị xóa.
* **`GetOrDefault<TK, TV>(this ConcurrentDictionary<TK, TV> dict, TK key, TV orDefault)`**: Lấy giá trị hoặc giá trị mặc định.
* **`GetOrDefault<TK, TV>(this ConcurrentDictionary<TK, TV> dict, TK key, Func<TV> orDefault)`**: Lấy giá trị hoặc tính toán giá trị mặc định.
* **`ReturnNullException`**: Ngoại lệ tùy chỉnh được ném ra khi `Compute` trả về `null`.

### `DictionaryExtensions`

Cung cấp một bộ sưu tập phong phú các phương thức mở rộng cho `IDictionary` và `Dictionary`, đơn giản hóa các thao tác từ điển phổ biến:

* **`GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)`**: Lấy hoặc thêm giá trị.
* **`GetOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, Func<TValue> value)`**: Lấy hoặc thêm giá trị được tạo bằng hàm.
* **`AddAll<TKey, TValue, TDict>(this TDict dict, Dictionary<TKey, TValue> dictToAdd)`**: Thêm tất cả các mục từ từ điển khác.
* **`Put<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)`**: Đặt hoặc cập nhật.
* **`PutIfNotNull<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)`**: Đặt chỉ khi giá trị không null.
* **`PutIfAbsent<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)`**: Đặt chỉ khi khóa không tồn tại.
* **`PutIfAbsentAndNotNull<TK, TV>(this Dictionary<TK, TV> dict, TK key, TV value)`**: Đặt chỉ khi khóa không tồn tại và giá trị không null.

### `GeneralExtensions`

Một lớp tiện ích đơn giản cho việc đúc kiểu an toàn:

* **`As<T>(this object obj)`**: Cố gắng đúc đối tượng thành kiểu `T`. Nếu không thành công, ném `InvalidCastException`.

### `JObjDictionaryExtensions`

Mở rộng `IDictionary<string, JToken>` để dễ dàng trích xuất các kiểu dữ liệu cơ bản từ `JObject`:

* **`TryGetString(this IDictionary<string, JToken> dictionary, string key, out string value)`**: Cố gắng lấy giá trị chuỗi.
* **`TryGetBool(this IDictionary<string, JToken> dictionary, string key, out bool value)`**: Cố gắng lấy giá trị boolean.
* **`TryGetDouble(this IDictionary<string, JToken> dictionary, string key, out double value)`**: Cố gắng lấy giá trị double.

### `JsonExtensions`

Cung cấp các phương thức mở rộng tiện lợi để serialize và deserialize đối tượng sang/từ chuỗi JSON sử dụng Newtonsoft.Json, với hai bộ cài đặt linh hoạt.

* **`JsonToObj<T>(this string jsonStr)`** / **`JsonToObj(this string jsonStr, Type type)`**: Deserialize với cài đặt "Normal".
* **`JsonToObjStrict<T>(this string jsonStr)`** / **`JsonToObjStrict(this string jsonStr, Type type)`**: Deserialize với cài đặt "Strict" (`TypeNameHandling.Auto`).
* **`ToJson(this object obj)`**: Serialize với cài đặt "Normal".
* **`ToJsonStrict(this object obj)`**: Serialize với cài đặt "Strict".

### `MonoBehaviourExtensions`

Dành riêng cho các dự án Unity, các tiện ích này giúp quản lý `MonoBehaviour` components dễ dàng hơn:

* **`GetOrAddComponent(this GameObject gameObject, Type type)`**: Lấy hoặc thêm một component vào `GameObject`.
* **`GetOrAddComponent(this MonoBehaviour monoBehaviour, Type type)`**: Tương tự, lấy hoặc thêm component vào `GameObject` của `MonoBehaviour` hiện tại.
* **`GetOrAddComponent<T>(this GameObject gameObject)`** / **`GetOrAddComponent<T>(this MonoBehaviour monoBehaviour)`**: Các phiên bản generic để làm việc với các kiểu cụ thể.

---

## 📝 Logging System (Hệ thống Log)

Bạn đã xây dựng một hệ thống ghi log linh hoạt và có thể mở rộng, sử dụng conditional compilation để kiểm soát đầu ra log trong các bản dựng.

### `UtilSingleton<T>` (Abstract Base Class)

Cung cấp nền tảng cho việc triển khai **singleton** sử dụng `Lazy<T>`. Đây là lớp cơ sở mà các Logger của bạn sẽ kế thừa để đảm bảo chỉ có một instance.

### `MyLogger<T>` (Abstract Base Class)

Một lớp trừu tượng để tạo các logger tùy chỉnh.

* Kế thừa từ `UtilSingleton<T>`, đảm bảo mỗi logger cụ thể là một singleton.
* Cung cấp các phương thức `Info`, `Warning`, và `Error`.
* Hỗ trợ **mã màu** tùy chỉnh cho thông báo log thông qua phương thức trừu tượng `GetColor()`.
* Tự động xử lý các trường hợp `AggregateException` và `TargetInvocationException` bằng cách log các `InnerException` của chúng.
* Tất cả các phương thức log đều được đánh dấu bằng `[Conditional("FALCON_LOG_DEBUG")]`, có nghĩa là chúng chỉ được biên dịch và chạy khi biểu tượng **`FALCON_LOG_DEBUG`** được định nghĩa trong cài đặt dự án. Điều này giúp loại bỏ hoàn toàn mã log trong các bản dựng sản phẩm, tối ưu hóa hiệu suất.
---