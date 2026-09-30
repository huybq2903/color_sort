# Module @Module_UI_Menu

## Tổng quan
- **Tên:** `Module_UI_Menu`
- **Giới thiệu:** Cung cấp giải pháp để di chuyển hình ảnh của một đơn vị tài nguyên từ vị trí A -> vị trí B.
- **Các thành phần chính:**
    - `MenuManager`: Component quản lý UI/UX phần Menu
    - `MenuNavigator`: Xử lý điều hướng các Tab lớn trong Menu.
    - `UIHome`: Khung UI, có thể chứa các module khác. Có các Tab lớn để hiển thị.
    - `UIHomeShortcuts`: Component này xử lý hiển thị danh sách các nút tắt.
    - `UIHomeShortcutsReceiveBus`: Nhận các nút tắt của các module khác, nếu muốn hiển thị vào UIHome

## Quick Start
    - Sử dụng đường dẫn editor: "Falcon/Modules/UI/*Menu*/Assets" để Import Assets cho phần này.
    - Bên trong thư mục Addressable/Prefabs có file Menu
    - Menu Cần phải kéo vào scene hoặc sinh ra tự động (Sử dụng: Instantie, Addressable,...).

## Chi tiết

### Cấu trúc Prefab:
Menu
    [Menu_Manager] -> UIMenu duy nhất. Quản lý tổng UI/UX.
        BG -> Hình nền
        [Scroll_Rect] -> Phần cuộn các Tab.
            Viewport
                Content -> Nơi chứa các Tab.
                    [Tab_0]
                    [Tab_1]
                    [Tab_2]
                    [Tab_3]
                    [Tab_5]
        [Navigator] -> Nơi chứa tính năng điều hướng.
    [Popup] -> Nơi chứa các popup được định nghĩa sẵn trong prefab.
    [Addons] -> Nơi chứa các tính năng bổ sung. Ví dụ: UIReward, UIFloating, UIToast,...

`Scroll_Rect` Sử dụng Tools `Simple Scroll-Snap` để xử lý phần snap cuộn các tab.

### MenuManager

Component chính chưa các hình nền và các Tab module.
Sử dụng EventBusHelper và UI_Core để hiển thị UI/UX.
Một số EventBus hỗ trợ. Khuyên sử dụng UIWrapper trong UI_Core sẽ sử dụng được dễ dàng và đầy đủ tính năng nhất.

``` csharp
GameEvent<Transform>.Register(Const.EVENT_OPEN_POPUP, popup => UIWrapper.OpenPopup(popup, p => GameEvent<Transform>.Emit(Const.EVENT_ON_OPEN_POPUP, p)), this);
GameEvent<string>.Register(Const.EVENT_OPEN_POPUP_NAME, name => UIWrapper.OpenPopup(name, p => GameEvent<Transform>.Emit(Const.EVENT_ON_OPEN_POPUP, p)), this);
GameEvent<Transform>.Register(Const.EVENT_CLOSE_POPUP, popup => UIWrapper.ClosePopup(popup), this);
GameEvent<string>.Register(Const.EVENT_CLOSE_POPUP_NAME, name => UIWrapper.ClosePopup(name), this);
```

#### Event bổ sung

``` csharp
public const string EVENT_MENU_GET_SCROLL_RECT_REQ = "falcon.modules.core.ui_menu_get_scroll_rect_req";
public const string EVENT_MENU_GET_SCROLL_RECT_RSP = "falcon.modules.core.ui_menu_get_scroll_rect_rsp";
public const string EVENT_MENU_LOAD_FEATURES_COMPLETE = "falcon.modules.core.ui_menu_load_features_complete";

GameEvent<int>.Register(MenuConst.EVENT_MENU_GET_SCROLL_RECT_REQ, obj => GameEvent<Transform>.Emit(MenuConst.EVENT_MENU_GET_SCROLL_RECT_RSP, scrollRect.transform), this);

```
EVENT_MENU_GET_SCROLL_RECT_REQ: yêu cầu lấy game object Scroll_Rect của Menu.
EVENT_MENU_GET_SCROLL_RECT_RSP: trả về game object Scroll_Rect.
EVENT_MENU_LOAD_FEATURES_COMPLETE: được gọi khi Menu load xong tất cả features.

### MenuNavigator

