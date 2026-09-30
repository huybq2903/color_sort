# Module Helper @Security

## Tổng quan
- **Tên:** `Prefab Utilities`
- **Giới thiệu:** Cung cấp các tiện ích cho prefab.
- **Tính năng chính:**
    - Fake Apply (giữ nguyên override ở prefab variant, thường dùng khi module có prefab nested).
## Quick Start
Đối với người tạo module:
- Thêm component `FakeApplyMarker.cs` vào Prefab Base.
- Hoặc `Falcon/Helper/FPrefabUtilities/Attach FakeApplyMarker (Nested Prefabs in Selected Folders)` giúp tự động detect và thêm component `FakeApplyMarker` vào tất cả các `prefab nested` trong các thư mục được chọn.
- Hoặc `Falcon/Helper/FPrefabUtilities/Attach FakeApplyMarker (All Prefabs in Selected Folders)` thêm component `FakeApplyMarker` vào tất cả các `prefab` trong các thư mục được chọn (không quan tâm có nested hay không).

Đối với người dùng module:
- Prefab module cho bạn sửa có thể có thêm Button `Fake Apply All` và `Reset prefab base from backup` ở Inspector:
- `Fake Apply All` dùng để apply các thay đổi vào Prefab Base (giống Apply all thường), nhưng những override ở Prefab Variant không bị xóa.
- `Reset prefab base from backup` dùng để Restore Prefab Base về trạng thái trước khi Fake Apply.
- Nếu muốn hàng loạt, `Falcon/Fake Apply All (Selected Folders)` giúp fake apply tất cả các prefab có gắn component `FakeApplyMarker` trong thư mục được chọn.