# Chuyển UIClan thành Shared/Clan, xoá UIModular — Design

Ngày: 2026-09-10
Trạng thái: đã duyệt

## Mục tiêu

Đưa `Falcon/Modules/UIModular/UIClan` về `_Game/Shared/Modules/Clan` theo đúng khuôn mà
Leaderboard đã dùng, rồi xoá sạch `Falcon/Modules/UIModular` và
`FalconAssets/Modules/UIModular`.

Không phải mục tiêu: đổi protocol mạng, đổi tên `FAMessage`, ráp Clan vào luồng game, dựng
lại module UIProfile đã xoá.

## Ràng buộc kỹ thuật đã xác minh

| Phát hiện | Nguồn | Hệ quả cho thiết kế |
|---|---|---|
| `scEvt2Type[eventName] = type` — map theo tên, ghi đè im lặng | `FNetManager.cs:103` | 27 class `FAMessage` của Clan **không được** tồn tại hai bản cùng lúc. Bước 1 phải là move thật, không phải copy rồi xoá sau. |
| FReflection chỉ scan assembly có `FullName` chứa `"Falcon"` hoặc tên `Assembly-CSharp` | `FReflection.cs:45` | asmdef mới bắt buộc đặt tên `Falcon.Shared.Clan`. |
| Không asmdef nào ngoài UIModular tham chiếu tới 7 asmdef trong UIModular | quét toàn bộ `*.asmdef` | Xoá UIModular không làm hỏng assembly nào khác. |
| Chỉ 2 file UIModular bị prefab **ngoài** UIModular dùng: `UIScaleOnDown` trong `UILeaderboard.prefab`, `SafeAreaPortrait` trong `UICollections.prefab` | quét GUID toàn project | Hai chỗ này phải đổi component trước khi xoá. |
| `UIScaleOnDown` phụ thuộc `ISoundPlayer` / `IHapticPlayer` của UIModular/Core | `UIScaleOnDown.cs:8,28-29` | Không cứu được nếu Core chết. Bắt buộc thay bằng component khác. |
| Bề mặt framework cũ mà Clan chạm tới chỉ gồm `UIRuntimePopup`, `BasePresenter`, `IModel`, `IView`, `UIManager.Instance.ClosePopup` | 12 file trong 3 thư mục Popup* | Gỡ khỏi framework cũ là việc nhỏ và khép kín, không lan sang phần còn lại của module. |
| `AddPopupsToAddressable` tự gom mọi prefab `UIPopupBase` vào group `Popup` | `AddPopupsToAddressables.cs:19` | Popup Clan chỉ cần kế thừa đúng base rồi chạy menu, không phải khai báo addressable tay. |
| Popup chuẩn của project: prefab mang `UIPopupAutoFade`, class logic khai `: MonoBehaviour` và không import EasyPopup; `Falcon.Shared.BaseBooster` không tham chiếu EasyPopup | `UIPopupBuyBooster.cs:14`, `UIPopupBuyBooster.prefab`, `Falcon.Shared.BaseBooster.asmdef` | Popup Clan làm y hệt. Toàn project chỉ 2 asmdef tham chiếu EasyPopup, Clan không cần là cái thứ ba. |
| `UIWrapper.OpenPopup(name, Action<Transform>)` và `ClosePopup(name/Transform)` nằm trong `Falcon.Modules.Core.UI.Runtime` | `UIWrapper.cs:61,82,92` | Đủ để mở và đóng popup, callback trả `Transform` rồi `GetComponent` nên class logic không cần base class riêng. |
| Prefab Clan đang dùng `UnityEngine.UI.Text`: 28 component trên 15 prefab, 22 field trong 8 script | quét `m_FontData` và khai báo field | Bỏ `MultiLineEllipsisLegacy` đồng nghĩa phải chuyển toàn bộ sang TMP. |
| `WrapperTime.AddTick` + `remain.ToTime()` là cách Leaderboard đếm ngược, không cần Update riêng | `LbCountdown.cs:38-46,72-76` | `CountdownTmpBasic` thay bằng `ClanCountdown` viết theo mẫu này. `WrapperTime` nằm trong `Falcon.Shared.Common` nên không thêm tham chiếu. |
| Clan chưa được ráp vào game, chỉ `UIClan_ComingSoon` nằm trong Addressables group `Common` | quét GUID prefab | Không có đường hồi quy runtime nào để bảo vệ. Cửa kiểm chứng duy nhất là compile và quét ref hỏng. |
| `LeaderboardService` đã khai ba khoá EventBus chờ module Clan: `join_clan`, `leave_clan`, `get_user_clan`, hiện không ai emit nên `LbClanEntry.MyClanCode` luôn bằng 0 | `LeaderboardService.cs:29-45` | `ClanService` phải emit đúng ba khoá này. Làm vậy đồng thời sửa lỗi BXH clan không tô được dòng của chính mình. |
| `UITab_Parent` có sẵn `lsChild`, `lsButton`, `ConditionSelect`, `Active(index)` | `UITab_Parent.cs:16-50` | Đủ thay `ClanToggle` cho ba tab của `NoClanPanel`. |
| Toast của game là `Falcon/Modules/UI/UIToast`, kích bằng `GameKeys.TOAST_OPEN` và `TOAST_OPEN_LOCALIZE` | `GameKeys.cs:54-55`, `StartBehaviour.cs:105,189` | Clan emit thẳng khoá này như Boosters, Mediation, Profile đang làm. Không cần tham chiếu assembly Toast. |
| `IClanCustom` chỉ có hai implementer: `ClanCustomDemo` (ForDemo) và `ClanCustom_ByUser` (FalconAssets/UIModular) | quét toàn project | Cả hai đều nằm trong danh sách xoá. Sau refactor không còn ai implement, interface mất lý do tồn tại. |
| `Falcon.Shared.Profile` đã có `UIProfile_Avatar`, `UIProfile_Frame`, `UITeam` | `Profile/Scripts/UI/` | Thay được `OnSetAvatarUI` và `OnSetNameStyleUI`, nối qua EventBus chứ không tham chiếu asmdef. |
| `ISessionListener` kế thừa `IFReflection`, nên `ClanNetwork` kéo theo `Falcon.Helpers.FReflection.Runtime` dù không file nào import chuỗi "FReflection" | `ISessionListener.cs:12` | Phải giữ tham chiếu này. Grep theo tên không thấy vì phụ thuộc đi qua kế thừa interface. |
| 23 trong 84 script Clan gốc dùng pattern `_Virtual`, chiếm 3507 trên 6509 dòng; Leaderboard không có file nào | quét `_Virtual` | Bỏ pattern này cắt khoảng 700-900 dòng và trùng mục tiêu spec Leaderboard 2026-08-12. |
| `SO_UI_Menu_FeaturesConfig.asset` khai `featuresTab_4: UIClan_ComingSoon`, nạp qua addressable | `SO_UI_Menu_FeaturesConfig.asset:26-27` | Prefab này đang sống, **không được xoá** cùng `FalconAssets/Modules/UIModular`. Phải chuyển sang `Shared/Clan/Prefabs` và giữ nguyên key. |
| `UIScaleOnDown` có 48 chỗ trên 16 prefab, `SafeAreaPortrait` có 10 chỗ trên 5 prefab | quét GUID | Việc đổi component lớn hơn hai chỗ ngoài Clan, phần lớn nằm ngay trong Clan. |
| `canShowClan` gán `true` rồi `WaitUntil` chính nó; override duy nhất của `DoSomethingBeforeShowClan` nằm ở `ClanPanelManager_ByUser.cs` sắp xoá | `ClanPanelManager.cs:86,95,99` | Bỏ `_Virtual` thì cờ, coroutine và hook đều thành rác. `OnOpenOrReconnect` chạy thẳng. |
| Tab của menu được instantiate sẵn và luôn active nên `OnEnable` chỉ chạy một lần; Leaderboard phải bám `MenuConst.EVENT_MENU_NAVIGATOR_ON_TAB_CHANGED` | `UILeaderboardPanel.cs:98,104` | Lần này chưa ráp Clan vào tab 4 nên chưa cần. Khi thay `UIClan_ComingSoon` bằng `UIClanPanel` thì phải thêm tham chiếu `Falcon.Modules.UI.Menu.Runtime` và bám khoá này. |

