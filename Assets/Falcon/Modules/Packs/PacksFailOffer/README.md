# Falcon Modules Packs Fail Offer

## 1. Mô tả

`PacksFailOffer` là module quản lý các gói ưu đãi (offer) hiển thị khi người chơi thua mở rộng từ hệ thống `Falcon Packs Core Module`.  
Bao gồm hệ thống hiển thị gói theo điều kiện cooldown, giới hạn mua, thứ tự ưu tiên, và đồng bộ server.  
Kết hợp với `UILosePromote` để hiển thị shop lose game.
Module này giúp tăng tỉ lệ chuyển đổi người chơi sau khi thua bằng các gói offer giới hạn thời gian, có thể mở rộng logic theo hành vi người dùng hoặc level.

Các thành phần chính:
- `UIBannerGroupPack`: class helper dùng cho các group pack có scroll kéo ngang, sử dụng Simple Scroll Snap
- `WrapperPacksFailOffer`: wrapper quản lý các gói
- `FailOfferElementPackConfig`: lớp config gói
- `FailOfferUserData`: lớp user data
- `UIPackFailOfferElement`: thành phần UI hiển thị gói, kế thừa từ APackElement
- `LosePromoteManager`: Class quản lý, lưu config, asset các pack sẽ xuất hiện trong shop lose game
- `UILosePromote`: UI tạo ra các gói để hiển thị theo config
### Shop Lose Game ngoài chứa các gói Fail Offer thì có thể chứa các gói khác tùy theo nhu cầu của game

---

## 2. Quick Start
### Bước 0 (Tùy chọn):
Nếu dùng module `Account` thì tải thêm module `Account4EventBus` để khi Login thành công sẽ tự động gửi CSGetLosePromoteConfig lên
### Bước 1: Import asset
- Unity menu: `Falcon > Modules > Pack > Import Pack Fail Offer Asset`
- Trong asset bao gồm:
  - UIBanner_LosePromote: prefab ui shop lose game, các gói sẽ được sinh ra ở đây
  - UIPack_FailOffer: prefab dùng chung cho các gói fail offer
  - SO_FCM_LosePromote_Config: SO config mặc định các gói sẽ hiện trong shop lose game
  - SO_FCM_PacksFailOffer_Config: SO config mặc định các gói fail offer

### Bước 2:
- Thêm các pack fail offer vào cms trong Group Fail Offer

### Bước 3: Cho các Module khác muốn thêm Pack vào shop lose promote

1. Đăng kí pack:
```csharp
GameEvent<(AssetReference, string)>.Emit("falcon.modules.shop.create_pack_shop", (new AssetReference(address), idPack));
```
2. Muốn ẩn pack (khi gói hết hiệu lực, vv...):
```csharp
GameEvent<string>.Emit("falcon.modules.shop.hide_pack_shop", idPack);
```
AssetReference là một wrapper metadata cho asset trong hệ thống Addressables, gọi `new AssetReference(string address)` sẽ trỏ tới asset có địa chỉ đó trong addressable
- Shop sẽ đăng kí sự kiện, cache lại AssetReference và khởi tạo UI từ cache đó khi được gọi
3. Config trên cms ở ShopLoseGame

---

## 3. Danh sách API

### `LosePromoteManager`
- `Action onInitialize`: callback khi khởi tạo `LosePromoteManager`, cho phép bên ngoài gán lại callback để custom
### `Event Bus`
```csharp
  //Đăng kí sự kiện để tiếp tục game nếu mua gói
  GameEvent.Register("falcon.modules.shoplosegame.continue", OnContinue);
  
  void OnContinue() {}
```

## 4. Chi tiết

### Trong Config của Shop lose game bao gồm List `LosePromoteConfig`

```csharp
    public class LosePromoteConfig
    {
        /// <summary>
        /// Mã duy nhất của pack
        /// </summary>
        public string idPack;
        /// <summary>
        /// Độ ưu tiên, theo thứ tự từ trái sang phải
        /// </summary>
        public int priority;
        /// <summary>
        /// Có cho chơi tiếp game nếu mua xong không? 0: không, 1: có
        /// </summary>
        public int continueGame;
    }
```
FailOfferElementPackConfig
```csharp
    /// <summary>
    /// Config của 1 pack fail offer
    /// </summary>
    public class FailOfferElementPackConfig : ABaseElementPackConfig
    {
        /// <summary>
        /// level mở khóa
        /// </summary>
        public int levelUnlock;
        /// <summary>
        /// Giới hạn số lần mua, -1 nếu k giới hạn
        /// </summary>
        public int limitBuy;
        /// <summary>
        /// pack này sẽ xuất hiện trong bao lâu
        /// </summary>
        public float durationShow;
        /// <summary>
        /// thời gian cho lần xuất hiện tiếp theo của pack
        /// </summary>
        public float coolDown;
        /// <summary>
        /// Nội dung đi kèm
        /// </summary>
        public string tag;
        /// <summary>
        /// Tên hiển thị của pack
        /// </summary>
        public string name;
    }
```

### Luồng

- Khi user login → gửi `CSGetLosePromoteConfig` → nhận `SCLosePromoteConfig`
- Gọi `LosePromoteManager.SaveConfig()` để lưu `quantityShow` + `configs`
- Khi wrapper `AfterGetData()`, nếu pack đủ điều kiện sẽ được emit sự kiện tạo UI
- UI `UILosePromote` sẽ:
  - Nghe event `falcon.modules.shop.create_pack_shop`, thêm vào group kéo ngang
  - Tự sắp xếp theo `priority` từ `LosePromoteConfig`

---

## 5. Các lỗi thường gặp

| Lỗi | Nguyên nhân | Giải pháp |
|------|-------------|-----------|
| Không hiện gói | Không đủ điều kiện cooldown hoặc chưa emit | Kiểm tra `GetStatePack()` |
| Banner không hiển thị đúng thứ tự | `priority` trùng hoặc thiếu config | Kiểm tra `LosePromoteConfig` |
| Không update countdown | Pack không có `durationShow` hoặc sai timestamp | Check `UserData.timeEnd` |