# Yêu cầu cập nhật — module Inventory/Shop/Lives ↔ BigData 1.3.2 (dòng tài nguyên §D9 + §D10)

Gửi owner các module giữ ví tài nguyên (chính là `Inventory`; `Shop`/`Packs`/`Lives` là caller lớn).
Server theo dõi kinh tế in-game qua `f_sdk_resource_data` (loader tách thành
`f_sdk_resource_source`/`_sink` theo `flowType`, cook net-flow + `currency_class`), nhưng hiện
**không module nào bắn log này** — game nào muốn có economy tracking phải tự log tay từng chỗ
cộng/trừ, dễ sót vế và không ai điền nổi `valueBefore/valueAfter` cho đúng. Trong khi đó mọi đồng
tiền của game đều chảy qua đúng một cửa: `InventoryWrapper.AddResource/RemoveResource`. Đưa log về
cửa đó là mọi game dùng Inventory được economy tracking miễn phí, và ví là nơi DUY NHẤT biết chính
xác số dư trước/sau.

## Việc cần làm

| # | Ở đâu | Làm gì |
|---|---|---|
| 1 | `InventoryWrapper.AddResource` / `RemoveResource` | Thêm overload nhận context log (đề xuất: `AddResource(idResource, value, ResourceParam ctx)`). Khi `ctx != null`: điền `currency = idResource`, `amount = value`, `valueBefore/valueAfter` từ ví, rồi gọi `FalconBigDataController.Resource.OnEarned(ctx)` (Add) / `OnSpent(ctx)` (Remove) — SDK tự set `flowType`. Overload cũ giữ nguyên — gọi không ctx thì **không log** (xem luật chống đếm đôi) |
| 2 | `Shop` — bán bundle/đổi chác | Giao dịch NHIỀU vế (500 gold → 3 booster; gems → gold + booster + heart) gọi **một** cú `FalconBigDataController.Resource.OnExchange(sinks, sources)` — SDK tự sinh `exchangeId` chung đóng lên mọi vế. Các cú Add/Remove ví đi kèm gọi bản **không ctx** |
| 3 | `Shop`/`Packs` — vế nhận từ TIỀN THẬT | Vế source của gói mua bằng tiền thật: `resourceWhen = FResourceWhen.Iap` + `transactionId` của giao dịch (loader ký 18/08: iap dời từ itemType sang resourceWhen, công thức buy/receive phía kho đã đọc 2 era; transactionId để JOIN sang event mua) |
| 4 | `Lives` — tiêu/hồi mạng | Mỗi lần trừ mạng vào ván / hồi mạng theo giờ / mua mạng là một vế `currency = "live"` — loader đã có `currency_class = live` riêng cho nó, hiện cột đó đói dữ liệu |

## Điền field thế nào (vocab §D9)

| Field | Nghĩa | Nguồn giá trị |
|---|---|---|
| `resourceWhen` | **NGUYÊN NHÂN** — vì sao tài nguyên chảy | Hằng số `FResourceWhen.*` (`shop`, `battle_pass`, `level_reward`, `daily_bonus`…); liveops dùng `FResourceWhen.EventPrefix + "halloween_2026"`. **Thêm giá trị mới = sửa hợp đồng (§H2)** — thiếu thì báo BigData, đừng gõ chuỗi tay |
| `resourceWhere` | **VỊ TRÍ** — màn/panel nào | Vocab riêng từng game (`shop_screen`, `level_end_popup`…), caller truyền xuống |
| `itemType` | LOẠI vật phẩm/giao dịch | Game tự đặt (`currency`, `booster`…) |
| `currency` + `amount` | Cái gì, bao nhiêu | `amount` **luôn dương** — chiều nằm ở `flowType`, không dùng số âm (CorrectValues ép về 0 kèm error log) |
| `valueBefore` / `valueAfter` | Số dư trước/sau | Ví điền — audit + để loader bắt miscount; đây là lý do log tại Inventory chứ không tại caller |
| `detail` | Context tuỳ ý | Dictionary tự do, flatten vào `event_extra_props` |
| `adViewId` / `transactionId` | Nối thưởng về ad view / giao dịch IAP đã đẻ ra nó | Đóng TẠI CHỖ PHÁT THƯỞNG (`Ad.CurrentViewId(type)` lúc grant reward / transactionId từ purchase) — CẤM auto-stamp, ĐỒNG THỜI ≠ NGUYÊN NHÂN |

⚠ `resourceWhen`/`resourceWhere` là thứ chỉ **caller** biết (Inventory nhìn cú `AddResource("gold", 500)`
không thể đoán là thưởng level hay mở quà) — vì vậy mới cần overload có ctx thay vì log mù trong ví.

## Ràng buộc phải biết

- **Chống đếm đôi là luật số một**: một giao dịch chỉ được log MỘT lần. Caller đã log
  `OnExchange` thì các cú Add/Remove ví của chính giao dịch đó phải đi bản không-ctx. Net-flow
  phồng 2× là đúng vết xe án `total_play_time` ratio 1.31–1.84 trên fleet — loader dò ra được,
  nhưng lúc đó là sửa số liệu lịch sử, rất đắt.
- **`exchangeId` — mọi vế cùng giao dịch phải cùng id**: `OnExchange` tự lo; nếu tự set (vd id
  giao dịch từ game server) thì set trước trên các vế, SDK thấy có sẵn sẽ dùng lại thay vì sinh
  mới. Không id chung thì server không ghép được vế, giá thật per item vỡ (§D10).
- **ĐỒNG THỜI ≠ NGUYÊN NHÂN**: SDK tự đóng `playTurnId` khi giao dịch xảy ra giữa ván — id đó chỉ
  nói "TRONG lúc chơi màn", không nói "VÌ màn". Nguyên nhân khai ở `resourceWhen` do chỗ phát
  thưởng khai, đừng suy từ id. (Đề xuất nối resource→ad/iap theo nguyên nhân đang chờ loader —
  không tự đóng `adViewId`/`purchaseAttemptId` lên resource log.)
- Thread nào gọi cũng được, pipeline tự khoá; log đi qua unsent queue nên kill-safe sẵn.
- Event id: `f_sdk_resource_data` (đã sống trên DWH, loader split theo `flowType`,
  case-insensitive). Tham chiếu: `DWH-EventLog-API-Spec.md` §5.5, `TrackingGuide.md` §Resource.
