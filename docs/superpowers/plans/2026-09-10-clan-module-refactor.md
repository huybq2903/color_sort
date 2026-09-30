# Chuyển UIClan thành Shared/Clan — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Đưa `Falcon/Modules/UIModular/UIClan` về `_Game/Shared/Modules/Clan` theo khuôn Leaderboard, rồi xoá sạch `UIModular`.

**Architecture:** Một assembly `Falcon.Shared.Clan` với 10 tham chiếu. Mạng đi qua `ClanService : IInitialize` sống trong `Center`, dùng `AddSCListener` từng lần gọi bọc `UniTask`. UI dùng TMP, `UIWrapper` để mở đóng popup, `UITab_Parent` cho tab, `UIPopupAutoFade` cho fade. Không còn framework UIModular/Core, không còn `IClanCustom`, không còn pattern `_Virtual`.

**Tech Stack:** Unity 6000.3.16f1, C#, asmdef, Addressables, TextMeshPro, UniTask, SuperScrollView, DOTween.

**Spec:** `docs/superpowers/specs/2026-09-10-clan-module-refactor-design.md`

## Global Constraints

- Assembly **bắt buộc** tên `Falcon.Shared.Clan`. `FReflection.cs:45` chỉ scan assembly có `FullName` chứa `"Falcon"` hoặc tên `Assembly-CSharp`.
- `rootNamespace` là `Game.Shared.Clan`, khớp cách Leaderboard đặt (`Falcon.Shared.Leaderboard` → `Game.Shared.Leaderboard`).
- 27 class `[FAMessage(...)]` của Clan **không được tồn tại hai bản cùng lúc**. `FNetManager.cs:103` map theo tên và ghi đè im lặng. Task 1 là move thật, không copy.
- 13 tham chiếu asmdef, không thêm: `Falcon.Shared.Common`, `Falcon.Shared.Tab`, `Falcon.Helpers.EventBus`, `Falcon.Helpers.FReflection.Runtime`, `Falcon.Modules.Core.Network.Runtime`, `Falcon.Modules.Core.AccountData.Runtime`, `Falcon.Modules.Core.UI.Runtime`, `Falcon.Modules.ChatRoom.Runtime`, `SuperScrollView`, `Unity.TextMeshPro`, `UniTask`.
- **Không** tham chiếu `Falcon.Shared.EasyPopup`, `Falcon.Helpers.UI`, `Falcon.Shared.Profile`, `Falcon.Modules.Translate.Runtime`. Component chỉ nằm trên prefab thì Unity nối bằng GUID, asmdef chỉ dùng lúc biên dịch.
- Comment trong code **một dòng ngắn**, kể cả `<summary>`. Giải thích dài thì nói trong chat.
- Font TMP dùng cho mọi text: asset `SVN-Mikado Black SDF`, GUID `d16b15b7a2769634eae3fdb4b9ca82b9`.
- **Không tự commit.** Mỗi task kết thúc bằng lệnh commit viết sẵn nhưng chỉ chạy khi chủ repo bảo chạy. Không đụng vào những gì họ đang stage dở.
- Unity đang mở kèm UnitySkills ở `http://127.0.0.1:8090`, chế độ `bypass`. Đó là bộ chạy kiểm chứng của plan này.

---

## File Structure

Đích cuối, `Assets/_Game/Shared/Modules/Clan/`:

| Đường dẫn | Trách nhiệm |
|---|---|
| `Falcon.Shared.Clan.asmdef` | assembly runtime, 10 tham chiếu |
| `SA_Clan.spriteatlasv2` | atlas cho 108 sprite |
| `SO_ClanLogoDatabase.asset` | bảng tra sprite logo theo id |
| `Prefabs/` | 20 prefab + `UIClan_ComingSoon` |
| `Sprites/` | 22 sprite UI + 86 sprite logo |
| `Scripts/ClanService.cs` | mạng, trạng thái clan, cầu nối EventBus |
| `Scripts/UIClanPanel.cs` | chọn một trong bốn panel gốc |
| `Scripts/Data/` | 4 file model |
| `Scripts/Message/` | 22 CS/SC + 5 file HelpRequest |
| `Scripts/ChatRoom/` | 8 file |
| `Scripts/Panel/` | 11 file |
| `Scripts/Popup/` | 3 `MonoBehaviour` |
| `Scripts/UI/` | 6 file, gồm `ClanCountdown`, `ClickOutside` |
| `Scripts/Editor/` | `ClanEditorTool.cs` + asmdef editor |

Biến mất: `ClanManager.cs`, `ClanEventManager.cs`, `ClanToggle.cs`, `IClanCustom.cs`, `ForDemo/` 4 file, `Helper/MultiLineEllipsisLegacy.cs`, `Helper/CountdownTmpBasic.cs`, `Helper/SafeArea.cs`, `Helper/ScrollRectEx.cs`, 12 file trong 3 thư mục `Popup*`, `ClanDemo.unity`, `package.json`, `CHANGELOG.md`, `README.md`.

---

## Bảng GUID dùng lại nhiều lần

| GUID | Là gì |
|---|---|
| `4891211c3e4e44878886aeb6361493e4` | `UIScaleOnDown` (bỏ) |
| `d719c8f7335f6bf44b9647f67136ef64` | `UIButtonExtension` (thay vào) |
| `8df451a16c634de298d24802c31f50b0` | `SafeAreaPortrait` (bỏ) |
| `3af6cd6274044bd41bb8dcba6f3e5bb7` | `SafeArea` của Helpers (thay vào) |
| `714c9bad9af448fc9ec997df2e2f34b2` | `ScrollRectEx` bản Clan (bỏ) |
| `16e9df741c9db54419275bba206617a8` | `ScrollRectEx` của Helpers (thay vào) |
| `985ea444e8144afd8155892b055d45dc` | `SafeArea` bản Clan (xoá, không ai dùng) |
| `9e8fd2bf6cd94cbe8a23a3308b809500` | `MultiLineEllipsisLegacy` (bỏ) |
| `a995216d588e4fed8656849b62e25de3` | `CountdownTmpBasic` (bỏ) |
| `b7afa4d698704d8e87fa2110f54bbc82` | `ClanToggle` (bỏ) |
| `fb63688c063a4d619a80dca2fb9cac9e` | `ClickOutside` (giữ) |
| `5b62438f09a08db4585a9028374ee450` | `UIPopupAutoFade` (thêm vào prefab popup) |
| `d16b15b7a2769634eae3fdb4b9ca82b9` | font asset `SVN-Mikado Black SDF` |
| `3e33562a0c7f72b4ca18c7e2a6386031` | `UIClan_ComingSoon.prefab` |

---

## Hai chỗ plan chưa chốt được

Ghi ra đây thay vì giấu trong task:

1. **API tiêu tài nguyên** — Task 6 Step 5. Chưa tìm được hàm trừ và cộng tiền dùng chung của project. Step đó có sẵn ba lệnh tìm và quy tắc chọn nhánh, phải chạy trước khi viết code.
2. **Phía Profile nghe avatar** — Task 6 Step 4. Clan sẽ emit `EVENT_BIND_AVATAR` và `EVENT_BIND_NAME_STYLE`, nhưng việc cho module Profile lắng nghe hai khoá đó nằm ngoài phạm vi. Cho tới lúc nối, avatar trong Clan hiển thị mặc định. Chấp nhận được vì Clan chưa ráp vào game, nhưng đây là việc còn nợ chứ không phải đã xong.

---

### Task 0: Bộ kiểm chứng và số nền

Project này không có unit test cho refactor kiểu này. Cửa kiểm chứng là compile của Unity, bản quét missing script, và số ref hỏng. Task này dựng một lệnh chạy cả ba, và chốt số nền để các task sau so.

**Files:**
- Create: `<scratchpad>/clan-check.sh`

**Interfaces:**
- Produces: lệnh `bash <scratchpad>/clan-check.sh` in ra ba dòng `COMPILE=`, `MISSING_SCRIPTS=`, `BROKEN_GUIDS=`.

- [ ] **Step 1: Viết script kiểm chứng**

