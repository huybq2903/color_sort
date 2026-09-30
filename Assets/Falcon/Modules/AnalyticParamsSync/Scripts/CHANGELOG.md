## 1.1.6
- ALevelHeartBeatService: CurrentLevelTotalScore/CurrentLevelBoosterUsed/CurrentLevelExtraParams đổi từ virtual-mặc-định về ABSTRACT — đảo một phần quyết định 1.1.4. Lý do: ba giá trị này chỉ GAME biết, SDK không cache được, nên mặc định 0/null là ĐIỀN HỘ chứ không phải suy ra — quên override là levelProgress/boosters mất im lặng và không có heuristic nào bắt được (null trông y như "không có thật"). Giờ "không có" phải là câu trả lời game tự gõ (=> 0 / => null), quên thì gãy compile chứ không sai im lặng. IsPlayingLevel giữ virtual vì SDK TỰ SUY được (playTurnId != null) — ranh giới bắt buộc/mặc định giờ đi theo đúng trục ai-biết. ⚠ BREAKING với subclass hiện có: thêm 3 dòng override là xong, compiler chỉ tận nơi
----------------------------------


## 1.1.5
- Patch statistic meta logic when conencting to server takes too many time
----------------------------------


## 1.1.4
- FIX chia cho 0 trong ALevelHeartBeatService: levelProgress = score*100/CurrentLevelTotalScore, game nào chưa có hệ điểm (hoặc màn chưa nạp xong) trả 0 là ném DivideByZeroException — mà hàm này chạy trong vòng lặp gom log lúc app pause nên nó giết luôn log của các generator còn lại, không riêng bản tin heartbeat. Giờ totalScore <= 0 thì bỏ qua levelProgress; phép chia cũng nâng lên long để score lớn không tràn khi nhân 100
- ALevelHeartBeatService giảm từ 7 xuống 3 hàm bắt buộc override: IsPlayingLevel mặc định đọc "có lượt nào đang mở không" từ chính SDK (playTurnId != null), còn CurrentLevelTotalScore/BoosterUsed/ExtraParams thành virtual với mặc định "không có". Ba cái còn bắt buộc (playTime/score/numberMove) là thứ chỉ game biết và cũng chính là nội dung của bản tin heartbeat — để mặc định thì game override 0 hết sẽ gửi bản tin rỗng, tệ hơn là không đăng ký service
- Không gửi số giả: playTime/score/movesUsed chỉ điền khi > 0, không thì để field vắng mặt (§H4) thay vì gửi 0 — 0 trông như giá trị hợp lệ nên không ai đi kiểm
- Dùng LevelHeartBeatParamV2 (tên mới sau khi sửa typo bên BigData) trong ALevelHeartBeatService
- ALevelHeartBeatService: bỏ elo = -1 (mỗi heartbeat bắn 1 Debug.LogError rồi ép elo về 0); elo để null nên bị loại hẳn khỏi payload theo FKey(RemoveIfNull)
----------------------------------


## 1.1.3
- add ALevelHeartBeatService
----------------------------------


## 1.1.2
-Giảm thời gian đợi server xuống còn 15s
----------------------------------


## 1.1.1
- Ngừng lưu non-test configs lên server
----------------------------------


## 1.1.0
- Make ServerPlayerAbTestRepository to reset _configs on Save new config (on update from net)
----------------------------------


## 1.0.9
- Điều chỉnh lại để không ném exception từ WaitInit nữa
----------------------------------


## 1.0.8
- Sửa lại theo cấu trúc của Devkit
----------------------------------


## 1.0.7
- Disable ReservePlayerAdRepository
- Disable ReservePlayerIapRepository
----------------------------------


## 1.0.6
- Chỉnh lỗi race condition khiên config không được cập nhật trên 1 số thiết bị
----------------------------------


## 1.0.5
- Thêm MmpInfoLog
----------------------------------


## 1.0.4
- Thêm header creator cho một số file còn thiếu
----------------------------------


## 1.0.3
- Sửa lại cấu trúc phụ thuộc của ServerTimeRepository do ghi đè nhầm trong phiên bản trước
----------------------------------


## 1.0.2
- Sửa bug không cập nhật RetentionChanged trong ServerPlayerSessionRepository
----------------------------------


## 1.0.1
- Khởi tạo module
----------------------------------