## Phạm vi

**Có:** toàn bộ Clan gồm 20 prefab, 22 sprite UI, 86 sprite logo, và 62 script còn lại sau khi
lược từ 84 file gốc.

**Không có:** scene `ClanDemo.unity`, thư mục `ForDemo` 4 file, `package.json`, `CHANGELOG.md`,
`README.md`, và `FalconAssets/Modules/UIModular` gồm nhóm prefab `_ForUser` / `_ByUser`.
Ngoại lệ duy nhất trong thư mục đó là `UIClan_ComingSoon.prefab`, nó đang là `featuresTab_4`
của menu Home nên được chuyển sang `Shared/Clan/Prefabs` chứ không xoá.
Hai prefab `AvatarUIClan_Anchor_ByUser` và `NameStyleUIClan_Anchor_ByUser` đang hỏng vì mất
base ở module UIProfile đã xoá, chúng biến mất theo.

## Vị trí file đích

```
Assets/_Game/Shared/Modules/Clan/
├── Falcon.Shared.Clan.asmdef
├── SA_Clan.spriteatlasv2
├── SO_ClanLogoDatabase.asset
├── Prefabs/            20 prefab + UIClan_ComingSoon, giữ Panels/ và Messages/
├── Sprites/            22 sprite UI + 86 sprite logo
└── Scripts/
    ├── ClanService.cs, UIClanPanel.cs
    ├── Data/           4 file
    ├── Message/        22 CS/SC + 5 file HelpRequest
    ├── ChatRoom/       8 file
    ├── Panel/          11 file
    ├── Popup/          UIPopupClanInfo.cs, UIPopupChooseLogo.cs, UIPopupHelpResource.cs
    ├── UI/             ClanLogoUI.cs, ClanRowUI.cs, ClanMemberRowUI.cs,
    │                   HelpResourceRow.cs, ClanCountdown.cs, ClickOutside.cs
    └── Editor/         ClanEditorTool.cs + Falcon.Shared.Clan.Editor.asmdef
```