```bash
#!/usr/bin/env bash
# Ba cửa kiểm chứng cho refactor Clan.
set -u
cd "D:/UnityProjects/TemplatePuzzle" || exit 1
S="$(dirname "$0")"

curl -s -m 600 -X POST http://127.0.0.1:8090/skill/asset_refresh \
  -H "Content-Type: application/json" -d '{}' > /dev/null

echo "COMPILE=$(curl -s -m 120 http://127.0.0.1:8090/compile/status \
  | grep -oE '"errorCount":[0-9]+' | head -1)"

echo "MISSING_SCRIPTS=$(curl -s -m 400 -X POST \
  http://127.0.0.1:8090/skill/validate_find_missing_scripts \
  -H "Content-Type: application/json" -d '{"searchInPrefabs":true}' \
  | grep -oE '"totalFound":[0-9]+')"

grep -rh --include='*.meta' "^guid: " Assets Packages Library/PackageCache 2>/dev/null \
  | sed 's/^guid: //' | tr -d '\r' | sort -u > "$S/known.txt"
grep -rn --include='*.prefab' --include='*.unity' --include='*.asset' \
  -E "guid: [0-9a-f]{32}" Assets/_Game Assets/Falcon/Modules Assets/FalconAssets \
  > "$S/refs.txt" 2>/dev/null
grep -oE "guid: [0-9a-f]{32}" "$S/refs.txt" | sed 's/guid: //' | sort -u \
  | comm -23 - "$S/known.txt" \
  | grep -vE "^0{16}[def]0{15}$|^0{32}$|^f70555f144d8491a825f0804e09c671c$" > "$S/broken.txt"
echo "BROKEN_GUIDS=$(wc -l < "$S/broken.txt")"
```

- [ ] **Step 2: Chạy để chốt số nền**

Run: `bash <scratchpad>/clan-check.sh`
Expected: `COMPILE="errorCount":0`, `MISSING_SCRIPTS="totalFound":0`, `BROKEN_GUIDS=24`

- [ ] **Step 3: Ghi số nền vào đầu plan**

Sửa dòng này bằng số thật vừa đo, để các task sau so được:
`BASELINE: compile 0 lỗi, 0 missing script, 24 GUID hỏng.`

Ba con số này là ngưỡng. Task nào làm chúng xấu đi thì task đó chưa xong. `BROKEN_GUIDS` được phép **giảm**, không được tăng.

- [ ] **Step 4: Liệt kê prefab có `SerializeReference _behaviour`**

Task 7 sẽ xoá pattern `_Virtual`. Prefab nào từng gán subclass tuỳ biến vào field đó sẽ mất logic im lặng, không lỗi compile. Chốt danh sách trước:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rln "_behaviour:" Assets/Falcon/Modules/UIModular/UIClan/Prefab \
  Assets/FalconAssets/Modules/UIModular --include='*.prefab' \
  > "<scratchpad>/behaviour-prefabs.txt"
grep -rn -A2 "_behaviour:" Assets/Falcon/Modules/UIModular/UIClan/Prefab --include='*.prefab' \
  | grep -B1 "type:" | head -40
