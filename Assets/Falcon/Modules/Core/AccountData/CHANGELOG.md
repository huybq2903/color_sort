## 1.2.0
- fix lỗi mất kết nối
----------------------------------


## 1.1.8
- internal AccountInfo
----------------------------------


## 1.1.7
- public AccountInfo
----------------------------------


## 1.1.6
- Tăng thời gian timeout của network lên 150s
----------------------------------


## 1.1.5
- Xử lý lỗi gửi UpdateGameData trước khi Login
----------------------------------


## 1.1.4
- Đổi CSUpdateGameData kế thừa CSMessageWaitLoginSuccess
----------------------------------


## 1.1.3
- fix bug
----------------------------------


## 1.1.2
- fix lỗi FGameData có __type = null (chưa được gắn)
- Gắn AccountManager.Instance.IsLogin sau khi các sự kiện update data lúc đăng nhập hoàn thành hết
----------------------------------


## 1.1.1
- Bỏ LogUtil.Log khỏi bản build
----------------------------------


## 1.1.0
- Bỏ LogUtil.Log khi build 
- Nâng phiên bản lên 1.1.0 cho số đẹp
- Lưu ý dùng ip tĩnh trong cấu hình Network Config, vì nếu dùng DNS thì thời gian chuyển từ DNS sang ip tĩnh sẽ lâu, gây chậm
----------------------------------


## 1.0.32
- Bỏ log trên Mobile với LogUtil.Log
----------------------------------


## 1.0.31
- commit đè lên bản 1.0.30 do Sang update tính năng lên
----------------------------------


## 1.0.29
- fix lỗi DontDestroy của SendMessageWorker
----------------------------------


## 1.0.28
- SendMessageWorker thành DontDestroy
----------------------------------


## 1.0.27
- SendMessageWorker thành DontDestroy (PersistentSingleton)
----------------------------------


## 1.0.26
- Bỏ khởi tạo Queue trong SendMessageWorker ra ngoài Awake
----------------------------------


## 1.0.25
- Do module Network thêm OnChannelDisconnected  vào ISessionListener
----------------------------------


## 1.0.24
- Thêm method PostConstructor vào FGameData để gọi sau khi load FGameData lên từ Cache
----------------------------------


## 1.0.23
- gửi về null nếu không có biến remote config trên client
----------------------------------


## 1.0.22
- Thêm bản tin để gửi giá trị remote config về cho Server
----------------------------------


## 1.0.21
- Đóng truy cập vào một số class CS, SC bằng internal
- gọi OnData cho bản tin SC trả về trong AddSCListener
----------------------------------


## 1.0.20
fix lỗi chưa set sequence khi lấy dữ liệu từ Server về
----------------------------------


## 1.0.19
- đổi type thành __type trong FGameData
- fix lỗi không cập nhật sequence khi lấy dữ liệu từ Server về
----------------------------------


## 1.0.18
đổi type thành __type trong FGameData cho đỡ nhầm lẫn
----------------------------------


## 1.0.17
đẩy cả sequence lên Server khi Client update dữ liệu game
----------------------------------


## 1.0.16
tăng sequence của client mỗi lần save hoặc update lên server, thay hàm SaveAndUpdateToServer bằng UpdateToServer
----------------------------------


## 1.0.15
Thêm hàm SaveAndUpdateToServer
----------------------------------


## 1.0.11
Cập nhật tài liệu
----------------------------------