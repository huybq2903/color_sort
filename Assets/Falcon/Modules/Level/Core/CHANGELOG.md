## 1.3.11
- FLevelManager chuyển hẳn về cửa chuẩn FalconBigDataController.Level (OnStart/OnHeartbeat/OnPass/OnFail) thay vì tự dựng FLevelLog + Send: cùng event, wire không đổi byte nào, nhưng invariant "SetLevelConfig trước enqueue" giờ nằm trong cửa OnStart thay vì phải tự nhớ, và Level module hết ôm chi tiết pipeline của BigData
- FIX chia cho 0 ở SendLevelLog nhánh Fail: levelProgress = score*100/totalScore — game không có hệ điểm (totalScore = 0) là DivideByZeroException ngay lúc thua màn, log Fail mất và exception ném vào call site của game. Cùng họ bug đã vá ở ALevelHeartBeatService 1.1.4; giờ totalScore <= 0 thì để levelProgress mặc định
- Dep: bigdata 1.3.2 (cửa OnHeartbeat mới), devkit 1.3.0, remoteconfig 1.1.5
----------------------------------


## 1.3.10
- Nối lại dây đứt: OnLevelStart(param) bị refactor pipeline (4938a6754) làm rơi — chữ ký còn nhận FParam nhưng thân hàm không dùng, game truyền vào bị nuốt im lặng từ đó. Giờ param.ToDictionary chảy vào extraMeta của log Start đúng nghĩa cũ (đời PuzzleLevelLog), cùng khuôn OnLevelResult; extraMeta tường minh thắng khi trùng key
- OnLevelStart/OnLevelResult thêm optional Dictionary<string,object> extraMeta — đi CẢ hai đường: extraMeta của log BigData (server lưu event_extra_props, đăng ký hợp đồng sau cook hồi tố được) và field extraMeta mới trên CSLevelStart/CSLevelResult cho game server (bản THÔ, không trộn với coinSpend/info vốn đã có field riêng). Trên log BigData, key game truyền tường minh THẮNG key suy từ info/param cũ khi trùng; cột hợp đồng thì không nguồn nào đè nổi (merge PutIfAbsent ở tầng param)
- playTurnId chuyển về BigData 1.3.0 quản lý tập trung (LevelTurnService/LevelTurnState): FLevelManager không tự sinh/reset nữa, PlayTurnId proxy đọc turn đang mở (null khi không có turn — trước đây lazy sinh mới); level log không set playTurnId, cache tự điền
- OnLevelStart thêm optional movesLimit/timeLimitSec; OnLevelResult thêm optional failReason (hợp đồng §D)
- movesLimit/timeLimitSec nay đi trong bundle nhãn màn thay vì field phẳng (amendment hợp đồng §H6 — attr của MÀN không thả lên bản tin lượt): OnLevelStart giữ NGUYÊN chữ ký, game không phải sửa dòng nào; FLevelManager gọi thêm FalconBigDataController.Level.SetLevelConfig trước khi Send
- Bỏ elo = -1 trong log Pass/Fail (gây LogError + ép về 0 mỗi lần) — elo để null, vắng mặt khỏi payload
----------------------------------


## 1.3.9
- Fix: reset IAP cộng dồn (_price/_currencyCode) khi OnLevelStart — trước đây CSLevelResult mang theo IAP của các level trước trong phiên
- LevelHeartBeat nhận LevelHeartBeatParamV2 (BigData 1.2.9 đổi tên từ LevelHeartBearParamV2 do typo)
----------------------------------


## 1.3.8
- Patch GUID using
----------------------------------


## 1.3.7
- Add LevelHeartBeat to FLevelManager
- Adjust FLevelManager to use new LevelParamV2
----------------------------------


## 1.3.6
- Đồng bộ levelData giữa các sự kiện OnLevelReady, OnLevelStart, OnLevelResult
----------------------------------


## 1.3.5
- update lại phần tính levelProgress khi gửi level log
----------------------------------


## 1.3.4
- Thêm tham số levelProgress để gửi sang Bigdata trong hàm OnLevelResult
----------------------------------


## 1.3.3
Thêm thông tin FilterID, AbVariant cho AB testing Level
----------------------------------


## 1.3.2
- Gửi md5 của level lên bigdata
----------------------------------


## 1.3.1
- Thêm hàm SendLevelPlayingInfo để gửi các sự kiện trong quá trình chơi level lên Server
----------------------------------


## 1.3.0
- thêm string info vào bản tin SCLevelResult
----------------------------------


## 1.2.9
- Thiết lập param cho CSLevelResult trùng với CSLevelStart
----------------------------------


## 1.2.8
- Thêm param vào PuzzleLevelLog
----------------------------------


## 1.2.7
- fix lỗi nullpointer
----------------------------------


## 1.2.6
- Support AB Testing bằng Server Logic
----------------------------------


## 1.2.5
- Sửa lỗi gửi các bản tin CSLevelXXX trước khi Login
----------------------------------


## 1.2.4
- Đổi FLevelLog thành PuzzleLevelLog
----------------------------------


## 1.2.3
- try cache lỗi từ ILevelProvider
----------------------------------


## 1.2.2
- Chuẩn hoá ký tự xuống dòng trước khi mấy MD5
----------------------------------


## 1.2.1
- Thêm tham số vào CSLevelResult
----------------------------------


## 1.2.0
- Tối ưu performance
----------------------------------


## 1.1.9
- Thêm class LevelCompressor để nén level text
----------------------------------


## 1.1.8
- Bỏ GetLevelDifficulty(int level) trong IFLevelProvider
----------------------------------


## 1.1.7
- Bỏ hàm GetLevelParam(int level), thay bằng hàm GetLevelParam(string levelData) trong IFLevelProvider
----------------------------------


## 1.1.6
- fix log level_start
----------------------------------


## 1.1.5
- Thêm log action khi vào chơi level
----------------------------------


## 1.1.4
- fix lỗi save difficulty
----------------------------------


## 1.1.3
- Thêm startCurrentLevel = true || false vào LevelData để kiểm tra level đã start hay chưa
----------------------------------


## 1.1.2
- Xoá level mà server trả về trước đó trên Client, nếu truyền về giá trị nội dung rỗng
----------------------------------


## 1.1.1
- Send LevelLog
----------------------------------


## 1.1.0
- Truyền thêm các tham số lên Server cho onLevelResult
----------------------------------


## 1.0.22
- thêm total score vào level result
----------------------------------


## 1.0.21
- đăng ký sự kiện lắng nghe để trả về level hiện tại cho các module khác
----------------------------------


## 1.0.20
- Thêm levelParam vào CSLevelStart để gửi lên Server
----------------------------------


## 1.0.19
Thêm log level
----------------------------------


## 1.0.18
log funnel
----------------------------------


## 1.0.17
Thêm hàm public int[] GetAllLevels() trả về mảng các level đã được tải từ Server về, theo thứ thự tăng dần, Ví dụ: [1,3,6,7,8,12,15,17]
----------------------------------


## 1.0.16
Thêm Force Play Level, để test bất kỳ level nào trên CMS
----------------------------------