```

Dòng nào có `type: {class: ..., ns: ..., asm: ...}` khác rỗng nghĩa là prefab đó **có** gán subclass, phải chép logic của subclass đó ra trước khi xoá. Ghi lại vào file, Task 7 đọc.

---

### Task 1: Dựng module và move nguyên trạng

Move thật, không copy. 27 class `FAMessage` không được có hai bản.

**Files:**
- Create: `Assets/_Game/Shared/Modules/Clan/Falcon.Shared.Clan.asmdef`
- Create: `Assets/_Game/Shared/Modules/Clan/Scripts/Editor/Falcon.Shared.Clan.Editor.asmdef`
- Move: toàn bộ `Assets/Falcon/Modules/UIModular/UIClan/**`
- Move: `Assets/Falcon/Modules/Clan/Sprites/**`, `Assets/FalconAssets/Modules/Clan/Sprites/**`
- Delete: `ClanDemo.unity`, `Scripts/Runtime/ForDemo/`, `package.json`, `CHANGELOG.md`, `README.md`

**Interfaces:**
- Produces: namespace `Game.Shared.Clan` cho mọi file Clan; assembly `Falcon.Shared.Clan`.

- [ ] **Step 1: Tạo asmdef runtime**

`Assets/_Game/Shared/Modules/Clan/Falcon.Shared.Clan.asmdef`:

```json
{
    "name": "Falcon.Shared.Clan",
    "rootNamespace": "Game.Shared.Clan",
    "references": [
        "Falcon.Shared.Common",
        "Falcon.Shared.Tab",
        "Falcon.Helpers.EventBus",
        "Falcon.Helpers.FReflection.Runtime",
        "Falcon.Modules.Core.Network.Runtime",
        "Falcon.Modules.Core.AccountData.Runtime",
        "Falcon.Modules.Core.UI.Runtime",
        "Falcon.Modules.ChatRoom.Runtime",
        "SuperScrollView",
        "Unity.TextMeshPro",
        "UniTask"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 2: Tạo asmdef editor**

`Assets/_Game/Shared/Modules/Clan/Scripts/Editor/Falcon.Shared.Clan.Editor.asmdef`:

```json
{
    "name": "Falcon.Shared.Clan.Editor",
    "rootNamespace": "Game.Shared.Clan.Editor",
    "references": [
        "Falcon.Shared.Clan",
        "Falcon.Shared.Common"
    ],
    "includePlatforms": [
        "Editor"
    ],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": false,
    "precompiledReferences": [],
    "autoReferenced": true,
    "defineConstraints": [],
    "versionDefines": [],
    "noEngineReferences": false
}
```

- [ ] **Step 3: Move file bằng git mv, giữ nguyên GUID**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
C="Assets/_Game/Shared/Modules/Clan"
U="Assets/Falcon/Modules/UIModular/UIClan"
mkdir -p "$C/Prefabs" "$C/Sprites" "$C/Scripts/Data" "$C/Scripts/Message" \
         "$C/Scripts/ChatRoom" "$C/Scripts/Panel" "$C/Scripts/Popup" \
         "$C/Scripts/UI" "$C/Scripts/Editor"

git mv "$U/Prefab"/* "$C/Prefabs/"
git mv "$U/Scripts/Runtime/Data"/* "$C/Scripts/Data/"
git mv "$U/Scripts/Runtime/CS SC"/* "$C/Scripts/Message/"
git mv "$U/Scripts/Runtime/HelpRequest"/CS*.cs "$U/Scripts/Runtime/HelpRequest"/CS*.cs.meta "$C/Scripts/Message/"
git mv "$U/Scripts/Runtime/HelpRequest"/SC*.cs "$U/Scripts/Runtime/HelpRequest"/SC*.cs.meta "$C/Scripts/Message/"
git mv "$U/Scripts/Runtime/HelpRequest/HelpResourceRow.cs" "$C/Scripts/UI/"
git mv "$U/Scripts/Runtime/HelpRequest/HelpResourceRow.cs.meta" "$C/Scripts/UI/"
git mv "$U/Scripts/Runtime/ChatRoom"/* "$C/Scripts/ChatRoom/"
git mv "$U/Scripts/Runtime/Panel"/* "$C/Scripts/Panel/"
git mv "$U/Scripts/Runtime/PopupChooseLogo" "$U/Scripts/Runtime/PopupClanInfo" \
       "$U/Scripts/Runtime/PopupHelpResource" "$C/Scripts/Popup/"
git mv "$U/Scripts/Runtime/UI"/* "$C/Scripts/UI/"
git mv "$U/Scripts/Runtime/Helper/ClickOutside.cs" "$C/Scripts/UI/"
git mv "$U/Scripts/Runtime/Helper/ClickOutside.cs.meta" "$C/Scripts/UI/"
git mv "$U/Scripts/Editor/ClanEditorTool.cs" "$U/Scripts/Editor/ClanEditorTool.cs.meta" "$C/Scripts/Editor/"
for f in ClanManager ClanEventManager ClanPanelManager ClanToggle IClanCustom; do
  git mv "$U/Scripts/Runtime/$f.cs" "$C/Scripts/"
  git mv "$U/Scripts/Runtime/$f.cs.meta" "$C/Scripts/"
done
git mv Assets/Falcon/Modules/Clan/Sprites/* "$C/Sprites/"
git mv Assets/FalconAssets/Modules/Clan/Sprites/* "$C/Sprites/"
```

- [ ] **Step 4: Xoá phần không mang theo**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
U="Assets/Falcon/Modules/UIModular/UIClan"
git rm -r "$U/Scripts/Runtime/ForDemo" "$U/ClanDemo.unity" "$U/ClanDemo.unity.meta" \
          "$U/package.json" "$U/package.json.meta" \
          "$U/CHANGELOG.md" "$U/CHANGELOG.md.meta" \
          "$U/README.md" "$U/README.md.meta" \
          "$U/Scripts/Runtime/Helper/SafeArea.cs" "$U/Scripts/Runtime/Helper/SafeArea.cs.meta"
```

- [ ] **Step 5: Đổi namespace**

Mọi file vừa move đang khai `namespace Falcon.Modules.UIModular.UIClan.Runtime`. Đổi hết:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rl "Falcon.Modules.UIModular.UIClan.Runtime" \
  Assets/_Game/Shared/Modules/Clan --include='*.cs' \
  | while IFS= read -r f; do
      sed -i 's/Falcon\.Modules\.UIModular\.UIClan\.Runtime/Game.Shared.Clan/g' "$f"
    done
grep -rn "Falcon.Modules.UIModular" Assets/_Game/Shared/Modules/Clan --include='*.cs'
```

Lệnh cuối phải in ra danh sách `using Falcon.Modules.UIModular.Core.Runtime` còn lại ở 12 file popup. Đó là nợ của Task 4, chưa xử ở đây.

- [ ] **Step 6: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: `COMPILE` sẽ **lỗi** vì 12 file popup còn `using Falcon.Modules.UIModular.Core.Runtime` mà asmdef mới không tham chiếu.

Đây là lỗi dự kiến. Để bước qua Task 4, tạm thêm `"Falcon.Modules.UIModular.Core.Runtime"` vào `references` của `Falcon.Shared.Clan.asmdef`, chạy lại, phải `COMPILE="errorCount":0`. Ghi TODO gỡ tham chiếu tạm này ở Task 4 Step 6.

- [ ] **Step 7: Commit (chờ chủ repo duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan Assets/Falcon/Modules/UIModular Assets/Falcon/Modules/Clan Assets/FalconAssets/Modules/Clan
git commit -m "refactor(clan): move UIClan sang _Game/Shared/Modules/Clan

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: Dọn helper trùng lặp

**Files:**
- Delete: `Assets/_Game/Shared/Modules/Clan/Scripts/UI/ScrollRectEx.cs` nếu Task 1 đã move nhầm; nếu chưa move thì xoá tại chỗ cũ
- Modify: 5 prefab dùng `ScrollRectEx` bản Clan

**Interfaces:**
- Consumes: module đã ở `_Game/Shared/Modules/Clan` (Task 1)
- Produces: không còn GUID `714c9bad9af448fc9ec997df2e2f34b2` và `985ea444e8144afd8155892b055d45dc` trong project

- [ ] **Step 1: Đổi GUID ScrollRectEx trên prefab**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
OLD=714c9bad9af448fc9ec997df2e2f34b2
NEW=16e9df741c9db54419275bba206617a8
grep -rl "$OLD" Assets --include='*.prefab' --include='*.unity' \
  | while IFS= read -r f; do sed -i "s/$OLD/$NEW/g" "$f"; echo "patched: $f"; done
```

Expected: 5 dòng `patched:`, gồm ChooseLogoPopup, ClanInfoPopup, ClanManagerPanel, HelpResourcePopup, InClanPanel.

- [ ] **Step 2: Xoá file ScrollRectEx bản Clan**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm "Assets/_Game/Shared/Modules/Clan/Scripts/UI/ScrollRectEx.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/UI/ScrollRectEx.cs.meta" 2>/dev/null \
  || git rm "Assets/Falcon/Modules/UIModular/UIClan/Scripts/Runtime/Helper/ScrollRectEx.cs" \
            "Assets/Falcon/Modules/UIModular/UIClan/Scripts/Runtime/Helper/ScrollRectEx.cs.meta"
```

- [ ] **Step 3: Kiểm không còn tham chiếu tới hai GUID cũ**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rl "714c9bad9af448fc9ec997df2e2f34b2\|985ea444e8144afd8155892b055d45dc" Assets \
  | grep -v FR2_Cache
```

Expected: không in gì.

- [ ] **Step 4: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: `COMPILE="errorCount":0`, `MISSING_SCRIPTS="totalFound":0`, `BROKEN_GUIDS` không tăng.

- [ ] **Step 5: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "refactor(clan): dung ScrollRectEx cua Falcon.Helpers.UI, bo ban trung

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 3: Chuyển legacy Text sang TMP

28 component `Text` trên 15 prefab, 22 field kiểu `Text` trên 8 file.

**Files:**
- Modify: 8 file có field `Text` — `ChatRoom/ClanChatRoomMessage_NOTI.cs`, `ChatRoom/ClanChatRoomMessage_TEXT.cs`, `ChatRoom/ClanChatRoom_PlayerComponentInMessage.cs`, `UI/HelpResourceRow.cs`, `Panel/ChatRoomPanel.cs`, `Popup/PopupClanInfo/ClanInfoPopupView.cs`, `UI/ClanMemberRowUI.cs`, `UI/ClanRowUI.cs`
- Modify: 15 prefab trong `Clan/Prefabs`
- Delete: `Scripts/UI/MultiLineEllipsisLegacy.cs`

**Interfaces:**
- Produces: mọi field text trong Clan có kiểu `TMPro.TextMeshProUGUI`.

- [ ] **Step 1: Đổi kiểu field trong 8 file**

Với mỗi file, đổi `using UnityEngine.UI;` giữ nguyên nếu còn dùng `Image`/`Button`, thêm `using TMPro;`, rồi đổi từng khai báo. Ví dụ `UI/ClanRowUI.cs`:

```csharp
// trước
[SerializeField] private Text _txtName;
// sau
[SerializeField] private TextMeshProUGUI _txtName;
```

Chỗ nào trước đây gắn `MultiLineEllipsisLegacy` thì đặt trong `Awake` hoặc ngay trên Inspector:

```csharp
_txtName.overflowMode = TextOverflowModes.Ellipsis;
```

Tìm đủ 22 chỗ:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rnE "(SerializeField|public|private|protected).*\bText\b +[_a-zA-Z]" \
  Assets/_Game/Shared/Modules/Clan/Scripts --include='*.cs'
```

- [ ] **Step 2: Đổi component trên prefab trong Unity**

Đây là việc phải làm trong Editor, không sửa YAML tay được vì `Text` và `TextMeshProUGUI` khác cấu trúc serialize hoàn toàn. Với mỗi trong 15 prefab: mở prefab, với mỗi GameObject có `Text`, ghi lại nội dung, alignment, màu, cỡ chữ, rồi Remove Component `Text`, Add Component `TextMeshPro - Text (UI)`, gán font asset `SVN-Mikado Black SDF`, khôi phục thuộc tính, gán lại vào field của script.

Danh sách 15 prefab và số component mỗi cái:

| Prefab | Số `Text` |
|---|---|
| ClanEditPanel | 4 |
| CreateClanPanel | 4 |
| OtherTextMessagePrefab | 3 |
| YourTextMessagePrefab | 3 |
| ClanInfoPopup | 2 |
| ClanManagerPanel | 2 |
| ClanRowPrefab | 2 |
| ClanMemberRow | 1 |
| HelpResourcePopup | 1 |
| JoinRequestPrefab | 1 |
| NotiMessagePrefab | 1 |
| OtherHelpMessagePrefab | 1 |
| YourHelpMessagePrefab | 1 |
| NameStyleUI_Anchor | 1 |
| InClanPanel | 1 |

- [ ] **Step 3: Xoá MultiLineEllipsisLegacy**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm "Assets/_Game/Shared/Modules/Clan/Scripts/UI/MultiLineEllipsisLegacy.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/UI/MultiLineEllipsisLegacy.cs.meta"
grep -rl "9e8fd2bf6cd94cbe8a23a3308b809500" Assets --include='*.prefab' | grep -v FR2_Cache
```

Expected: lệnh grep không in gì. Nếu còn prefab nào, component `MultiLineEllipsisLegacy` chưa được gỡ ở Step 2.

- [ ] **Step 4: Kiểm không còn legacy Text trong Clan**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rc "m_FontData:" Assets/_Game/Shared/Modules/Clan/Prefabs --include='*.prefab' \
  | grep -v ":0" || echo "sach"
```

Expected: `sach`.

- [ ] **Step 5: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi. `BROKEN_GUIDS` có thể giảm nếu prefab bỏ được ref font cũ.

- [ ] **Step 6: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "refactor(clan): chuyen legacy Text sang TMP, bo MultiLineEllipsisLegacy

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: Port popup sang MonoBehaviour

15 file thành 3. Theo đúng mẫu `UIPopupBuyBooster`: class logic khai `: MonoBehaviour`, prefab mang `UIPopupAutoFade`, mở đóng bằng `UIWrapper`.

**Files:**
- Create: `Scripts/Popup/UIPopupClanInfo.cs`, `Scripts/Popup/UIPopupChooseLogo.cs`, `Scripts/Popup/UIPopupHelpResource.cs`
- Delete: 15 file trong `Scripts/Popup/PopupChooseLogo`, `PopupClanInfo`, `PopupHelpResource`
- Modify: `Prefabs/ClanInfoPopup.prefab`, `ChooseLogoPopup.prefab`, `HelpResourcePopup.prefab`
- Modify: `Falcon.Shared.Clan.asmdef` — gỡ tham chiếu tạm

**Interfaces:**
- Consumes: TMP field từ Task 3
- Produces: `UIPopupClanInfo.Bind(ClanData data)`, `UIPopupChooseLogo.Bind(Action<int> onPick)`, `UIPopupHelpResource.Bind()` — Task 6 gọi ba hàm này

- [ ] **Step 1: Viết UIPopupClanInfo**

Gộp `ClanInfoPopupInstaller`, `ClanInfoPopupModel`, `ClanInfoPopupPresenter`, `ClanInfoPopupView`, `IClanInfoPopupPresenter` thành một file. Khung:

```csharp
using Falcon.Modules.Core.UI.Runtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shared.Clan
{
    /// <summary>Popup xem thông tin clan; prefab mang UIPopupAutoFade lo fade và nút back.</summary>
    public class UIPopupClanInfo : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _txtName;
        [SerializeField] private TextMeshProUGUI _txtDescription;
        [SerializeField] private TextMeshProUGUI _txtMemberCount;
        [SerializeField] private ClanLogoUI _logo;
        [SerializeField] private Button _btnLeave;

        public void Bind(ClanData data)
        {
            _txtName.text = data.name;
            _txtDescription.text = data.description;
            _txtMemberCount.text = $"{data.memberCount}/{data.maxMember}";
            _logo.Init(data.logoId);
        }

        public void Close() => UIWrapper.ClosePopup(transform);
    }
}
```

Chép nguyên phần thân hàm từ `ClanInfoPopupPresenter` và `ClanInfoPopupView` vào, bỏ tầng `IModel` `IView` `BasePresenter`. Chỗ nào gọi `UIManager.Instance.ClosePopup(_popupName)` thì đổi thành `Close()`.

- [ ] **Step 2: Viết UIPopupChooseLogo và UIPopupHelpResource**

Cùng khuôn. `UIPopupChooseLogo` nhận callback chọn logo:

```csharp
using System;
using Falcon.Modules.Core.UI.Runtime;
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Popup chọn logo clan.</summary>
    public class UIPopupChooseLogo : MonoBehaviour
    {
        [SerializeField] private Transform _content;
        [SerializeField] private ClanLogoUI _itemPrefab;

        private Action<int> _onPick;

        public void Bind(Action<int> onPick)
        {
            _onPick = onPick;
            Build();
        }

        private void OnPick(int id)
        {
            _onPick?.Invoke(id);
            Close();
        }

        public void Close() => UIWrapper.ClosePopup(transform);
    }
}
```

`Build()` sẽ hoàn thiện ở Task 10 khi có `SO_ClanLogoDatabase`. Tạm thời để nó dựng từ danh sách rỗng và ghi `Debug.LogWarning("[Clan] chưa gán logo database")`.

- [ ] **Step 3: Xoá 15 file cũ**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm -r "Assets/_Game/Shared/Modules/Clan/Scripts/Popup/PopupChooseLogo" \
          "Assets/_Game/Shared/Modules/Clan/Scripts/Popup/PopupClanInfo" \
          "Assets/_Game/Shared/Modules/Clan/Scripts/Popup/PopupHelpResource"
```

- [ ] **Step 4: Sửa prefab trong Unity**

Với ba prefab popup: gỡ component Installer cũ ở root, Add Component `UIPopupAutoFade`, Add Component class mới, gán `btnBack` cho `UIPopupAutoFade`, gán lại toàn bộ field TMP và Button cho class mới. Đổi tên file prefab:

```bash
cd "D:/UnityProjects/TemplatePuzzle/Assets/_Game/Shared/Modules/Clan/Prefabs"
git mv ClanInfoPopup.prefab UIPopupClanInfo.prefab
git mv ClanInfoPopup.prefab.meta UIPopupClanInfo.prefab.meta
git mv ChooseLogoPopup.prefab UIPopupChooseLogo.prefab
git mv ChooseLogoPopup.prefab.meta UIPopupChooseLogo.prefab.meta
git mv HelpResourcePopup.prefab UIPopupHelpResource.prefab
git mv HelpResourcePopup.prefab.meta UIPopupHelpResource.prefab.meta
```

- [ ] **Step 5: Đăng ký addressable**

Trong Unity chạy menu `Tools > Addressables > Add all UIPopupBase prefabs to 'Popup' group`. Kiểm ba prefab đã vào group:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -c "UIPopupClanInfo\|UIPopupChooseLogo\|UIPopupHelpResource" \
  Assets/AddressableAssetsData/AssetGroups/Popup.asset
```

Expected: `3` trở lên.

- [ ] **Step 6: Gỡ tham chiếu tạm khỏi asmdef**

Xoá dòng `"Falcon.Modules.UIModular.Core.Runtime"` khỏi `references` trong `Falcon.Shared.Clan.asmdef`. Rồi kiểm không còn ai import:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rn "Falcon.Modules.UIModular" Assets/_Game/Shared/Modules/Clan
```

Expected: không in gì.

- [ ] **Step 7: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: `COMPILE="errorCount":0`. Đây là lần đầu Clan compile mà không cần UIModular.

- [ ] **Step 8: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan Assets/AddressableAssetsData
git commit -m "refactor(clan): port 3 popup sang MonoBehaviour + UIPopupAutoFade

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: ClanService thay ClanManager và ClanEventManager

**Files:**
- Create: `Scripts/ClanService.cs`
- Delete: `Scripts/ClanManager.cs`, `Scripts/ClanEventManager.cs`
- Modify: mọi file gọi `ClanManager.` hoặc `ClanEventManager.`
- Modify: `Assets/_Game/Shared/Scripts/StartBehaviour.cs` — boot service
- Modify: các class `SC*` cần đẩy dữ liệu vào service

**Interfaces:**
- Consumes: 22 class CS/SC ở `Scripts/Message/`
- Produces:
  - `Center.GetOrCreate<ClanService>()`
  - `bool ClanService.Connected { get; }`
  - `bool ClanService.Unlocked { get; }`
  - `int ClanService.MyClanCode { get; }`
  - `event Action ClanService.OnConnectionChanged`
  - `event Action ClanService.OnClanChanged`
  - `event Action<Vector3, int> ClanService.OnShowCoinAnim`
  - `UniTask<SCClanInfo> ClanService.FetchClanInfo(int clanCode, CancellationToken ct)`
  - `UniTask<SCRandomListClan> ClanService.FetchRandomList(CancellationToken ct)`
  - `UniTask<SCSearchClan> ClanService.Search(string keyword, CancellationToken ct)`
  - `UniTask<SCJoinClan> ClanService.Join(int clanCode, CancellationToken ct)`
  - `UniTask<SCLeaveClan> ClanService.Leave(CancellationToken ct)`
  - `void ClanService.Toast(string localizeKey)`

- [ ] **Step 1: Viết khung ClanService**

```csharp
using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Falcon.Helpers.EventBus;
using Falcon.Modules.Core.AccountData;
using Falcon.Modules.Core.Network;
using Falcon.Shared.Common;
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Bọc CS/SC của clan thành UniTask, giữ trạng thái, làm cầu EventBus sang module khác.</summary>
    public class ClanService : IInitialize
    {
        private const int TIMEOUT = 10;

        // Ba khoá này LeaderboardService đã chờ sẵn, xem LeaderboardService.cs:29-31
        public const string EVENT_JOIN_CLAN = "falcon.modules.clan.join_clan";
        public const string EVENT_LEAVE_CLAN = "falcon.modules.clan.leave_clan";
        public const string EVENT_GET_USER_CLAN = "falcon.modules.clan.get_user_clan";

        public int MyClanCode { get; private set; }
        public bool Connected { get; private set; }
        public int LevelUnlock { get; private set; } = -1;
        public bool Unlocked => LevelUnlock >= 0 && CurrentLevel >= LevelUnlock;

        private static int CurrentLevel => GameRequest<int>.Request(GameKeys.GET_LEVEL);

        public event Action OnConnectionChanged;
        public event Action OnClanChanged;
        public event Action<Vector3, int> OnShowCoinAnim;

        public void OnInitialize()
        {
            GameEvent<(int, Action<int, string, Sprite>)>.Register(
                EVENT_GET_USER_CLAN, OnAskMyClan, null);
        }

        private void OnAskMyClan((int code, Action<int, string, Sprite> reply) req)
        {
            if (req.code == AccountManager.Instance.Code) req.reply?.Invoke(MyClanCode, null, null);
        }

        internal void SetConnected(bool value)
        {
            if (Connected == value) return;
            Connected = value;
            OnConnectionChanged?.Invoke();
        }

        internal void SetConfig(int levelUnlock)
        {
            LevelUnlock = levelUnlock;
        }

        /// <summary>Đổi clan hiện tại và báo cho Leaderboard biết.</summary>
        internal void SetMyClan(int clanCode, string clanName, Sprite logo)
        {
            MyClanCode = clanCode;
            if (clanCode > 0)
                GameEvent<(int, string, Sprite)>.Emit(EVENT_JOIN_CLAN, (clanCode, clanName, logo));
            else
                GameEvent.Emit(EVENT_LEAVE_CLAN);
            OnClanChanged?.Invoke();
        }

        public void Toast(string localizeKey) =>
            GameEvent<string>.Emit(GameKeys.TOAST_OPEN_LOCALIZE, localizeKey);

        internal void RaiseCoinAnim(Vector3 pos, int value) => OnShowCoinAnim?.Invoke(pos, value);
    }
}
```

- [ ] **Step 2: Thêm các hàm Fetch theo mẫu LeaderboardService**

Mỗi hàm một khuôn, chép cho cả 5:

```csharp
        public async UniTask<SCRandomListClan> FetchRandomList(CancellationToken ct)
        {
            var tcs = new UniTaskCompletionSource<SCRandomListClan>();
            new CSRandomListClan()
                .AddSCListener<SCRandomListClan>(
                    (msg, timeout, success) => tcs.TrySetResult(success ? msg : null), TIMEOUT)
                .Send();

            var sc = await tcs.Task.AttachExternalCancellation(ct);
            if (sc == null) Debug.LogWarning("[Clan] lấy danh sách clan thất bại.");
            return sc;
        }
```

Bốn hàm còn lại đổi cặp CS/SC và tham số: `FetchClanInfo` dùng `CSClanInfo { clan_code = clanCode }` với `SCClanInfo`; `Search` dùng `CSSearchClan { keyword = keyword }` với `SCSearchClan`; `Join` dùng `CSJoinClanReq { clan_code = clanCode }` với `SCJoinClan`; `Leave` dùng `CSLeaveClan()` với `SCLeaveClan`. Tên field thật đọc trong `Scripts/Message/`.

- [ ] **Step 3: Nối SessionListener**

Giữ nguyên `ClanNetwork : ISessionListener` nhưng rút gọn còn gọi service:

```csharp
namespace Game.Shared.Clan
{
    /// <summary>Báo trạng thái kết nối cho ClanService.</summary>
    public class ClanSessionListener : ISessionListener
    {
        public void OnFirstSession() => Center.GetOrCreate<ClanService>().SetConnected(true);
        public void OnSessionReset() => Center.GetOrCreate<ClanService>().SetConnected(true);
        public void OnChannelDisconnected(FChannel fChannel) =>
            Center.GetOrCreate<ClanService>().SetConnected(false);
    }
}
```

- [ ] **Step 4: Sửa các SC class đẩy dữ liệu vào service**

Theo mẫu `SCGetConfigLB.cs:23`. Ví dụ `SCClanConfig`:

```csharp
        public override void OnData()
        {
            Center.GetOrCreate<ClanService>().SetConfig(levelUnlock);
        }
```

`SCJoinClan` và `SCLeaveClan` gọi `SetMyClan(...)`.

- [ ] **Step 5: Boot service lúc khởi động**

Trong `Assets/_Game/Shared/Scripts/StartBehaviour.cs`, ngay dưới dòng 134 đang boot Leaderboard, thêm:

```csharp
        try { Center.GetOrCreate<ClanService>(); } catch (Exception e) { Debug.LogException(e); }
```

- [ ] **Step 6: Đổi mọi lời gọi cũ**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rn "ClanManager\.\|ClanEventManager\." Assets/_Game/Shared/Modules/Clan --include='*.cs'
```

Sửa từng chỗ: `ClanManager.ConnectToServer()` thành `Center.GetOrCreate<ClanService>().Connected`, `ClanManager.Unlock()` thành `.Unlocked`, `ClanEventManager.onJoinClanAction += X` thành `service.OnClanChanged += X`, và tương tự.

- [ ] **Step 7: Xoá hai file cũ**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm "Assets/_Game/Shared/Modules/Clan/Scripts/ClanManager.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/ClanManager.cs.meta" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/ClanEventManager.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/ClanEventManager.cs.meta"
```

- [ ] **Step 8: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi.

- [ ] **Step 9: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan Assets/_Game/Shared/Scripts/StartBehaviour.cs
git commit -m "refactor(clan): gop ClanManager va ClanEventManager thanh ClanService

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: Bỏ IClanCustom

**Files:**
- Delete: `Scripts/IClanCustom.cs`
- Modify: mọi file gọi qua `_custom`

**Interfaces:**
- Consumes: `ClanService` (Task 5), ba popup (Task 4)
- Produces: `ClanService.ShowPlayerInfo(int playerCode)`

- [ ] **Step 1: Rải 7 hàm popup**

```csharp
// ShowClanInfoPopup()  →
UIWrapper.OpenPopup("UIPopupClanInfo", popup => popup.GetComponent<UIPopupClanInfo>().Bind(data));
// CloseClanInfoPopup() →
UIWrapper.ClosePopup("UIPopupClanInfo");
// ShowChooseClanLogoPopup() →
UIWrapper.OpenPopup("UIPopupChooseLogo", popup => popup.GetComponent<UIPopupChooseLogo>().Bind(OnPickLogo));
// ShowHelpResourcePopup() →
UIWrapper.OpenPopup("UIPopupHelpResource", popup => popup.GetComponent<UIPopupHelpResource>().Bind());
```

`ShowPlayerInfoPopup(int player_code)` thành hàm trên service:

```csharp
        /// <summary>Mở popup profile của người chơi khác.</summary>
        public void ShowPlayerInfo(int playerCode) =>
            UIWrapper.OpenPopup("PopupProfile", popup => { /* Profile tự đọc code từ event */ });
