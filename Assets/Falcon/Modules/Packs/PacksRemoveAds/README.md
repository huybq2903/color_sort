# Falcon Modules Packs Remove Ads

## 1. Mô tả

`PacksRemoveAds` là module quản lý gói mua **Remove Ads** trong game mở rộng từ hệ thống `Falcon Packs Core Module`.
Khi người chơi mua gói, trạng thái sẽ được lưu trong `RemoveAdsData`, toàn bộ UI liên quan sẽ được ẩn, và truyền đi event rằng gói đã được kích hoạt.  
Hệ thống hỗ trợ shortcut ở màn hình chính, popup quảng bá, và đồng bộ server qua `RemoveAdsData`.

Packs bao gồm 2 gói Remove Ads thường: kích hoạt sẽ ẩn các quảng cáo, và gói Remove Ads Bundle: bao gồm tính năng của gói Remove Ads và các phần thưởng đi kèm

Các thành phần chính:
- `WrapperPacksRemoveAds`: wrapper xử lý logic cho các gói remove ads
- `RemoveAdsElementPackConfig`: class config
- `RemoveAdsUserData`: class user data của các gói
- `RemoveAdsData`: FGameData lưu trên server, chứa `RemoveAdsResource`
- `UIPackRemoveAdsElement`: UI gói ở shop
- `UIPopup_RemoveAds`: popup đẩy gói
- `UIShortcut_RemoveAds`: icon shortcut ngoài home để vào popup

Như vậy để đảm bảo tính độc lập, trên server sẽ lưu cả `RemoveAdsUserData` là user data của gói
và `RemoveAdsData` là data trạng thái kích hoạt remove ads, các module khác dùng `RemoveAdsData` nếu muốn biết trạng thái.

---

## 2. Quick Start

### Bước 1: Import asset
- Import Asset: `Falcon > Modules > Pack > Import Pack Remove Ads Asset`
- Trong asset bao gồm:
  - UIPack_Remove_Ads: prefab pack Remove Ads thường
  - UIPack_Remove_Ads_Bundle: prefab pack Remove Ads Bundle
  - UIPopup_RemoveAds: popup bán gói Remove Ads Bundle
  - UIShortcut_RemoveAds: icon shortcut ngoài home vào popup

### Bước 2:
- Thêm 2 pack vào trong cms, cố định 2 gói sẽ có idPack là "Remove_Ads" và "Remove_Ads_Bundle"
---

## 3. Danh sách API

Event Bus

```csharp
  //Đăng kí sự kiện để khi mua gói thành công kích hoạt trạng thái RemoveAds
  GameEvent.Register("falcon.modules.packs.packsremoveads.buy", OnBuyRemoveAds, null);

  private static void OnBuyRemoveAds()
  {
      
  }
```

## 3. Chi tiết

- Gói remove ads là gói 1 lần, có `UserData.timeBuy` lưu số lần mua (thường 0 hoặc 1)
- Lưu cả biến data trong RemoveAdsData, chuyển active = 1 (chưa kích hoạt thì active = 0)
- Khi gói đã mua:
  - Tự động xóa wrapper khỏi hệ thống
  - Ẩn UI liên quan (pack shop, shortcut, popup)
- Khi chưa mua:
  - Tự động tạo UI tại shop
  - Gửi sự kiện promote
  - Tạo shortcut sau khi màn home load xong

---

## 4. Các lỗi thường gặp

| Lỗi | Nguyên nhân | Giải pháp |
|------|-------------|-----------|
| Không ẩn gói sau khi mua | Không gọi `SaveAndSend()` hoặc resource chưa cập nhật | Đảm bảo gọi `BuyRemoveAds()` đúng flow |
| Không hiển thị shortcut | Không nhận `EVENT_LOAD_HOME_COMPLETE` | Kiểm tra có đang ở màn Home và đúng sự kiện |