``` csharp
public const string EVENT_MENU_NAVIGATOR_GO_TO_TAB = "falcon.modules.ui.menu.navigator_go_to_tab";
public const string EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED = "falcon.modules.ui.menu.navigator_on_tab_changed";

GameEvent<(int index, bool isForceSnap)>.Register(MenuConst.EVENT_MENU_NAVIGATOR_GO_TO_TAB, data => GoToTab(data.index, data.isForceSnap), this);

GameEvent<int>.Emit(MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED, _scrollSnap.CenteredPanel);

```
EVENT_MENU_NAVIGATOR_GO_TO_TAB: nếu muốn chuyển sang tab khác. Truyền vào index của tab và có bỏ qua hiệu ứng cuộn không. Nếu bỏ qua nó sẽ chuyển tab ngay lập tức.
EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED: được gọi khi chuyển tab. Trả về index của tab hiện tại.

### UIHome

Là khung layout chính của Menu. Bao gồm xử lý hình ảnh profile, số lượng gold, nút Settings.
Chứa UIHomeShortcuts và UIHomeShortcutsReceiveBus để xử lý phần nút tắt 2 bên trái / phải.

Đây là khung rất cơ bản.
Hãy duplicate hoặc sửa lại theo nhu cầu phù hợp với game.

#### Event bổ sung

``` csharp
public const string EVENT_LOAD_UI_COMPLETE = "falcon.modules.core.ui_home_load_complete";
```
EVENT_LOAD_UI_COMPLETE: được gọi khi UIHome load xong.

### UIHomeShortcuts

Là nơi chứa các phần nút tắt. 

Cấu trúc file:
[UIHome_Shortcuts]
    Layout_Expand -> lối tắt ở phần trên cùng. Ví dụ: sự kiện dạng tiến trình cần hiển thị full ở trên cùng.
    Layout_Left -> danh sách các lối tắt bên trái.
    Layout_Right -> danh sách các lối tắt bên phải.
    Target_End -> điểm neo để căn chỉnh lối tắt (bỏ qua)

### UIHomeShortcutsReceiveBus

Đi cùng UIHomeShortcuts. Có sự kiện EventBus để nhận các lối tắt từ các module khác gửi về.

``` csharp
public const string EVENT_ADD_SHORTCUT_LEFT = "falcon.modules.ui.home_shortcut_add_left";
public const string EVENT_ADD_SHORTCUT_RIGHT = "falcon.modules.ui.home_shortcut_add_right";
public const string EVENT_SHORTCUT_REMOVE = "falcon.modules.ui.home_shortcut_remove";
public const string EVENT_ADD_SHORTCUT_EXPAND = "falcon.modules.ui.home_shortcut_add_expand";

GameEvent<Transform>.Register(MenuConst.EVENT_ADD_SHORTCUT_LEFT, button =>
{
    //...
}, this);

GameEvent<Transform>.Register(MenuConst.EVENT_ADD_SHORTCUT_RIGHT, button =>
{
    //...
}, this);

GameEvent<Transform>.Register(MenuConst.EVENT_ADD_SHORTCUT_EXPAND, button =>
{
    //...
}, this);

GameEvent<Transform>.Register(MenuConst.EVENT_SHORTCUT_REMOVE, button =>
{
    //...
}, this);
```

Tham số là Transform của nút bấm (lối tắt).

### Cấu hình
`SO_UI_Menu_FeaturesConfig`
`SO_UI_Menu_NavigatorConfig`
`SO_UI_Home_ShortcutsConfig`
Đây là file cấu hình ScriptableObject nằm trong thư mục Resources, để có thể tùy chỉnh một số tham số được cung cấp.
Các thành phần đã được mô tả chi tiết ở bên ngoài Inspector.

#### SO_UI_Menu_FeaturesConfig
Sẽ chỉnh số số lượng tab.
Mỗi tab sẽ có những tính năng nào, liệt kê và nó sẽ load trong Addressable.
Addons cũng tương tự.

#### SO_UI_Menu_NavigatorConfig
Sẽ chỉnh số số lượng tab tương ứng SO_UI_Menu_FeaturesConfig.
Mỗi tab sẽ có tên Localize I2 và hình ảnh của nó.
Ngoài ra cung cấp một só thông số như chiều cao, animation,...

## Lưu ý:
- Nên duplicate code, prefab trong module ra thư mực riêng. Không được phép chỉnh sửa trực tiếp.
- Những file mẫu trong Import Assets. Có thể sử dụng nó và kéo về thư mục riêng để tùy chỉnh. 
- Khi cập nhật module mới nhất, sẽ không ảnh hưởng tới file Import Assets. Tuy nhiên khi lấy Import Assets mới nhất
có thể sẽ bị cảnh báo ghi đè file, do trùng GUID.
- Nếu sửa code, prefab trong module có thể sẽ làm module hoạt động sai hoặc mất code.
Cần phải kế thừa hoặc tạo riêng.