```

- [ ] **Step 2: Rải 5 hàm toast**

```csharp
// ShowToast(s)                        → service.Toast(s);
// ShowToastSuccess()                  → service.Toast("clan_toast_success");
// ShowToastFailed()                   → service.Toast("clan_toast_failed");
// ShowToastTimeout()                  → service.Toast("clan_toast_timeout");
// ShowToastWaitAMomentAfterOnClick()  → service.Toast("clan_toast_wait_a_moment");
```

Bốn khoá localize mới phải thêm vào bảng ngôn ngữ. Nếu dự án chưa có, dùng đúng chuỗi trên làm key và để bản dịch trống, `ULocManager.GetLocalizedString` trả về chính key nên không vỡ.

- [ ] **Step 3: Rải 3 hàm danh tính và level**

```csharp
// YourPlayerCode()   → AccountManager.Instance.Code
// YourCurrentLevel() → GameRequest<int>.Request(GameKeys.GET_LEVEL)
// Unlock()           → Center.GetOrCreate<ClanService>().Unlocked
```

- [ ] **Step 4: Rải 2 hàm avatar và name style qua EventBus**

Thêm hai khoá trên service:

```csharp
        public const string EVENT_BIND_AVATAR = "falcon.modules.clan.bind_avatar";
        public const string EVENT_BIND_NAME_STYLE = "falcon.modules.clan.bind_name_style";
