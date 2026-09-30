# Module @Core_UI

## Tổng quan
- **Tên:** `Core_UI`
- **Giới thiệu:** Cung cấp giải pháp quản lý giao diện UI/UX sử dụng Canvas cho toàn bộ modules.
- **Các thành phần chính:**
    - `UIWrapper`: Chứa các function tiện ích để có thể sử dụng nhanh.
    - `UIManager`: Hệ thống xử lý các luồng hiển thị nhiều popup, menu.
    - `UIBase`: Component cơ sở, chứa các thành phần chung của Popup và Menu.
    - `UIPopup`: Component popup có thể hiển thị/ẩn và có thể xếp chồng lên nhau. Không ảnh hưởng tới popup khác.
    - `UIMenu`: Component menu có thể hiển thị/ẩn và không thể xếp chồng lên nhau. Menu mới sẽ hiển thị thay thế menu cũ, menu cũ bị ẩn đi.
    - `UIPopupAddressableHandler`: Component xử lý dọn bộ nhớ nếu popup này sử dụng Addressable (Hỗ trợ tự động thêm).
    - `UIAnimation`: Component cơ sở để xử lý animation cho giao diện (Không bắt buộc sử dụng).

## Quick Start
    - Sử dụng Canvas_UIManager trong thư mục Prefabs kéo vào trong 1 Scene bất kỳ và lưu lại Scene.

## Chi tiết

### UIBase
Đây là component cơ sở. `UIPopup` và `UIMenu` sẽ kế thừa nó để xử lý riêng biệt.

```csharp
public class UIBase : MonoBehaviour
{
    public Action onHide;

    [HideInInspector]
    public float hidingTime = 0f;

    [HideInInspector]
    public UIAnimation uiAnimation;

    public override void ChangeVisibility(bool visible)
    {
        if (!Initialized)
            InitializeElements();

        base.visible = visible;

        if (visible) gameObject.SetActive(true);

        if (visible)
        {
            ShowAnimation();
        }
        else if (!visible)
        {
            HideAnimation();
            onHide?.Invoke();
        }

        if (deactivateWhileInvisible)
        {
            if (!visible)
                Invoke(nameof(DeactivateMe), hidingTime);
            else
                CancelInvoke(nameof(DeactivateMe));
        }
    }

    //...
}
```
Thành phần:
onInit: Khi ui lần đầu được khởi tạo sẽ gọi Action này
onShow: Khi ui được hiển thị sẽ gọi Action này
onHide: Khi ui được ẩn đi sẽ gọi Action này

hidingTime: Thời gian tối đa (thời gian chờ) để ẩn ui. Mặc định = 0, tức là ẩn luôn. Nếu ui này có đính kèm UIAnimation nó sẽ lấy bằng duration.
Ví dụ: khi đóng 1 popup, có diễn họa zoom to nhỏ. Thì popup sẽ phải đợi diễn họa đó xong thì mới ẩn thực sự đi.

### UIAnimation
Đây là component cơ sở. Chứa các function trạng thái, có thể kế thừa nó để tạo Animation riêng cho giao diện.

```csharp
public class UIAnimation : MonoBehaviour
{
    public float duration = 0.275f;
    public virtual void Init(UIBase parentUIBase) { }
    public virtual void Show(UIBase parentUIBase) { } 
    public virtual void Hide(UIBase parentUIBase) { }
}
```
Lưu ý: 3 function trên được gọi cùng 3 Action trong UIBase và được gọi trước Action.
Xem function ChangeVisibility trong mục UIBase.

### UIPopup
Đây là component được kéo vào và bắt buộc ở bên ngoài cùng của prefab. 
Hệ thống sẽ quét component này để xử lý, nếu không sẽ không nhận diện được
nó là Popup và sinh ra lỗi.

```csharp
public class UIPopup : UIBase
{
    [HorizontalGroup("TypePopup"), ToggleLeft]
    public bool isFullScreen;

    [HorizontalGroup("TypePopup"), ToggleLeft]
    public bool isHalfScreen;

    // ...
}
```

Thành phần:
isFullScreen: Đánh dấu Popup dạng toàn màn hình. Ví dụ: UI Battle Pass, Shop, Event, ...
isHalfScreen: Đánh dấu Popup dạng 1 phần trên màn hình. Ví dụ: UI Remove Ads, Notify, Settings ...

Mục đích:
Tùy vào trường hợp sử dụng 2 biến này một cách hợp lý. 

Ví dụ 1: khi mở nhiều popup, nó có thể sẽ tăng draw call, batch.
Để tối ưu việc này, sẽ kiểm tra có popup fullscreen nào không thì sẽ ẩn hết đi những popup cũ, vì dạng popup fullscreen sẽ che hết 100% popup cũ,
nên popup cũ không có ý nghĩa về mặt hiển thị.