Assembly `Falcon.Shared.Clan`, namespace gốc `Game.Shared.Clan`, 13 tham chiếu:

| Tham chiếu | Dùng cho |
|---|---|
| `Falcon.Shared.Common` | GameKeys, WrapperTime, Center / IInitialize |
| `Falcon.Shared.Tab` | field `UITab_Parent` trong `NoClanPanel` |
| `Falcon.Helpers.EventBus` | `GameEvent`, `GameRequest` |
| `Falcon.Modules.Core.Network.Runtime` | CS/SC, `AddSCListener` — 36 file |
| `Falcon.Modules.Core.AccountData.Runtime` | `AccountManager` |
| `Falcon.Modules.Core.UI.Runtime` | `UIWrapper.OpenPopup("PopupProfile")` |
| `Falcon.Modules.ChatRoom.Runtime` | 12 file |
| `SuperScrollView` | 3 file |
| `Unity.TextMeshPro` | 16 file |
| `UniTask` | `ClanService` |
| `Falcon.Helpers.FReflection.Runtime` | `ISessionListener : IFReflection`, xem `ISessionListener.cs:12` |
| `Falcon.Modules.Core.GameData.Runtime` | `ResourceCollector.ResourceAdd/ResourceRemove`, API tiền của project |
| `Falcon.Shared.Lives` | `WrapperLives.IsMax` và `Add(1)` cho luồng xin trợ giúp |

Bốn tham chiếu bị bỏ. Component chỉ nằm trên prefab thì Unity nối bằng GUID lúc
nạp, asmdef chỉ phục vụ lúc biên dịch, nên không cần khai:

| Bỏ | Vì sao |
|---|---|
| `Falcon.Modules.Translate.Runtime` | 0 file dùng; `Localize` chỉ là component trên prefab |
| `Falcon.Helpers.UI` | `UIButtonExtension`, `SafeArea`, `ScrollRectEx` chỉ là component trên prefab |
| `Falcon.Shared.Profile` | thay bằng khoá EventBus, tránh hai module Shared bám chéo |
| `Falcon.Shared.EasyPopup` | `UIPopupAutoFade` chỉ là component trên prefab; class logic kế thừa `MonoBehaviour` |

## Gộp ClanManager và ClanEventManager thành ClanService

`ClanEventManager` là 11 `UnityAction` static kèm 13 hàm bắn sự kiện, không nói chuyện được với
module khác. `ClanManager` là facade static 476 dòng ôm cả mạng, cả popup, cả logo. Cả hai thay
bằng một `ClanService : IInitialize` sống trong Center, đúng vai `LeaderboardService`.

- **Request và response** — 5 trong 11 event — chuyển sang `AddSCListener` theo từng lần gọi,
  bọc `UniTaskCompletionSource`, bên gọi `await`. Giống `LeaderboardService.Fetch`.
- **Server đẩy xuống thật sự** — disconnect, refresh, đổi userdata, coin anim — thành
  `event Action` trên instance, giống `OnConnectionChanged`.
- **`ClanNetwork : ISessionListener`** giữ lại nhưng chỉ gọi `SetConnected`, đúng vai
  `LbSessionListener`.
- **Emit ba khoá EventBus** `falcon.modules.clan.join_clan`, `leave_clan`, `get_user_clan` để
  Leaderboard biết mình đang ở clan nào.
- **Toast** emit `GameKeys.TOAST_OPEN_LOCALIZE` thay cho năm hàm toast cũ.

## Bỏ IClanCustom

27 thành viên của interface tan về những chỗ cụ thể:

| Nhóm | Số hàm | Đi đâu |
|---|---|---|
| Show, close popup | 7 | `UIPopupX.Show()` / `Hide()`; `ShowPlayerInfoPopup` thành `UIWrapper.OpenPopup("PopupProfile")` |
| Toast | 5 | `GameKeys.TOAST_OPEN_LOCALIZE` |
| Kinh tế và help | 6 | `ResourceCollector.Instance.ResourceAdd/ResourceRemove("gold", ...)` |
| Danh tính, level | 3 | `AccountManager.Instance.Code`, `GameRequest<int>.Request(GameKeys.GET_LEVEL)` |
| Avatar, name style | 2 | khoá EventBus, Profile lắng nghe và tự dựng bằng `UIProfile_Avatar`, `UIProfile_Frame`, `UITeam` |
| Logo | 2 | `SO_ClanLogoDatabase` |
| Còn lại | 2 | `ClanInfoAvailable` là validate tên, `GetMessagePrefabName` là logic nội bộ Clan |

Đánh đổi: `Shared/Clan` phụ thuộc thẳng vào AccountData, hết là module lắp rời mang sang project
khác. Leaderboard đã trả đúng cái giá này. Riêng phần Profile thì đi qua EventBus nên hai module
Shared không bám chéo nhau.

## UIClanPanel

`ClanPanelManager` đổi tên thành `UIClanPanel` cho khớp `UILeaderboardPanel`. Vai trò giữ nguyên:
chọn một trong bốn panel gốc là lock, no-internet, no-clan, in-clan.

Từ 144 dòng còn khoảng 40. Bốn event static thành subscribe vào `ClanService`. `canShowClan`,
coroutine `IEWaitAndShowClan` và hook `DoSomethingBeforeShowClan` bị bỏ vì chúng chỉ phục vụ
đường override của `_Virtual`.

## Bỏ pattern _Virtual

23 script cõng field `_behaviour` với `SerializeReference`, `BindOwner`, bảy hàm vòng đời forward
và một loạt property bọc lại. Pattern này tồn tại để game ngoài override logic bằng cách gán
subclass trong Inspector, cùng mục đích với `IClanCustom`. Bỏ cả hai thì logic nằm thẳng trong
class, cắt khoảng 700-900 dòng.