```

Chỗ cũ gọi `_custom.OnSetAvatarUI(go, data)` đổi thành:

```csharp
GameEvent<(GameObject, int)>.Emit(ClanService.EVENT_BIND_AVATAR, (avatarObject, data.player_code));
```

Module Profile lắng nghe khoá này và tự dựng bằng `UIProfile_Avatar`. Việc nối phía Profile nằm ngoài phạm vi plan này; ghi TODO và để module hiển thị avatar mặc định khi chưa ai nghe.

- [ ] **Step 5: Tìm API tiêu tài nguyên của game**

Sáu hàm kinh tế cần một API trừ và cộng tài nguyên. Plan này **chưa chốt được** tên hàm đó, `AccountManager` chỉ lộ ra `Code` ở dòng 425. Chạy đúng ba lệnh sau để tìm, đừng đoán:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
# 1. Module Shop mua đồ bằng gì
grep -rnE "gold|coin|price|cost" Assets/Falcon/Modules/Shop/Scripts/Runtime --include='*.cs' -i | head -20
# 2. Module Packs cộng thưởng bằng gì
grep -rnE "Grant|Reward|Receive|Add" Assets/_Game/OutGame/Packs --include='*.cs' | head -20
# 3. RewardFlow phát thưởng bằng gì
grep -rnE "public .*(void|bool|int) " Assets/_Game/Shared/Modules/RewardFlow --include='*.cs' | head -20
```

