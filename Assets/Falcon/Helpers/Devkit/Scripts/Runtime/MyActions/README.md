# MyAction Plugin for Unity

---

**Author**: leehuyyhoangg
**Email**: hoanglh@falcongames.com
**Company**: Falcon Games
**Date**: 2025-06-12

---

## 🚀 Overview

**MyAction** là một plugin Unity mạnh mẽ, được thiết kế để đơn giản hóa việc quản lý và thực thi các **tác vụ bất đồng bộ (asynchronous tasks)** trong quá trình phát triển game. Nó cung cấp một kiến trúc linh hoạt để định nghĩa, lên lịch, và theo dõi trạng thái của các hành động, đồng thời hỗ trợ việc xử lý tác vụ trên các luồng khác nhau của ứng dụng Unity (main thread hoặc các background thread).

---

## 🎯 Core Concepts

Plugin xoay quanh hai khái niệm chính:

### 1. `IMyAction` (Các Tác vụ)

`IMyAction` là interface cơ sở cho tất cả các tác vụ trong hệ thống. Nó định nghĩa hợp đồng cho một hành động, bao gồm các thuộc tính và phương thức để quản lý vòng đời của nó:

* **`ThreadPool`**: Tham chiếu đến `IThreadPool` mà tác vụ sẽ được lên lịch và thực thi.
* **`Exception`**: Bất kỳ lỗi nào phát sinh trong quá trình thực thi tác vụ.
* **`State`**: Trạng thái hiện tại của tác vụ (ví dụ: `NotStarted`, `Pending`, `Running`, `Success`, `Failure`, `Canceled`).
* **`Schedule()`**: Yêu cầu `ThreadPool` đưa tác vụ vào hàng đợi để thực thi.
* **`Cancel()`**: Cố gắng hủy bỏ tác vụ nếu có thể.
* **`Invoke()`**: Phương thức chứa logic thực thi chính của tác vụ. Đây là nơi công việc thực sự được thực hiện.
* **`CanInvoke()`**: Kiểm tra xem tác vụ có sẵn sàng hoặc được phép để thực thi hay không.

### 2. `IThreadPool` (Nơi xử lý tác vụ)

`IThreadPool` là interface chịu trách nhiệm quản lý hàng đợi và phân phối các `IMyAction` để thực thi. Plugin cung cấp hai triển khai chính để đáp ứng các yêu cầu về luồng khác nhau:

* **`GlobalThreadPool`**: Được sử dụng cho các tác vụ không cần chạy trên **main thread** của Unity. Lý tưởng cho các công việc tính toán nặng, xử lý dữ liệu, hoặc các hoạt động I/O không chặn có thể chạy song song mà không ảnh hưởng đến hiệu suất UI/game.
* **`MainThreadPool`**: Được sử dụng cho các tác vụ **cần phải chạy trên main thread** của Unity. Điều này bao gồm các hoạt động tương tác với Unity API (ví dụ: tạo GameObject, cập nhật UI, truy cập các thành phần MonoBehavior), đảm bảo an toàn về luồng.

---

## 🏗️ Cấu trúc và Phân loại Tác vụ (Action Hierarchy)

Plugin định nghĩa một hệ thống phân cấp các interface và lớp trừu tượng để tạo ra các loại tác vụ khác nhau, hỗ trợ việc xây dựng các chuỗi hành động phức tạp và các luồng công việc:

* **`MyAction` (Abstract Class)**: Triển khai cơ sở của `IMyAction`. Nó cung cấp các phương thức `Schedule()` và `Cancel()` mặc định, ủy quyền việc thêm/xóa tác vụ cho `ThreadPool`. Các lớp con cần triển khai các logic cụ thể cho `Invoke()` và `CanInvoke()`.

* **`IContinuableAction`**: Interface đánh dấu một tác vụ có thể được "tiếp tục" hoặc là cơ sở cho các tác vụ khác trong một chuỗi.

* **`IFollowAction`**: Interface đánh dấu một tác vụ được thiết kế để "theo sau" một tác vụ khác.

