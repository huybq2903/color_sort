# Falcon Modules Packs Gold

## 1. Mô tả

Module `PacksGold` là một nhóm mở rộng của hệ thống `Falcon Packs`, chuyên dùng để hiển thị các gói gold vật phẩm trong shop.  
Mỗi gói bao gồm phần thưởng gold, và được hiển thị bằng prefab động `UIPack_Gold`.

Các thành phần chính:
- `WrapperPacksGold`: Wrapper chính quản lý các gói vật phẩm trong hệ thống PacksBundle
- `UIPackGoldElement`: thành phần UI hiển thị gói, kế thừa từ APackElement

---

## 2. Quick Start

### Bước 1:
 - Import Asset: Falcon > Modules > Pack > Import Pack Gold Asset
 - Trong asset bao gồm sẵn 1 prefab UIPack_Gold, UIPack_Gold_Grid dùng chung, và 1 SO_FCM_PacksBundle_Config config sẵn dưới local
 - Trong SO có param "isGrid": "0" sẽ là layout dọc bình thường, còn "1" sẽ là layout grid (ví dụ 2 x 3)
 - Nếu dùng grid pack thì trên cms điền idPack trong ShopConfig là "grid_packs_gold" thay thế cho các idPack Gold
 - Thay ảnh reward set lại name reward trong từng UIReward để tương ứng với game
### Bước 2:
 - Thêm các idPack vào config shop, miniShop nếu muốn hiện trên cms
---

## 3. Chi tiết


#### Prefab UIPack_Gold

Prefab dùng chung cho các gói Bundle, nhận idPack, lấy reward và khởi tạo layout tương ứng.
Kế thừa từ APackElement nên đã có sẵn setup giá tiền và gán callback mua hàng.
Trong pack có sẵn các UIReward, sẽ bật tắt tùy theo reward tương ứng có trong pack không.
Có thể tự viết 1 prefab khác tùy theo nhu cầu

#### SO_FCM_PacksGold_Config

SO config sẵn của các gói. Trong trường hợp server chưa trả về config kịp thì sẽ lấy ở trong SO để khởi tạo mặc định

#### WrapperPacksBundle
- Khi login hoặc load local xong, `WrapperPacksGold.AfterGetData()` được gọi
- Với mỗi `idPack`, hệ thống sẽ phát:
```csharp
GameEvent<(AssetReference, string)>.Emit("falcon.modules.shop.create_pack_shop", (new AssetReference("UIPack_Gold"), idPack));
```
AssetReference là một wrapper metadata cho asset trong hệ thống Addressables, gọi `new AssetReference(string address)` sẽ trỏ tới asset có địa chỉ đó trong addressable
- Các shop sẽ đăng kí sự kiện, cache lại AssetReference và khởi tạo UI từ cache đó


---

## 4. Các lỗi thường gặp

| Lỗi | Nguyên nhân                                                   | Cách khắc phục                             |
|------|---------------------------------------------------------------|--------------------------------------------|
| Không thấy gói trong UI | Chưa có prefab `UIPack_Gold` hoặc chưa thêm vào config shop | Kiểm tra `AssetReference` hoặc config shop |

---