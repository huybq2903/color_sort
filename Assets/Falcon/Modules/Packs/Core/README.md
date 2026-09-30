# Falcon Packs Core Module

## 1. Mô tả

Module quản lý hệ thống gói inapp trong game, hỗ trợ:
- Đồng bộ client/server qua `SCAllPacksData`
- Mở rộng dễ dàng theo từng nhóm (`Wrapper`)
- Quản lý UI, local save, sync server, CMS config
- Có Debug Window để xem các chỉ số

Mỗi nhóm pack có `config`, `userData`, và `wrapper` riêng, được tự động đăng ký tại runtime.

Các thành phần chính:
- `PacksManager`: Quản lý tất cả các wrapper nhóm pack đang có, lưu config và userData
- `ABaseWrapperPack`: Lớp wrapper để quản lý từng nhóm pack, có `ABaseElementPackConfig` và `ABasePackUserData` tương ứng
- `WrapperPacksCommon`: Lớp wrapper cơ bản kế thừa từ `ABaseWrapperPack`, dùng cho khác module khác nếu muốn config pack trên cms và lấy thông tin reward config nếu không có config đặc biệt.
- `ABaseElementPackConfig`: Lớp config cơ sở cho một phẩn tử trong nhóm pack, là class abstract
- `ABaseElementPackConfig`: config cơ bản kế thừa `ABaseElementPackConfig` để dùng khi không có config đặc biệt
- `ABasePackUserData`: Lớp data cơ sở để lưu dữ liệu người dùng
- `NullPackUserData`: data kế thừa `ABasePackUserData` dành cho những nhóm pack không yêu cầu lưu dữ liệu người dùng
- `DoAllTheSameThingOneTime`: Lớp tiện ích đảm bảo là 1 hành động (ứng với 1 key) chỉ được thực hiện 1 lần khi bị gọi lặp lại để tối ưu hiệu năng, như việc gửi data lên server...
- `UIItemReward`: UI Reward trong pack, gồm 2 thành phần Icon và Text Amount
- `APackElement`: Lớp UI cơ sở cho pack, tích hợp sẵn việc setup reward trên UI Pack, in giá tiền và callback khi mua pack, cần kế thừa class này để tạo UI Pack riêng
---

## 2. Quick Start
### (Tùy chọn):
Nếu dùng module `Account` thì tải thêm module `Account4EventBus` để khi Login thành công sẽ tự động gửi CSGetAllPacksData lên
### - Nếu muốn tạo 1 nhóm pack mới
1. Tạo folder riêng
2. Tạo `SO_FCM_MyGroup_Config` từ menu: `Assets > Create > Pack > PacksConfig`
3. Tạo `RewardNameRegistry` từ menu: `Assets > Create > Pack > Reward Name Registry` và điền tên reward
4. Tạo config:
```csharp
public class MyElementConfig : ABaseElementPackConfig { ... }
```
5. (Tuỳ chọn) Tạo userData:
```csharp
public class MyUserData : ABasePackUserData { ... }
```
6. Tạo wrapper:

Class Wrapper cần phải để trong namespace có tên là Falcon.{NAME_SPACE}, ví dụ

```csharp
namespace Falcon.Modules.Packs.MyGroup.Runtime
{
    public class WrapperMyGroup : ABaseWrapperPack<MyElementConfig, MyUserData> 
    {
        public override string Key => "MyGroup";
    }
}

```

Cần trao đổi với server để tạo data tương ứng

### - Muốn tạo nhanh 1 pack cơ bản: sử dụng WrapperPackCommon có sẵn
1. Lên cms điền sẵn 1 pack trong Group Common
2. Sử dụng hàm `PackManager.GetConfigCommon(string idPack)` để lấy ra thông tin của pack đó gửi về từ server


---

## 3. Danh sách API

### `PackManager`
- `Action onInitialize`: callback khi khởi tạo PackManager, cho phép bên ngoài gán lại callback để custom
- `T Get<T>() where T : class, IWrapperPack`: Lấy wrapper pack hiện có của loại T, hoặc null nếu không tìm thấy.
- `IWrapperPack GetOrCreate(Type type)`: Lấy wrapper pack hiện có theo Type, hoặc tạo và khởi tạo nó nếu chưa tồn tại.
- `void Remove<T>() where T : class, IWrapperPack`: Xóa wrapper pack đã đăng ký của loại T khỏi danh sách đăng ký.
- `ABaseElementPackConfig GetConfigCommon(string idPack)`: Lấy gói config cơ bản được cấu hình nhanh trên cms ở group common, dành cho các module khác muốn tạo nhanh gói bán