Quy tắc quyết định:

- Nếu tìm thấy một API dùng chung, ví dụ `ResourceService.Add(id, amount)` và `.TrySpend(id, amount)`, thì gọi thẳng nó và thêm asmdef tham chiếu tương ứng vào `Falcon.Shared.Clan`. Ghi rõ tham chiếu thứ 11 này vào spec.
- Nếu **không** có API dùng chung, giữ đúng kiểu giao tiếp của plan: khai hai khoá EventBus trên `ClanService` là `"falcon.modules.clan.spend"` với payload `(int amount, Action<bool> reply)` và `"falcon.modules.clan.grant"` với payload `(int amount)`, rồi để phần game nối vào. Không thêm tham chiếu asmdef nào.

Dừng lại báo chủ repo biết đã chọn nhánh nào trước khi viết tiếp.

- [ ] **Step 5b: Chuyển 2 hàm còn lại**

`ClanInfoAvailable<T>` chuyển thành hàm private trong `ClanService`, bỏ generic vì chỉ dùng với `ClanEditingData`. `GetMessagePrefabName` chuyển thành hàm private trong `ChatRoomPanel`.

- [ ] **Step 6: Xoá interface**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm "Assets/_Game/Shared/Modules/Clan/Scripts/IClanCustom.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/IClanCustom.cs.meta"
grep -rn "IClanCustom\|_custom" Assets/_Game/Shared/Modules/Clan --include='*.cs'
```

Expected: lệnh grep không in gì.

- [ ] **Step 7: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi.

- [ ] **Step 8: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "refactor(clan): bo IClanCustom, rai 27 ham ve dung cho

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: Bỏ pattern _Virtual

23 file. Đọc `<scratchpad>/behaviour-prefabs.txt` từ Task 0 Step 4 trước khi bắt đầu.

**Files:**
- Modify: 23 file có `_Virtual`, trong đó `ClanPanelManager.cs` đổi tên thành `UIClanPanel.cs`

**Interfaces:**
- Produces: mọi MonoBehaviour Clan có logic viết thẳng, không còn field `_behaviour`

- [ ] **Step 1: Xử lý prefab có gán subclass**

Với mỗi prefab trong `behaviour-prefabs.txt` mà field `_behaviour` có `type` khác rỗng: mở file `.cs` của subclass đó, chép logic override vào class chính, rồi mới sang Step 2. Bỏ qua bước này là mất logic im lặng.

- [ ] **Step 2: Kéo logic lên, từng file một**

Khuôn chuyển, ví dụ `ClanLogoUI`:

```csharp
// trước
public class ClanLogoUI : MonoBehaviour
{
    [SerializeReference] private ClanLogoUI_Virtual _behaviour = new();
    private void Awake() => Behaviour.Awake();
    public void Init(int id) => Behaviour.Init(id);
    [Serializable] public class ClanLogoUI_Virtual { /* logic ở đây */ }
}

// sau
public class ClanLogoUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    public Action onClickAction;

    private void Awake() { /* logic cũ của _Virtual.Awake */ }
    public void Init(int id) { /* logic cũ của _Virtual.Init */ }
}
```

Bỏ luôn những hàm vòng đời rỗng. Nếu `_Virtual.Update()` là thân rỗng thì không viết `Update()` nữa, Unity đỡ gọi thừa.

- [ ] **Step 3: Đổi ClanPanelManager thành UIClanPanel**

```bash
cd "D:/UnityProjects/TemplatePuzzle/Assets/_Game/Shared/Modules/Clan/Scripts"
git mv ClanPanelManager.cs UIClanPanel.cs
git mv ClanPanelManager.cs.meta UIClanPanel.cs.meta
```

Đổi tên class trong file, và bỏ ba thứ chết theo `_Virtual`:

```csharp
namespace Game.Shared.Clan
{
    /// <summary>Chọn một trong bốn panel gốc theo trạng thái clan.</summary>
    public class UIClanPanel : MonoBehaviour
    {
        [SerializeField] private GameObject _noClanPanel;
        [SerializeField] private GameObject _inClanPanel;
        [SerializeField] private GameObject _lockPanel;
        [SerializeField] private GameObject _noInternetPanel;

        private ClanService _service;

        private void OnEnable()
        {
            _service = Center.GetOrCreate<ClanService>();
            _service.OnConnectionChanged += Refresh;
            _service.OnClanChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            _service.OnConnectionChanged -= Refresh;
            _service.OnClanChanged -= Refresh;
        }

        private void Refresh()
        {
            SetActiveSafe(_lockPanel, false);
            SetActiveSafe(_noInternetPanel, false);
            SetActiveSafe(_noClanPanel, false);
            SetActiveSafe(_inClanPanel, false);

            if (!_service.Unlocked) { SetActiveSafe(_lockPanel, true); return; }
            if (!_service.Connected) { SetActiveSafe(_noInternetPanel, true); return; }

            SetActiveSafe(_noClanPanel, _service.MyClanCode == 0);
            SetActiveSafe(_inClanPanel, _service.MyClanCode > 0);
        }

        private static void SetActiveSafe(GameObject go, bool active)
        {
            if (go == null) return;
            if (go.activeSelf != active) go.SetActive(active);
        }
    }
}
```

`canShowClan`, `IEWaitAndShowClan` và `DoSomethingBeforeShowClan` biến mất ở đây.

- [ ] **Step 4: Kiểm sạch**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rn "_Virtual\|BindOwner\|canShowClan" Assets/_Game/Shared/Modules/Clan --include='*.cs'
```

Expected: không in gì.

- [ ] **Step 5: Gán lại field trên prefab**

Sau reimport, prefab mất field `_behaviour`. Mở từng prefab trong `behaviour-prefabs.txt`, kiểm mọi field `[SerializeField]` của class chính đã có tham chiếu, gán lại chỗ nào trống.

