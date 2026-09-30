## 2.3.9
- MenuNavigator Do giới hạn của odin 
không hiển thực được interface cho các prefab nested. Nên sẽ thay bằng MonoBehavior
** Chú ý: MenuNavigator có thể sẽ cần phải kéo lại trong Inspector.
----------------------------------


## 2.3.8
- Nếu duplicate, bổ sung xóa luôn shortcut vừa truyền vào nữa nếu nó đang tồn tại
----------------------------------


## 2.3.7
- Thêm kiểm tra có bị duplicate shortcut item hay không, thì sẽ xóa toàn bộ theo tên
shortcut item vừa thêm.
** Khuyên cáo: Không nên có nhiều shortcut item giống nhau trong màn Home. 
Framework này sẽ cố gắng xóa những item duplicate của lần gần nhất vửa thêm.
Tùy chọn này có thể tắt trong component UIHomeShortcutsReceiveBus.
** Vì check theo tên, nếu shortcut giống nhau nhưng khác tên, thì không tính là duplicate.
----------------------------------


## 2.3.6
- Fix Init Navigator
----------------------------------


## 2.3.5
- tích hợp facebook vào avatar màn home
----------------------------------


## 2.3.4
- Sửa function GoToTab. Nếu sử dụng isForce sẽ không đợi 1 frame nữa
----------------------------------


## 2.3.3
- Add Load Avatar Facebook if changed
----------------------------------


## 2.3.2
Update Singleton
----------------------------------


## 2.3.1
Optimize Scroll
----------------------------------


## 2.3.0
- Xóa UIAnimationLightPopup, chuyển code sang UIICore
----------------------------------


## 2.2.0
- Chuyển lấy menuConfig lên Awake
----------------------------------


## 2.1.8
New changes
----------------------------------


## 2.1.7
- Cập nhật lại event bus load xong hết các tabs
----------------------------------


## 2.1.6
Update Event Bus
----------------------------------


## 2.1.5
- Update Event Bus for UICore
----------------------------------


## 2.1.4
- Fix Force Snap Navigator
----------------------------------


## 2.1.3
- Check null term tab
----------------------------------


## 2.1.2
- Add force update canvas
----------------------------------


## 2.1.1
- Thêm một số tùy chọn về Tween
- Hỗ trợ trường hợp đặc biệt 5 tab
----------------------------------


## 2.0.18
Xóa code gọi hàm AccountManager.Init lúc trong Awake của UIHome.cs
----------------------------------


## 2.0.17
Add intertab animator
----------------------------------


## 2.0.16
Update Features
----------------------------------


## 2.0.15
Update Code
----------------------------------


## 2.0.14
Update Code
----------------------------------


## 2.0.13
Add I2
----------------------------------


## 2.0.11
Fix Tên asset SO_UI_Menu_FeaturesConfig
----------------------------------


## 2.0.10
New changes
----------------------------------


## 2.0.9
New changes
----------------------------------


## 2.0.8
New changes
----------------------------------


## 2.0.7
New changes
----------------------------------


## 2.0.6
change code
 _config = Resources.Load<UIHomeShortcutsConfig>("SO_UI_Menu_Home_ShortcutsConfig");
----------------------------------


## 2.0.5
New changes
----------------------------------


## 2.0.4
New Functions
----------------------------------


## 2.0.3
Verify Code
----------------------------------


## 2.0.2
New changes
----------------------------------


## 2.0.1
New changes
----------------------------------


## 2.0.0
Big Update
----------------------------------