### `FormatHelper`
- `Func<(string name, int amount, string data), string> FormatQuantityReward`: callback format lại cách hiển thị số lượng reward (ví dụ reward unlimited lives có amount là 3600 nhưng phải hiển thị là 1h)
Gán lại callback này để custom
```csharp
FormatHelper.FormatQuantityReward = reward =>
{
    return reward.name switch
    {
        "gold" => $"{reward.amount.FormatWithSpace()}",
        "unlimited_live" => $"{reward.amount.FormatSeconds()}",
        _ => $"x{reward.amount}"
    };
};
```

### `PlacementResolver`: 
Quản lý việc xác định placement mua pack, có thể đăng kí thêm để custom
- `Action AddRule(Func<string> rule)`: Thêm 1 rule xác định placement, trả về hàm để remove rule đó đi
```csharp
        var dispose = PlacementResolver.AddRule(() =>
        {
            if (LevelManager.GetCurrentScene() == "Home") return "ShopHome";
            if (LevelManager.GetCurrentScene() == "GamePlay") return "ShopInGame";
            return null;
        });
        
        OnDestroy()
        {
            dispose?.Invoke(); //gọi hàm này để remove rule đi
        }
```
- `string Get() `: Lấy placement hiện tại sau khi lọc qua các rule được thêm

### `Event Bus`
```csharp
//Đăng kí sự kiện khi user data D thay đổi với D : ABasePackUserData
GameEvent<D>.Register("falcon.modules.packs.on_change_data", OnChangeUserData);

void OnChangeUserData(D userData) {}
```

```csharp
//Đăng kí sự kiện khi mua gói thành công
GameEvent<string>.Register("falcon.modules.packs.buy_success", OnBuyPackSuccess);

void OnBuyPackSuccess(string idPack) {}
```

```csharp
//Đăng kí sự kiện khi nhận phần thưởng
GameEvent<(string name, int amount, string data)[]>.Register("falcon.modules.packs.add_reward", OnClaimReward);
```
---

## 4. Chi tiết


### Wrapper (`ABaseWrapperPack`)

| Hàm | Mô tả |
|-----|------|
| `OnInitialize()` | Khởi tạo wrapper |
| `InitializeConfig()` | Load config từ local hoặc `SO` |
| `InitializeData()` | Load userData hoặc tạo mới |
| `AfterGetData()` | Gọi sau khi sync xong |
| `SendToServer()` | Gửi userData bằng `CSUpdatePackUserData` |
| `SaveOnClient()` | Lưu local `UserData` |
| `OnChangeData()` | Emit `falcon.modules.packs.on_change_data` |
| `AddSequenceAndSave()` | Tăng sequence, save local |
| `SaveAndSend()` | Gộp save/send, debounce bằng `DoAllTheSameThingOneTime` |
| `UpdateParam(string key, string value)` | Update config thêm
### `IWrapperPack`

| Property                           | Mô tả                     |
|------------------------------------|---------------------------|
| `string Key`                       | idGroup của wrapper       |
| `PacksConfig Config`               | Danh sách pack            |
| `SOPacksConfig LocalConfig`        | SO config từ `Resources`  |
| `Dictionary<string, string> Param` | config thêm cho nhóm pack |

- `ABaseWrapperPack` kế thừa từ `IWrapperPack`

### `ABaseElementPackConfig` và `ABaseElementPackConfig`

| Property | Mô tả |
|----------|------|
| `string idPack` | ID duy nhất định danh pack |
| `string productId` | ID sản phẩm, dùng cho giao dịch InApp Purchase |
| `Reward[] rewards` | Danh sách phần thưởng người chơi sẽ nhận khi mua pack |

### `Reward`
| Property | Mô tả                                                                                               |
|----------|-----------------------------------------------------------------------------------------------------|
| `string name` | Tên phần thưởng                                                                                     |
| `int amount` | Số lượng phần thưởng (nếu là phần thưởng có tác dụng trong một khoảng thời gian thì đơn vị là giây) |
| `string data` | Dữ liệu phụ đi kèm phần thưởng (tùy biến theo ngữ cảnh)                                      |
---

## 5. Các lỗi thường gặp

| Lỗi                            | Nguyên nhân                                    | Cách khắc phục |
|--------------------------------|------------------------------------------------|----------------|
| Không hiển thị pack trong shop | Pack không trong config của shop               |
| Wrapper không chạy             | Không có local config và không có trong server |

---