- [ ] **Step 6: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi. Kiểm thêm số dòng đã cắt:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
find Assets/_Game/Shared/Modules/Clan/Scripts -name '*.cs' -exec cat {} \; | wc -l
```

Expected: dưới 5800 dòng, so với 6509 dòng ban đầu.

- [ ] **Step 7: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "refactor(clan): bo pattern _Virtual tren 23 file, ClanPanelManager thanh UIClanPanel

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 8: UITab_Parent thay ClanToggle

**Files:**
- Delete: `Scripts/ClanToggle.cs`
- Modify: `Scripts/Panel/NoClanPanel.cs`
- Modify: `Prefabs/ClanManagerPanel.prefab`

**Interfaces:**
- Consumes: `NoClanPanelTab` enum có sẵn trong `NoClanPanel.cs`
- Produces: `NoClanPanel.SelectTab(NoClanPanelTab type)` giữ nguyên chữ ký

- [ ] **Step 1: Thêm field UITab_Parent vào NoClanPanel**

Theo mẫu `UILeaderboardPanel.cs:41-42`:

```csharp
using Falcon.Shared.Tab;

namespace Game.Shared.Clan
{
    public class NoClanPanel : MonoBehaviour
    {
        [SerializeField] private UITab_Parent _tabs;
        [SerializeField] private GameObject _listClanPanel;
        [SerializeField] private GameObject _searchClanPanel;
        [SerializeField] private GameObject _createClanPanel;

        public void SelectTab(NoClanPanelTab type) => _tabs.Active((int)type);
    }
}
```

`NoClanPanelTab` phải khai đúng thứ tự khớp `lsChild` trên prefab: `ListClan = 0, SearchClan = 1, CreateClan = 2`.

- [ ] **Step 2: Sửa prefab**

Trên `ClanManagerPanel.prefab`: gỡ 3 component `ClanToggle`, thêm `UITab_Parent` lên object cha của ba tab, gán `lsButton` là ba Button, `lsChild` là ba `UITab_Child` đặt trên ba panel con theo đúng thứ tự enum.

- [ ] **Step 3: Xoá ClanToggle**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm "Assets/_Game/Shared/Modules/Clan/Scripts/ClanToggle.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/ClanToggle.cs.meta"
grep -rl "b7afa4d698704d8e87fa2110f54bbc82" Assets --include='*.prefab' | grep -v FR2_Cache
```

Expected: lệnh grep không in gì.

- [ ] **Step 4: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi.

- [ ] **Step 5: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "refactor(clan): dung UITab_Parent thay ClanToggle

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 9: ClanCountdown thay CountdownTmpBasic

**Files:**
- Create: `Scripts/UI/ClanCountdown.cs`
- Delete: `Scripts/UI/CountdownTmpBasic.cs`
- Modify: `Scripts/ChatRoom/ChatBoxInClan.cs`
- Modify: `Prefabs/Panels/InClanPanel.prefab`

**Interfaces:**
- Produces: `ClanCountdown.Begin(string key, long endSecond)`, `ClanCountdown.Stop()`

- [ ] **Step 1: Viết ClanCountdown theo mẫu LbCountdown**

```csharp
using Falcon.Shared.Common.Time;
using TMPro;
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Đếm ngược cooldown xin trợ giúp, chạy nhờ WrapperTime nên không tốn Update riêng.</summary>
    public class ClanCountdown : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;
        [SerializeField] private GameObject _root;

        private string _key;

        /// <summary>endSecond là giây unix; 0 hoặc đã qua thì ẩn luôn.</summary>
        public void Begin(string key, long endSecond)
        {
            Stop();

            if (endSecond <= WrapperTime.CurrentSecond)
            {
                SetVisible(false);
                return;
            }

            _key = key;
            SetVisible(true);
            WrapperTime.AddTick(_key, endSecond, OnTick, OnEnd);
        }

        public void Stop()
        {
            if (_key == null) return;

            // RemoveAction chứ không RemoveTick: schedule dùng chung key giữa các lần mở
            WrapperTime.RemoveAction(_key, OnTick);
            _key = null;
        }

        private void OnDisable() => Stop();

        private void OnTick(long remain)
        {
            if (_text != null) _text.text = remain.ToTime();
        }

        private void OnEnd()
        {
            _key = null;
            SetVisible(false);
        }

        private void SetVisible(bool on)
        {
            var go = _root != null ? _root : gameObject;
            if (go.activeSelf != on) go.SetActive(on);
        }
    }
}
```

- [ ] **Step 2: Đổi lời gọi trong ChatBoxInClan**

Tìm chỗ dùng `CountdownTmpBasic` và đổi sang `ClanCountdown.Begin(key, endSecond)`. Key nên là `"clan_help_cooldown"` để không đụng key của module khác.

- [ ] **Step 3: Sửa prefab InClanPanel**

Gỡ component `CountdownTmpBasic`, thêm `ClanCountdown`, gán `_text` và `_root`.

- [ ] **Step 4: Xoá file cũ**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm "Assets/_Game/Shared/Modules/Clan/Scripts/UI/CountdownTmpBasic.cs" \
       "Assets/_Game/Shared/Modules/Clan/Scripts/UI/CountdownTmpBasic.cs.meta"
grep -rl "a995216d588e4fed8656849b62e25de3" Assets --include='*.prefab' | grep -v FR2_Cache
```

Expected: lệnh grep không in gì.

- [ ] **Step 5: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi.

- [ ] **Step 6: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "refactor(clan): ClanCountdown theo mau LbCountdown

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 10: SO_ClanLogoDatabase

**Files:**
- Create: `Scripts/ClanLogoDatabase.cs`
- Create: `SO_ClanLogoDatabase.asset`
- Create: `SA_Clan.spriteatlasv2`
- Modify: `Scripts/UI/ClanLogoUI.cs`
- Modify: `Scripts/Popup/UIPopupChooseLogo.cs`

**Interfaces:**
- Consumes: `UIPopupChooseLogo.Build()` bỏ trống ở Task 4
- Produces: `ClanLogoDatabase.Instance`, `Sprite ClanLogoDatabase.GetById(int id)`, `int ClanLogoDatabase.Count`

- [ ] **Step 1: Viết ClanLogoDatabase**

```csharp
using UnityEngine;

namespace Game.Shared.Clan
{
    /// <summary>Bảng tra sprite logo clan theo id, nạp từ Resources.</summary>
    [CreateAssetMenu(fileName = "SO_ClanLogoDatabase", menuName = "Falcon/Clan/Logo Database")]
    public class ClanLogoDatabase : ScriptableObject
    {
        [SerializeField] private Sprite[] _logos;

        private static ClanLogoDatabase _instance;

        public static ClanLogoDatabase Instance =>
            _instance ??= Resources.Load<ClanLogoDatabase>("SO_ClanLogoDatabase");

        public int Count => _logos?.Length ?? 0;

        /// <summary>id ngoài khoảng thì trả null, gọi bên ngoài tự lo.</summary>
        public Sprite GetById(int id) =>
            _logos != null && id >= 0 && id < _logos.Length ? _logos[id] : null;
    }
}
```

- [ ] **Step 2: Tạo asset và gán 86 sprite**

Trong Unity: `Assets > Create > Falcon > Clan > Logo Database`, lưu vào `Assets/_Game/Shared/Modules/Clan/Resources/SO_ClanLogoDatabase.asset`. Kéo 86 sprite trong `Clan/Sprites` có tên dạng `Property 1=icon_teamN` và `flag_teamN` vào mảng `_logos`, xếp theo số N để id khớp thứ tự cũ.

- [ ] **Step 3: Tạo atlas**

`Assets > Create > 2D > Sprite Atlas V2`, tên `SA_Clan`, đặt tại gốc module, kéo thư mục `Sprites` vào `Objects for Packing`. Theo mẫu `SA_Profile` và `SA_Reward`.

- [ ] **Step 4: Sửa ClanLogoUI**

```csharp
        public void Init(int id)
        {
            var sprite = ClanLogoDatabase.Instance != null
                ? ClanLogoDatabase.Instance.GetById(id)
                : null;

            if (sprite == null) Debug.LogWarning($"[Clan] không có logo id {id}.");
            _icon.sprite = sprite;
        }