Prefab nào đang có `SerializeReference _behaviour` sẽ mất field đó khi reimport, phải kiểm lại
binding sau bước này.

## Port popup

Mỗi popup hiện là bốn file Installer, Model, Presenter, View. Gộp thành một class `MonoBehaviour`
theo đúng mẫu `UIPopupBuyBooster`:

```
ChooseLogoPopupInstaller : UIRuntimePopup          UIPopupChooseLogo : MonoBehaviour
ChooseLogoPopupPresenter : BasePresenter<M,V>  →   prefab mang thêm UIPopupAutoFade
ChooseLogoPopupModel : IModel
ChooseLogoPopupView : IView
IChooseLogoPopupPresenter
```

Mở và đóng qua `UIWrapper`, cùng assembly `Falcon.Modules.Core.UI.Runtime` đã tham chiếu:

```csharp
UIWrapper.OpenPopup("UIPopupClanInfo", popup => {
    var ui = popup.GetComponent<UIPopupClanInfo>();
    ui.Bind(data);
});
UIWrapper.ClosePopup(transform);   // popup tự đóng
```

`UIPopupAutoFade` lo nút back và fade, `AddPopupsToAddressable` vẫn gom được prefab vì component
đó kế thừa `UIPopupBase`. 15 file thành 3 file, và Clan không phải tham chiếu EasyPopup.

## Chuyển legacy Text sang TMP

28 component `Text` trên 15 prefab đổi thành `TextMeshProUGUI` dùng font asset
`SVN-Mikado Black SDF`. 22 field kiểu `Text` trên 8 script đổi kiểu theo. Chỗ nào trước đây
dựa vào `MultiLineEllipsisLegacy` thì đặt `overflowMode = Ellipsis`.

Việc này làm cho lần đổi font trên 15 prefab Clan hôm 2026-09-09 thành thừa: prefab sẽ không
còn tham chiếu file `.otf` nữa mà trỏ thẳng vào SDF asset.

## Lược bỏ và thay thế

| Thứ bị bỏ | Vì sao | Thay bằng |
|---|---|---|
| `MultiLineEllipsisLegacy` — 11 prefab | Chỉ phục vụ `UI.Text` | `overflowMode = Ellipsis` của TMP |
| `CountdownTmpBasic` — 1 prefab, `ChatBoxInClan` gọi | Tự chạy Update | `ClanCountdown` theo mẫu `LbCountdown` |
| `SafeArea.cs` bản Clan | Không prefab nào dùng | bỏ hẳn |
| `ScrollRectEx.cs` bản Clan — 5 prefab | Trùng `Falcon/Helpers/UI` | trỏ sang bản dùng chung |
| `UIScaleOnDown` — 48 chỗ / 16 prefab | Phụ thuộc UIModular/Core | `UIButtonExtension` |
| `SafeAreaPortrait` — 10 chỗ / 5 prefab | Nằm trong Kit sắp xoá | `SafeArea` của `Falcon/Helpers/UI` |
| `ClanLogoRsDemo` | Nguồn sprite của scene demo | `SO_ClanLogoDatabase` |
| `ClanEventManager` — 85 dòng | Event hub static, không nói chuyện được với module khác | `ClanService` |
| `ClanToggle` — 43 dòng, `ClanManagerPanel.prefab` | Toggle tự viết cho 3 tab của `NoClanPanel` | `UITab_Parent` + `UITab_Child` |
| `IClanCustom` — 27 hàm | Không còn ai implement | tan về 7 chỗ, xem bảng trên |
| `canShowClan` + `IEWaitAndShowClan` + `DoSomethingBeforeShowClan` | Chỉ phục vụ đường override đã bỏ | code chạy thẳng |
| pattern `_Virtual` — 23 file | Đường tuỳ biến thứ hai, trùng vai `IClanCustom` | logic viết thẳng trong class |

