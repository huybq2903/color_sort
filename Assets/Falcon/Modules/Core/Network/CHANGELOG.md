## 2.0.6
Fix bug: sử dụng 1 instance StringBuilder chung trong Parser (cả 2 implementation DefaultJsonParser và JsonDotNetParser)
----------------------------------


## 2.0.5
- bỏ SendHandShake gây lỗi của UDPChannel
----------------------------------


## 2.0.4
- fix lỗi OnSessionReset
----------------------------------


## 2.0.3
- fix lỗi UDP khi kết nối lại
----------------------------------


## 2.0.2
- fix lỗi gửi binary
----------------------------------


## 2.0.1
- socket.Socket.On<ConnectResponse>("connect", OnConnected);
----------------------------------


## 2.0.0
- Update thư viện BestHTTP mới nhất 3.x
----------------------------------


## 1.3.1
- Fix lỗi mất kết nối
----------------------------------


## 1.3.0
- fix bug
----------------------------------


## 1.2.9
- Thêm lắng nghe sự kiện SC từ Server trả về
----------------------------------


## 1.2.8
- fix bug
----------------------------------


## 1.2.7
- Tăng timeout của session lên 120s
----------------------------------


## 1.2.6
- fix lỗi gửi SCCloseSession sẽ ngắt kết nối và không tự kết nối lại lên Server nữa
----------------------------------


## 1.2.5
- Nâng thời gian timeout của network lên 150s
----------------------------------


## 1.2.4
- Tăng thời gian timeout của Socket lên 60s (trước đây là mặc định 20s)
----------------------------------


## 1.2.3
- fix lỗi sequence với UDP
----------------------------------


## 1.2.2
- Chủ động thiết lập kết nối UDP từ client khi khởi tạo network
----------------------------------


## 1.2.1
- Bỏ thư mục test
----------------------------------


## 1.2.0
- Hỗ trợ mã hoá bản tin CS, SC bằng Binary, nhẹ và nhanh hơn nhiều lần Json
- Xem ví dụ với bản tin FPing, FPong
----------------------------------


## 1.1.4
- Thêm timout 20s để kiểm tra kết nối mạng
- Thêm biến bool FNetManager.Instance.Connected để kiểm tra trạng thái kết nối
----------------------------------


## 1.1.3
- fix lỗi UDP
----------------------------------


## 1.1.2
- Thêm SendAsyn(), SendAsyncAfter(delay), SendAfter(delay) vào CSMessage
----------------------------------


## 1.1.1
- Tăng tốc độ encode khi gửi dữ liệu dùng Formating.None
----------------------------------


## 1.1.0
- Bỏ LogUtil.Log khỏi bản build
----------------------------------


## 1.0.11
- Cập nhật tài liệu
----------------------------------


## 1.0.10
- Thêm AddSCListenerExt, hỗ trợ truyền vào bản tin CS gốc khi nhận SC trả về
Cách dùng:
new CSChat("Hello").AddSCListenerExt<SCChat>(((csMessage, scMessage, timeout, success) =>
 {
    if (timeout)
       //To-do timeout 
    else
       //To-do success 
}), 5).Send();
----------------------------------


## 1.0.9
- Thêm sự kiện OnChannelDisconnected vào ISessionListener
----------------------------------


## 1.0.8
- fix lỗi dns khi client không kết nối mạng
----------------------------------


## 1.0.7
cập nhật tài liệu
----------------------------------