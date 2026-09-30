## 1.3.0
- MonotonicClock + PlayerSessionService + IDataPool.TrySync
----------------------------------


## 1.2.9
- FIX total_play_time chạy 2× tốc độ thật: PlayerSessionService.OnPostStop và SessionLogService cùng cộng MỘT khoảng foreground vào cùng counter USER_TOTAL_TIME (double-write từ BigData 1.1.7 / 2026-01-08). Giờ PlayerSessionService là writer duy nhất của mode này; KHÔNG reset giá trị tích luỹ (loader clamp bỏ mọi cú tụt — chỉ hạ tốc độ về 1× rồi forward tiếp)
- FIX đồng hồ chơi chạy cả lúc app ở background + TotalPlayTime đếm đôi stretch cuối ở mọi log bắn lúc app pause: thêm mốc đóng băng _pausedAt, TimeSinceLastPause freeze tại lúc pause (thứ tự gọi giữa các ITerminal không còn ảnh hưởng), TotalPlayTime không cộng lại phần đã vào repository. Đúng hợp đồng §H5: giây, foreground-only, counter không reset
- Fix InvalidOperationException trong NoAutoCreateMySingletonImplementShellDisabler: enumerate LINQ lazy trên chính set rồi Remove giữa chừng (thiếu ToList như ISingletonShellSourceDisabler đã làm) — bug tiềm ẩn, chỉ nổ khi project có class gắn [NoAutoCreate]
----------------------------------


## 1.2.8
- Bỏ dependency ngầm vào Odin (Sirenix.Utilities) trong SingletonDecorators, thay bằng GetCustomAttribute chuẩn .NET
- Thêm unit tests cho AtomicRef, MyConcurrentQueue, LazyVal
- Sửa doc: xoá ConcurrentState (không tồn tại) khỏi Core/README; cập nhật tên class thật (IFFile/FLocalFile/FLocalFileRepository) trong BaseSystem/README
----------------------------------


## 1.2.7
- Them tham số về Last Iap vào user params
----------------------------------


## 1.2.6
- Vá lại logic của MainGameObj để tự đảm bảo khi trên scene có nhiều instance
- Vá lại InitService để tự ngắt khi editor stop
----------------------------------


## 1.2.5
- Patch ReservePlayerGeneralRepository._advertisingId
----------------------------------


## 1.2.4
- Sửa RepeatAction
----------------------------------


## 1.2.3
- Thay đổi singleton để có thể thêm các instance sinh qua hàm
- Chuyển ISingletonServiceReady sang SingletonExtension
----------------------------------


## 1.2.2
- Thêm ISingletonsAdviser
----------------------------------


## 1.2.1
- Chỉnh lại RepeatAction và ScheduleAction để đảm bảo concurrency
- Dùng RepeatAction để đồng bộ FDataPool để sync pool vào file mỗi 5p
----------------------------------


## 1.2.0
- Điều chỉnh Devkit để thêm NoLazy và ISingletonServiceReady
----------------------------------


## 1.1.9
- Update IOS Advertisement Support dependency
----------------------------------


## 1.1.8
- Không dùng currentmillis.com để đồng bộ thgian nữa mà chuyển sang dùng api nhà làm trên đầu api bên d4g
----------------------------------


## 1.1.7
- Chuyển DataPool và Time tới thư mục Config
- Đồng bộ thời gian với ITimeRepository
----------------------------------


## 1.1.6
- Restructure Info Repository Info Update
----------------------------------


## 1.1.5
Tách phần khởi tạo giá trị khỏi phần logging do có thể bị ảnh hưởng bởi bật tắt log với scripting symbol
----------------------------------


## 1.1.4
- Thêm 1 số thông tin về CPU và GPU vào IDeviceInfoRepository và FCentralUserParamService 
- Thêm hàm RemoveAll vào MyConcurrentQueue
----------------------------------


## 1.1.3
update InitService to reduce fps
----------------------------------


## 1.1.2
adjust HttpResponse.StreamBody() to close the response with stream, avoid resource leak
----------------------------------


## 1.1.1
fix HttpResponse.StreamBody()
----------------------------------


## 1.1.0
add author
----------------------------------


## 1.0.9
thay đổi FFileRepository và logic của FFile
----------------------------------


## 1.0.8
sửa http Requests uri khi có thông số param
----------------------------------


## 1.0.7
fix http GetRequest uri when have query
----------------------------------


## 1.0.6
thêm file REAME.md
thêm author doc vào 1 số file còn thiếu
----------------------------------