Cảnh báo cũ trong bản design này đã sai và được sửa: đổi sang `UIButtonExtension` KHÔNG mất âm
thanh hay haptic. `StartBehaviour.cs:96` đăng ký `GlobalButtonListener`, và handler ở dòng 144-148
gọi `HapticManager.LightFeedback()` cùng `AudioManager.PlaySFX(SoundEnum.Click)` cho mọi `Button`
được nhả. Điều kiện là object phải có `Button` thật. Hiện 42 object mang `UIScaleOnDown` đều
KHÔNG có `Button`, nên bước này phải thêm component đó.

## Nguồn logo

`SO_ClanLogoDatabase` giữ mảng sprite tra theo id. `ClanLogoUI.Init(int id)` đọc bảng thay vì
gọi `ClanLogoRsDemo.Instance`. Chữ ký giữ nguyên nên binding trên prefab không đổi.

## Thứ tự triển khai

Mỗi bước phải compile sạch trước khi sang bước sau.

1. **Move nguyên trạng.** Tạo `Shared/Clan` và asmdef mới, `git mv` toàn bộ UIClan sang, đổi
   namespace, gộp sprite từ `Falcon/Modules/Clan` và `FalconAssets/Modules/Clan`. Bỏ luôn demo,
   ForDemo, package.json, CHANGELOG, README. Move thật để 27 class `FAMessage` không có hai bản.
2. **Dọn helper.** Bỏ `SafeArea` bản Clan, trỏ `ScrollRectEx` sang bản dùng chung.
3. **Chuyển TMP.** 28 component và 22 field, rồi bỏ `MultiLineEllipsisLegacy`.
4. **Port popup.** 15 file thành 3 `MonoBehaviour`, prefab thêm `UIPopupAutoFade`, đổi tên
   prefab, chạy menu addressable.
5. **Gộp service.** `ClanService` thay `ClanManager` và `ClanEventManager`, emit ba khoá EventBus
   cho Leaderboard, toast chuyển sang `GameKeys.TOAST_OPEN_LOCALIZE`.
6. **Bỏ IClanCustom.** Rải 27 hàm về đúng chỗ theo bảng trên.
7. **Bỏ `_Virtual`.** 23 file, kéo logic từ class con lên thẳng MonoBehaviour, kiểm lại binding
   prefab sau khi reimport.
8. **Tab.** `UITab_Parent` thay `ClanToggle` trên `ClanManagerPanel.prefab`.
9. **Countdown.** `ClanCountdown` thay `CountdownTmpBasic`.
10. **Logo database.** Tạo SO, sửa `ClanLogoUI`.
11. **Đổi component.** `UIScaleOnDown` sang `UIButtonExtension` ở 48 chỗ trên 16 prefab,
    `SafeAreaPortrait` sang `SafeArea` ở 10 chỗ trên 5 prefab.
12. **Xoá UIModular.** Cả `Falcon/Modules/UIModular` lẫn `FalconAssets/Modules/UIModular`, cộng
    hai vỏ thư mục sprite Clan đã rỗng. Chuyển `UIClan_ComingSoon.prefab` ra trước khi xoá.

Bước 3, 4, 5 và 7 rủi ro nhất. Bước 3 và 4 vừa đổi kiến trúc vừa đổi binding trên prefab, bước 5
đổi cách toàn bộ module nói chuyện với mạng, bước 7 chạm 23 file cùng lúc. Giữ chúng tách rời để
revert độc lập được.

## Kiểm chứng

Sau mỗi bước:

- Unity compile không lỗi, đọc qua `GET /compile/status`.
- `validate_find_missing_scripts` với `searchInPrefabs` trả về 0.
- Quét ref hỏng trên `_Game`, `Falcon/Modules`, `FalconAssets` không vượt quá con số nền
  24 GUID ghi nhận ngày 2026-09-10.

Sau bước cuối, thêm: `Assets/Falcon/Modules/UIModular` và
`Assets/FalconAssets/Modules/UIModular` không còn tồn tại, không GUID nào của chúng còn bị tham
chiếu, và `featuresTab_4` vẫn nạp được `UIClan_ComingSoon`.