```

- [ ] **Step 5: Hoàn thiện UIPopupChooseLogo.Build**

```csharp
        private void Build()
        {
            var db = ClanLogoDatabase.Instance;
            if (db == null) { Debug.LogWarning("[Clan] chưa có logo database."); return; }

            for (var i = 0; i < db.Count; i++)
            {
                var item = Instantiate(_itemPrefab, _content);
                var id = i;
                item.Init(id);
                item.SetButton(() => OnPick(id));
            }
        }
```

- [ ] **Step 6: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi. Kiểm thêm database đã đủ:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -c "guid:" "Assets/_Game/Shared/Modules/Clan/Resources/SO_ClanLogoDatabase.asset"
```

Expected: 87 trở lên, 86 sprite cộng 1 dòng `m_Script`.

- [ ] **Step 7: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game/Shared/Modules/Clan
git commit -m "feat(clan): SO_ClanLogoDatabase thay ClanLogoRsDemo

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 11: Đổi component

`UIScaleOnDown` 48 chỗ trên 16 prefab, `SafeAreaPortrait` 10 chỗ trên 5 prefab. Hai component này nằm trong Kit sắp xoá.

**Files:**
- Modify: 16 prefab có `UIScaleOnDown`, 5 prefab có `SafeAreaPortrait`

**Interfaces:**
- Produces: không còn GUID `4891211c3e4e44878886aeb6361493e4` và `8df451a16c634de298d24802c31f50b0` trong project

- [ ] **Step 1: Đổi GUID SafeAreaPortrait sang SafeArea**

Hai component có cùng vai và không có field bắt buộc nào khác nhau, đổi GUID thẳng được:

```bash
cd "D:/UnityProjects/TemplatePuzzle"
OLD=8df451a16c634de298d24802c31f50b0
NEW=3af6cd6274044bd41bb8dcba6f3e5bb7
grep -rl "$OLD" Assets --include='*.prefab' --include='*.unity' \
  | while IFS= read -r f; do sed -i "s/$OLD/$NEW/g" "$f"; echo "patched: $f"; done
```

Expected: 5 dòng `patched:`.

- [ ] **Step 2: Đổi UIScaleOnDown sang UIButtonExtension trong Unity**

Hai component **không** cùng cấu trúc serialize: `UIScaleOnDown` có `_scaleFactor`, `_scaleDuration`, `_soundPlayer`, `_hapticPlayer`, `_onClick`; `UIButtonExtension` có `target`, `ratioOffsetX`, `ratioOffsetY` và yêu cầu có `Button` cùng object. Không đổi GUID thẳng được.

Với mỗi trong 48 chỗ: ghi lại `_onClick` đang nối vào đâu, Remove Component `UIScaleOnDown`, bảo đảm object có `Button`, Add Component `UIButtonExtension`, đặt `ratioOffsetX` và `ratioOffsetY` bằng `_scaleFactor` cũ, nối lại `onClick` vào `Button.onClick`.

Danh sách 16 prefab và số chỗ:

| Prefab | Số chỗ |
|---|---|
| ClanManagerPanel | 7 |
| ClanEditPanel | 7 |
| CreateClanPanel | 6 |
| ClanMemberRow | 5 |
| ClanInfoPopup | 4 |
| ClanLeaveConfirmPanel | 3 |
| ClanRemoveConfirmPanel | 3 |
| InClanPanel | 3 |
| JoinRequestPrefab | 2 |
| UILeaderboard | 2 |
| AvatarUIClan_Anchor | 1 |
| ChooseLogoPopup | 1 |
| ClanRowPrefab | 1 |
| HelpResourcePopup | 1 |
| OtherHelpMessagePrefab | 1 |
| UIPopupNoInternet | 1 |

`UIPopupNoInternet.prefab` thuộc module UINoInternet sắp xoá ở Task 12, bỏ qua.

- [ ] **Step 3: Kiểm hết sạch**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
grep -rl "4891211c3e4e44878886aeb6361493e4\|8df451a16c634de298d24802c31f50b0" Assets \
  | grep -v FR2_Cache | grep -v "/UIModular/"
```

Expected: không in gì.

- [ ] **Step 4: Nghe lại tiếng nút**

`UIScaleOnDown` phát âm thanh và haptic, `UIButtonExtension` thì không. Vào Play mode, bấm thử nút trên `UILeaderboard` và một panel Clan. Nếu mất tiếng mà chủ repo muốn giữ thì dừng lại hỏi, đừng tự sửa `UIButtonExtension` vì đó là file dùng chung.

- [ ] **Step 5: Chạy kiểm chứng**

Run: `bash <scratchpad>/clan-check.sh`
Expected: ba con số không xấu đi.

- [ ] **Step 6: Commit (chờ duyệt)**

```bash
git add -A Assets/_Game Assets/Falcon
git commit -m "refactor: doi UIScaleOnDown sang UIButtonExtension, SafeAreaPortrait sang SafeArea

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 12: Xoá UIModular

**Files:**
- Move: `FalconAssets/Modules/UIModular/UIClan/Addressable/UIClan_ComingSoon.prefab` sang `Clan/Prefabs/`
- Delete: `Assets/Falcon/Modules/UIModular/`, `Assets/FalconAssets/Modules/UIModular/`
- Delete: vỏ rỗng `Assets/Falcon/Modules/Clan/`, `Assets/FalconAssets/Modules/Clan/`

**Interfaces:**
- Consumes: mọi task trên đã xong
- Produces: project không còn `UIModular`

- [ ] **Step 1: Cứu UIClan_ComingSoon trước**

Prefab này là `featuresTab_4` của menu Home. Xoá là mất tab.

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git mv "Assets/FalconAssets/Modules/UIModular/UIClan/Addressable/UIClan_ComingSoon.prefab" \
       "Assets/_Game/Shared/Modules/Clan/Prefabs/"
git mv "Assets/FalconAssets/Modules/UIModular/UIClan/Addressable/UIClan_ComingSoon.prefab.meta" \
       "Assets/_Game/Shared/Modules/Clan/Prefabs/"
grep -n "3e33562a0c7f72b4ca18c7e2a6386031" Assets/AddressableAssetsData/AssetGroups/Common.asset
```

Expected: vẫn còn dòng đó. GUID không đổi khi move nên addressable entry vẫn đúng.

- [ ] **Step 2: Xoá hai cây UIModular**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm -r "Assets/Falcon/Modules/UIModular" "Assets/Falcon/Modules/UIModular.meta" \
          "Assets/FalconAssets/Modules/UIModular" "Assets/FalconAssets/Modules/UIModular.meta"
```

- [ ] **Step 3: Xoá vỏ rỗng**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
git rm -r "Assets/Falcon/Modules/Clan" "Assets/Falcon/Modules/Clan.meta" 2>/dev/null
git rm -r "Assets/FalconAssets/Modules/Clan" "Assets/FalconAssets/Modules/Clan.meta" 2>/dev/null
find Assets/Falcon Assets/FalconAssets -type d -empty -print
```

Nếu `find` in ra thư mục nào thì xoá cả nó và file `.meta` tương ứng.

- [ ] **Step 4: Kiểm không còn tham chiếu**

```bash
cd "D:/UnityProjects/TemplatePuzzle"
ls -d Assets/Falcon/Modules/UIModular Assets/FalconAssets/Modules/UIModular 2>&1 | head -2
grep -rn "Falcon.Modules.UIModular" Assets --include='*.cs' --include='*.asmdef'
```

Expected: hai lệnh đều báo không tồn tại hoặc không in gì.

- [ ] **Step 5: Kiểm tab 4 vẫn nạp được**

Vào Play mode, mở màn Home, bấm sang tab 4. Phải thấy màn hình Coming Soon của Clan chứ không phải tab trống.

- [ ] **Step 6: Chạy kiểm chứng lần cuối**

Run: `bash <scratchpad>/clan-check.sh`
Expected: `COMPILE="errorCount":0`, `MISSING_SCRIPTS="totalFound":0`, `BROKEN_GUIDS` bằng hoặc thấp hơn 24.

- [ ] **Step 7: Commit (chờ duyệt)**

```bash
git add -A Assets
git commit -m "refactor: xoa Falcon/Modules/UIModular va FalconAssets/Modules/UIModular

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```