* **`IStartAction`**: Interface đại diện cho một tác vụ khởi đầu một chuỗi hành động.
    * **`StartAction` (Abstract Class)**: Lớp trừu tượng triển khai `IStartAction`. Đặc điểm nổi bật là phương thức `CanInvoke()` của nó luôn trả về `true`, ngụ ý rằng tác vụ khởi đầu luôn sẵn sàng để được gọi.

* **`IChainAction`**: Interface đại diện cho một tác vụ đóng vai trò là một "mắt xích" trong một chuỗi, vừa có thể được tiếp tục vừa có thể theo sau một tác vụ khác.
    * **`ChainAction` (Abstract Class)**: Lớp trừu tượng triển khai `IChainAction`. Nó được xây dựng để bao bọc một `IContinuableAction` (`_baseAction`), và ủy quyền hầu hết các thuộc tính (`Exception`, `State`, `ThreadPool`) và phương thức (`Invoke()`, `CanInvoke()`, `Cancel()`) cho tác vụ cơ sở đó. Điều này cho phép tạo ra các hành vi bổ sung (như trì hoãn, chờ điều kiện) mà không làm thay đổi logic cốt lõi của tác vụ cơ sở.

* **`IEndAction`**: Interface đại diện cho một tác vụ kết thúc một chuỗi hành động.
    * **`EndAction` (Abstract Class)**: Lớp trừu tượng triển khai `IEndAction`. Giống như `ChainAction`, nó cũng bao bọc một `IContinuableAction` và ủy quyền các thuộc tính/phương thức chính cho tác vụ cơ sở. `EndAction` thường được sử dụng để thêm logic cuối cùng, xử lý kết quả, hoặc điều khiển việc kết thúc/lặp lại một chuỗi.

---

## 🔗 MyAction Extensions

Các phương thức mở rộng tiện ích được cung cấp để tương tác thuận tiện với `IMyAction` trong các kịch bản bất đồng bộ:

* **`WaitTillDone(this IMyAction action, CancellationToken cancellationToken = default)`**: Một phương thức `async` cho phép bạn đợi cho đến khi một `IMyAction` hoàn thành quá trình thực thi của nó (dù thành công, thất bại hay bị hủy). Hỗ trợ `CancellationToken` để hủy bỏ việc chờ.
* **`WaitTillSuccess(this IMyAction action, CancellationToken cancellationToken = default)`**: Tương tự `WaitTillDone`, nhưng sẽ ném `action.Exception` nếu tác vụ hoàn thành với trạng thái không phải là `Success`.

---

## 🧩 Các Triển khai MyAction Cơ bản

Dưới đây là một số ví dụ về các triển khai `MyAction` cụ thể, minh họa cách các lớp trừu tượng được sử dụng để tạo ra các hành vi hữu ích:

### `UnitAction`

* **Loại**: `StartAction` (tác vụ khởi đầu)
* **Mục đích**: Là tác vụ cơ bản nhất, trực tiếp thực thi một `System.Action` được cung cấp.
* **Đặc điểm**:
    * Có thể được khởi tạo với một `Action` và một `IThreadPool` cụ thể, hoặc mặc định sử dụng `GlobalThreadPool.Instance` nếu chỉ cung cấp `Action`. Điều này giúp kiểm soát linh hoạt việc thực thi trên main thread hay background thread.
    * Quản lý trạng thái thực thi (`ExecState`) và bắt/lưu trữ bất kỳ `Exception` nào xảy ra trong quá trình `Invoke()`.

### `DelayAction`

* **Loại**: `ChainAction`
* **Mục đích**: Chèn một khoảng thời gian trì hoãn trước khi tác vụ cơ sở của nó được phép thực thi.
* **Đặc điểm**: Phương thức `CanInvoke()` của nó kiểm tra xem một khoảng thời gian (`_delayTime`) đã trôi qua kể từ khi `DelayAction` được tạo hay chưa, kết hợp với điều kiện `CanInvoke()` của tác vụ cơ sở.

### `WaitInit`

* **Loại**: `ChainAction`
* **Mục đích**: Đảm bảo rằng một tác vụ chỉ được thực thi sau khi tất cả các dịch vụ khởi tạo (`InitService.AllInitState`) đã hoàn thành thành công.
* **Đặc điểm**: Phương thức `CanInvoke()` của nó phụ thuộc vào trạng thái của `InitService.AllInitState` và `CanInvoke()` của tác vụ cơ sở, hữu ích cho các tác vụ cần môi trường đã được khởi tạo đầy đủ.

