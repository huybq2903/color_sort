# Module @Helper UI

## Tổng quan
- **Tên:** `Helper UI`
- **Giới thiệu:** Cung nhiều component tiện ích phục vụ UI Canvas.

## Chi tiết
ScrollRectEx: cơ chế giống ScrollRect, cho phép để chồng chéo nhau.
ScrollRectOcclusion: sử dụng cơ chế giống ScrollRect và Culling Camera. Nếu các item con trong Scroll Rect nằm bên ngoài Viewport sẽ bị ẩn đi.
UIButtonExtension: thêm chuyển động nếu bấm vào nút bấm. Kế thừa UISelectableExtension.
UISelectableExtension: mở rộng hành vi khi nhấn nút bấm.
BeardyGridLayoutGroup: một lựa chọn thú vị khác thay GridLayoutGroup.
UIColorGroupOverlay: cơ chế giống CanvasGroup, thay vì chỉnh alpha. Nó sẽ thay đổi Color các phần tử Graphics bên trong. Kết hợp đc với CanvasGroup.
UIContentSizeFitter: cơ chế giống ContentSizeFitter, cho phép để chồng chéo nhau.
UITMPCurve: cho phép đặt vị trí các ký tự theo độ cong có đỉnh được tùy chỉnh trong TMP.