Ví dụ 2: khi mở 1 popup, thì thành phần phía sau cái popup đó ko thể thấy được. Sử dụng isHalfScreen để kiểm tra và ẩn thành phần nó đi.

#### Lưu trữ Popup

Có 3 dạng Popup:
- Được kéo sẵn ở trong prefab `Canvas_UIManager` và kéo vào danh sách popup trong component `UIManager`.
- Tạo Scene theo tên của Popup. Gọi thông qua tên Scene.
- Tạo Addressable theo tên của Popup. Gọi thông qua tên Path. Popup này sẽ cần phải thêm `UIPopupAddressableHandler` để dọn bộ nhớ nếu Destroy.

Nếu popup đã từng mở ít nhất 1 lần, thì sẽ được lưu vào danh sách popup trong component `UIManager`. 
Lần sau gọi lại sẽ lấy từ đó ra.

**Có thể tự quản lý lưu trữ popup theo cách riêng, hãy đảm bảo popup đó không có trong component `UIManager`.**

### UIMenu
Đây là component được kéo vào và bắt buộc ở bên ngoài cùng của prefab. 
Hệ thống sẽ quét component này để xử lý, nếu không sẽ không nhận diện được
nó là Popup và sinh ra lỗi.

```csharp
public class UIMenu : UIBase
{
    [Header("The Watcher")]
    public UIMenu PreviousMenu;
    public UIMenu NextMenu;

    // ...
}
```

Thành phần:
PreviousMenu: reference menu trước đó trước khi menu mới được mở. 
Lưu ý: biến này có thể bị thay đổi trong quá trình mở/ẩn menu.

NextMenu: reference menu tiếp theo mà menu hiện tại có thể mở.
Lưu ý: biến này cố định được định nghĩa sẵn ban đầu. Sử dụng function `NextMenu()` trong `UIManager` để mở nhanh menu. 

Ví dụ: có 2 menu A và B. Menu A sẽ có next là menu B, Menu B sẽ có next là menu A. Xoay vòng như vậy. Function này rất ít được sử dụng nên không cần sử dụng và hãy bỏ qua biến này.

#### Lời khuyên:
Để xử lý UI/UX một cách đơn giản chúng ta chỉ cần 1 Menu duy nhất là đủ để đáp ứng 98% nhu cầu. 
Việc sử dụng nhiều menu có thể sẽ làm giao diện phức tạp không cần thiết.
Mọi giao diện khác hãy coi nó là Popup.

### UIWrapper
Lớp tiện ích tĩnh để thao tác UI, UIPopup dễ dàng thông qua UIManager

#### Sự kiện (Events)

`public static event Action onPopupChanged`
Kích hoạt khi stack popup thay đổi (mở hoặc đóng).

`public static event Action onPopupBeforeOpen`
Gọi ngay trước khi popup được mở.

`public static event Action onPopupBeforeClose`
Gọi ngay trước khi popup bị đóng.

#### Quản lý Popup

`public static void OpenPopup(string name, Action<Transform> callback = null)`
Mở popup theo tên. Gọi callback khi mở xong (nếu có).

`public static void OpenPopup(Transform popup, Action<Transform> callback = null)`
Mở trực tiếp một popup từ Transform. Gọi callback khi mở xong (nếu có).

`public static void ClosePopup(string name)`
Đóng popup dựa trên tên.

`public static void ClosePopup(Transform popup)`
Đóng popup dựa trên Transform.

#### Truy vấn Popup Stack

`public static List<Transform> GetAllPopupStack()`
Trả về danh sách Transform của các popup đang nằm trong stack.

`public static int GetNumberPopupStack()`
Trả về số lượng popup hiện tại đang mở.

#### Kiểm tra slot popup

`public static bool IsFree_UIPopupFullScreen()`
Trả về true nếu slot popup toàn màn hình đang trống.

`public static bool IsFree_UIPopupHalfScreen()`
Trả về true nếu slot popup nửa màn hình đang trống.

#### Shortcut

`public static UIManager Manager => UIManager.Instance;`
Truy cập nhanh singleton UIManager nếu cần mở rộng nâng cao.

## Lỗi thường gặp
- Không tìm thấy Menu. Hãy đảm bảo có ít nhất 1 Menu được kéo vào trong component.

## Lưu ý
- Canvas_UIManager chỉ chứa phần khung cơ bản nhất của một luồng UI/UX.
- Tham khảo module UI Framework Menu, UI Framework GamePlay (UI_InGame). 2 module này là mở rộng từ Canvas_UIManager