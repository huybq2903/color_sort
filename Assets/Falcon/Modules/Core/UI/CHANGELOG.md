## 1.2.11
- Fix lỗi Broadcast khi mở popup
- Thêm OnUIPopupAwake (chỉ gọi 1 lần)
* Vòng đời popup:
OnUIPopupAwake -> OnUIPopupEnable -> OnUIPopupStart
----------------------------------


## 1.2.10
- Fix Change Name
----------------------------------


## 1.2.9
- Bỏ force disable khi tạo popup
----------------------------------


## 1.2.8
- Sau khi thử phương án thay thế lifecycle của unity. Nó đang có một số luồng liên quan
đến cách hoạt động game object của unity. Nên sẽ bỏ các function lifecyle của
version 1.2.7.

- Thay vào đó sẽ có 2 function:
OnUIPopupStart: gọi 1 lần duy nhất khi mở popup lên
OnUIPopupEnable: gọi mỗi lần mở popup lên

Thứ tự gọi như sau:
OnUIPopupEnable -> OnUIPopupStart (1 lần) -> OnUIPopupEnable -> ...
----------------------------------


## 1.2.7
- Bổ sung StartUIPopup, OnEnableUIPopup, OnDisableUIPopup.

Khuyên dùng để thay thế lifecyle mặc định. Cơ chế gọi tương tự 
Start, OnEnable, OnDisable.

- Vì UIManager xử lý Canvas theomột trình tự nhất định, đảm bảo hoàn thành 
mới cho phép chạy các lifecycle.
----------------------------------


## 1.2.6
- Sửa lỗi mở popup không nằm trong canvas. Dẫn đến tính toán sử dụng Rect Transform
các game object con trong popup gọi ở awake, start, onenable không chính xác.
- Tối ưu hiệu năng
----------------------------------


## 1.2.5
- Add override duration
----------------------------------


## 1.2.4
- Change Default Execute UIManager to First
----------------------------------


## 1.2.3
- Add HasInstance to Wrapper
----------------------------------


## 1.2.2
Update Singleton
----------------------------------


## 1.2.1
- Update Code
----------------------------------


## 1.2.0
- Thêm UIAnimationLightPopup
----------------------------------


## 1.1.3
Thêm action

- onShow: sau khi popup được bật lên. 
Được gọi sau OnEnable và sau kết thúc 1 frame.
- onShowLayoutCompleted: gọi sau onShow
 và sau kết thúc 1 frame.
----------------------------------


## 1.1.2
Fix Open Popup Delay
----------------------------------


## 1.1.1
- Cập nhật tạo Popup Scene
----------------------------------


## 1.1.0
- Bỏ onInit và onShow
----------------------------------


## 1.0.9
Thêm 2 function:

- IsPopupStackActiveInHierarchy. 
Kiểm tra 1 popup có đang được mở hay không

- CloseAllPopup. Đóng tất cả popup
----------------------------------


## 1.0.8
Fix Spam Load Scene
----------------------------------


## 1.0.7
Fix Spam Open Popup
----------------------------------


## 1.0.6
Fix Load Addressable
----------------------------------


## 1.0.5
Update Readme
----------------------------------