### `RepeatAction`

* **Loại**: `EndAction`, triển khai `IDisposable`
* **Mục đích**: Thực thi lặp đi lặp lại tác vụ cơ sở của nó sau mỗi khoảng thời gian nhất định (`TimeSpan`).
* **Đặc điểm**:
    * Sau khi `Invoke()` tác vụ cơ sở, nó tự động lên lịch lại chính mình (`Schedule()`) để thực hiện lần tiếp theo sau `TimeSpan` đã định.
    * Có thể hủy bỏ lặp lại bằng cách gọi `Cancel()` hoặc thông qua `Dispose()`.
    * Có thể có một độ trễ ban đầu trước lần thực thi đầu tiên.

### `ScheduleAction`

* **Loại**: `EndAction`, triển khai `IDisposable`
* **Mục đích**: Tương tự `RepeatAction`, nó cũng thực hiện một tác vụ lặp lại theo lịch trình.
* **Đặc điểm**:
    * Khác với `RepeatAction`, `ScheduleAction` sẽ *lên lịch* cho lần thực thi tiếp theo **trước khi** gọi `Invoke()` của tác vụ cơ sở trong lần hiện tại.
    * Có thể hủy bỏ lịch trình bằng cách gọi `Cancel()` hoặc thông qua `Dispose()`.
    * Có thể có một độ trễ ban đầu trước lần thực thi đầu tiên.

---

## 🛠️ Cài đặt

(Hãy thêm hướng dẫn cài đặt cụ thể tại đây. Ví dụ: "Thả thư mục `Falcon.Helpers.Devkit.MyActions` vào thư mục `Assets` của dự án Unity của bạn.")

---

## 🚀 Cách sử dụng (Ví dụ)

(Thêm các ví dụ code tại đây để minh họa cách tạo và sử dụng các `MyAction` khác nhau, ví dụ: chuỗi `StartAction -> ChainAction -> EndAction`, hoặc sử dụng `DelayAction`, `RepeatAction`.)

```csharp
// Ví dụ cơ bản: Thực thi một hành động trên GlobalThreadPool
var unitAction = new UnitAction(() =>
{
    Debug.Log("This action runs in a background thread.");
});
unitAction.Schedule();

// Ví dụ: Thực thi một hành động trên MainThreadPool sau một độ trễ
// Giả sử MainThreadPool.Instance đã được khởi tạo đúng cách
var delayedMainThreadAction = new DelayAction(
    new UnitAction(() =>
    {
        Debug.Log("This action runs on the main thread after a delay.");
        // Ví dụ: Cập nhật UI
    }, MainThreadPool.Instance),
    TimeSpan.FromSeconds(2)
);
delayedMainThreadAction.Schedule();

// Ví dụ: Một hành động lặp lại mỗi 5 giây
// Sử dụng using để đảm bảo Dispose được gọi khi không còn dùng nữa
using (var repeatedAction = new RepeatAction(() =>
{
    Debug.Log("This action repeats every 5 seconds!");
}, TimeSpan.FromSeconds(5)))
{
    repeatedAction.Schedule();
    // Logic khác của game...
    // repeatedAction.Cancel(); // Để dừng lặp lại
}

// Ví dụ: Chờ khởi tạo xong rồi mới chạy tác vụ
var waitAndRunAction = new WaitInit(() =>
{
    Debug.Log("All services initialized. Now running this task.");
});
waitAndRunAction.Schedule();

// Ví dụ về việc chờ một tác vụ hoàn thành
async void RunAndWaitExample()
{
    var longRunningAction = new UnitAction(() =>
    {
        Debug.Log("Starting long running task...");
        System.Threading.Thread.Sleep(3000); // Simulate work
        Debug.Log("Long running task finished.");
    });

    longRunningAction.Schedule();
    await longRunningAction.WaitTillDone();
    Debug.Log("Long running task is done, continuing after await.");
}

RunAndWaitExample();