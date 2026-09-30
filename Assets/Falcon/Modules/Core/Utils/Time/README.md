# Module @Time

## Tổng quan
- **Tên:** `Time`
- **Giới thiệu:** Cung cấp nguồn thời gian đáng tin cậy và chống gian lận, được thiết kế để lấy thời gian từ server.
- **Tính năng chính:**
    - Cung cấp thời gian UTC và Local đáng tin cậy (`TimeUtils.UTCNow`, `TimeUtils.Now`).
    - Chuyển đổi giữa `DateTime` và Unix timestamp.
    - Các hàm tiện ích để so sánh ngày (`IsSameDay`, `IsSameDayAsNow`).
    - Bao gồm `AutoCountdownTimer` để dễ dàng tạo bộ đếm ngược.
    - Cơ chế chống gian lận qua nhiều lớp (server, Device Time).

## Quick Start

### Lấy thời gian đáng tin cậy
```csharp
// Lấy thời gian UTC đáng tin cậy
DateTime currentUtcTime = TimeUtils.UTCNow;

// Lấy thời gian địa phương đáng tin cậy
DateTime currentLocalTime = TimeUtils.Now;

Debug.Log($"Thời gian UTC hiện tại: {currentUtcTime}");
```

### Sử dụng Bộ đếm ngược tự động
Ví dụ với một khoảng thời gian
```csharp
// Đếm ngược trong 60 giây
// 'this' là một MonoBehaviour để coroutine có thể chạy
AutoCountdownTimer myCountdownTimer = new();

myCountdownTimer.StartCountDownFromPeriodTime(this, 60, false, secondsLeft => {
    countdownText.text = $"Còn lại: {secondsLeft} giây";
});
```


Lưu ý trong `AutoCountdownTimer` có các defines (*_minMinutes, _minHours, _minDays*) về việc trả về callback time remaining mỗi cycle.

Nếu bạn muốn chắc rằng callback trả về mỗi 1 giây, hãy pass các param số lớn.

```csharp
 AutoCountdownTimer myCountdownTimer = new(9999, 9999, 9999);
```


## Danh sách đầy đủ API

### `TimeUtils` (static class)
- `DateTime UTCNow`: Trả về `DateTime` UTC đáng tin cậy hiện tại.
- `DateTime Now`: Trả về `DateTime` Local đáng tin cậy hiện tại.
- `long GetCurrentTimestampInSecondsUTC()`: Lấy timestamp UTC hiện tại (tính bằng giây).
- `long GetCurrentTimestampInSecondsLocal()`: Lấy timestamp Local hiện tại (tính bằng giây).
- `long GetTimestampInSecondsOf(DateTime d)`: Chuyển đổi một `DateTime` đã cho thành timestamp (tính bằng giây).
- `DateTime GetDateTimeFromTimestampUTC(long timestampUTC)`: Chuyển đổi timestamp UTC (giây) thành `DateTime`.
- `bool IsSameDay(long timestampA, long timestampB)`: Kiểm tra xem hai timestamp có cùng một ngày không.
- `bool IsSameDayAsNow(long timestamp, bool isLocalTime)`: Kiểm tra xem một timestamp có cùng ngày với hiện tại không.

### `AutoCountdownTimer` (class)
- `StartCountDownFromFutureTimestampUTC(...)`: Bắt đầu đếm ngược từ một thời điểm trong tương lai (timestamp UTC).
- `StartCountDownFromPeriodTime(...)`: Bắt đầu đếm ngược từ một khoảng thời gian (tính bằng giây).
- `Stop()`: Dừng bộ đếm ngược.

## Chi tiết

### Cơ chế lấy thời gian
Module ưu tiên các nguồn thời gian theo thứ tự sau để đảm bảo tính chính xác và chống gian lận:
1.  **Thời gian từ server**: Khi có kết nối mạng, module sẽ đồng bộ UTC Time với server để có được thời gian chính xác nhất.
2.  **Thời gian thiết bị**: Được sử dụng làm phương án dự phòng cuối cùng.

## Lỗi thường gặp

### Độ trễ khi khởi tạo thời gian
Khi ứng dụng vừa khởi động, `TimeUtils.UTCNow` có thể tạm thời trả về thời gian từ thiết bị trong khi chờ đồng bộ với máy chủ. Điều này có thể gây ra một bước nhảy nhỏ về thời gian sau khi đồng bộ thành công. Hãy lưu ý điều này khi thực hiện các phép tính thời gian quan trọng ngay khi ứng dụng bắt đầu.

### Vòng đời của `AutoCountdownTimer`
Lớp `AutoCountdownTimer` cần một `MonoBehaviour` làm ngữ cảnh để chạy coroutine. Nếu `GameObject` chứa `MonoBehaviour` này bị hủy, bộ đếm ngược cũng sẽ dừng lại. Hãy đảm bảo vòng đời của đối tượng ngữ cảnh phù hợp với thời gian bạn muốn bộ đếm ngược hoạt động. 