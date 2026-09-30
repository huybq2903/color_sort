# Module @Module_UI_Setting
## Tổng quan
- **Tên:** `Module_UI_Setting`
- **Giới thiệu:** Cung cấp giải pháp hiển thị thông tin cài đặt và ngôn ngữ.
- **Các thành phần chính:**
    - `UISettingWrapper`: API tĩnh hỗ trợ hiển thị.
    - `UIPopupSetting`: Popup chỉnh sửa phần cài đặt và một số thông tin cơ bản khác.
    - `UIPopupSettingLanguage`: Popup sửa đổi ngôn ngữ.
    - `FGameDataUISetting`: Cấu trúc dữ liệu.
    - `FGameDataUISettingWrapper`:  API tĩnh xử lý dữ liệu.

## Quick Start
    - Sử dụng đường dẫn editor: "Falcon/Modules/UI/Settings/Assets" để Import Assets cho phần này.

## Chi tiết

### UISettingWrapper
Sử EventBus Helper. Gửi sự kiện để mở Popup Setting lên.

```csharp
GameEvent<int>.Register(Const.EVENT_OPEN_UI, obj =>
{
    UIWrapper.OpenPopup(Const.POPUP_SETTINGS);
}, null);

GameEvent<int>.Register(Const.EVENT_CLOSE_UI, obj => UIWrapper.ClosePopup(Const.POPUP_SETTINGS), null);
```

### UIPopupSetting
Popup này hiển thị và hỗ trợ thay đổi một số chỉ số cài đặt của game.
Popup này đã bao gồm nút bấm để mở UIPopupSettingLanguage.

### UIPopupSettingLanguage

Popup này hỗ trợ thay đổi ngôn ngữ của game. Sử dụng I2 Localize.

### FGameDataUISetting
```csharp
[FGameDataType("ui_setting")]
public class FGameDataUISetting : FGameData<FGameDataUISetting>
{
    public int sound = 1;
    public int vibrate = 1;
    public int music = 1;
}
```
Hiện tại đang lưu 3 thông số.
Âm thanh (Sound)
Rung (Haptic/Vibrate)
Nhạc nền (Music)

### FGameDataUISettingWrapper

Sử EventBus Helper. 

```csharp
GameEvent<List<string>>.Register("falcon.modules.ui.settings_config_req", listFields =>
{
    var configSetting = Resources.Load<UISettingConfig>("SO_UI_SettingConfig");
    
    List<object> data = new();
    for (int i = 0; i < listFields.Count; i++) data.Add(GameDataUtils.GetFieldValueByName(configSetting, listFields[i]));

    GameEvent<List<object>>.Emit("falcon.modules.ui.settings_config_rsp", data);
}, null);

GameEvent<int>.Register("falcon.modules.ui.settings_media_req", status =>
{
    var sound = FGameDataUISetting.Instance.sound;
    var vibrate = FGameDataUISetting.Instance.vibrate;
    var music = FGameDataUISetting.Instance.music;
    GameEvent<(int sound, int vibrate, int music)>.Emit("falcon.modules.ui.settings_media_rsp", (sound, vibrate, music));
}, null);

GameEvent<(string name, int value)>.Register("falcon.modules.ui.settings_media_save", data =>
{
    if (data.name == "sound") FGameDataUISetting.Instance.sound = data.value;
    if (data.name == "vibrate") FGameDataUISetting.Instance.vibrate = data.value;
    if (data.name == "music") FGameDataUISetting.Instance.music = data.value;
    FGameDataUISetting.Instance.Save();
    FGameDataUISetting.Instance.UpdateToServer();
}, null);

GameEvent<int>.Register("falcon.modules.ui.settings_account_id_req", status =>
{
    GameEvent<int>.Emit("falcon.modules.ui.settings_account_id_rsp", AccountManager.Instance.Code);
}, null);
```
Tên event: 
falcon.modules.ui.settings_config_req: yêu lấy file config setting. Tham số truyền vào là danh sách tên các field trong config.
falcon.modules.ui.settings_config_rsp: trả về danh sách giá trị (object) của các field.

falcon.modules.ui.settings_media_req: yêu cầu lấy giá trị trong `FGameDataUISetting`. Tham số truyền vào là tên field.
falcon.modules.ui.settings_media_rsp: trả về giá trị (object) của field.

falcon.modules.ui.settings_media_save: lưu lại dữ liệu.

falcon.modules.ui.settings_account_id_req: yêu cầu lấy code (id) của người chơi
falcon.modules.ui.settings_account_id_rsp: phản hồi lại code

Hoàn toàn có thể sử dụng trực tiếp `FGameDataUISetting` và `UISettingConfig` bằng cách Reference tới nó. Sẽ lấy dữ liệu dễ dàng hơn.

### Cấu hình
`UISettingConfig`
Đây là file cấu hình ScriptableObject nằm trong thư mục Resources, để có thể tùy chỉnh một số tham số được cung cấp.
Các thành phần đã được mô tả chi tiết ở bên ngoài Inspector

## Lưu ý:
- Nên duplicate code, prefab trong module ra thư mực riêng. Không được phép chỉnh sửa trực tiếp.
- Những file mẫu trong Import Assets. Có thể sử dụng nó và kéo về thư mục riêng để tùy chỉnh. 
- Khi cập nhật module mới nhất, sẽ không ảnh hưởng tới file Import Assets. Tuy nhiên khi lấy Import Assets mới nhất
có thể sẽ bị cảnh báo ghi đè file, do trùng GUID.
- Nếu sửa code, prefab trong module có thể sẽ làm module hoạt động sai hoặc mất code.
Cần phải kế thừa hoặc tạo riêng.
