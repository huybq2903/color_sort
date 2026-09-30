# Thư mục doc của BigData — bản đồ và trạng thái

Cập nhật 23/09/2026. Bốn thư mục, bốn loại người đọc:

| Thư mục | Ai đọc | Vòng đời |
|---|---|---|
| `design/` | Người làm SDK + data team | Sống lâu dài, sửa khi hợp đồng đổi |
| `games/` | Dev game từng event | Sống tới khi event đóng |
| `modules/` | Owner từng module SDK | **Xoá khi module ship xong** |
| `loader/` | Loader / data team | **Xoá khi được ký và ship** |
| `archive/` | Ai cần tra lại số đo cũ | Đã đóng, giữ để tham chiếu |

## design/ — hợp đồng và khuôn mẫu

| File | Nội dung |
|---|---|
| `DWH-EventLog-API-Spec.md` | Đặc tả mọi event trên wire: trường, kiểu, luật đọc. **Nguồn sự thật** khi có tranh cãi |
| `EntityLifecycle-Design.md` | Vòng đời từng entity (level turn, ad view, offer, phiên) và lý do thiết kế |
| `LiveOpsTrackingPattern.md` | Khuôn tracking cho event live-op mới — đọc trước khi viết doc cho event mới |
| `TrackingGuide.md` | Hướng dẫn dev game dùng SDK |
| `StringVocabInventory.md` | Kiểm kê string/key trên bề mặt API — đầu vào cho đề xuất enum-hoá |

## games/ — hướng dẫn cho dev từng event

| File | Trạng thái |
|---|---|
| `Request-Game-MasterLeague.md` | ⏳ Chờ làm: module M1–M5, game G1–G3, server S1–S2 |
| `Request-Game-RaceEvent.md` | ⏳ Chờ làm: X1–X2 (gấp — nghi nút event bị giấu), module A1–A6, game G1–G4 |
| `Request-Game-EndlessTreasure.md` | ⏳ Chờ dev Endless Treasure làm A1–A6; phần Packs tuỳ luồng cộng quà |

Tất cả đều theo một khuôn: **TODO cầm tay chỉ việc ở đầu, giải thích và spec ở nửa dưới**.

## modules/ — thư yêu cầu gửi owner module

| File | Gửi ai | Trạng thái |
|---|---|---|
| `Request-Mediation-AdLifecycle.md` | Mediation | 📗 Hướng dẫn nền, giữ lâu dài. Đã cập nhật `tier`/`floor` và event gộp 23/09 |
| `Request-Mediation-AdEventV2.md` | Mediation | ⏳ Chờ trả lời: V1–V3 (gọi `OnShowAttempt`, truyền `adMediation`, tách `tier`/`floor`), H1–H4 |
| `Request-Packs-RewardContext.md` | Packs (huybq) | ⏳ Chờ trả lời — chỉ cần nếu bundle Endless Treasure đi qua Packs |
| `Request-EventsCore-GrantContext.md` | Events Core | ✅ Đã ship (`Grant(..., context)`), **chờ owner bump version** rồi xoá file |
| `Request-GameData-ResourceParam.md` | GameData (ngocdx) | 🟡 Ship một phần ở 1.1.7: `param` đã thông; còn mục 3.1 (`itemType`/`itemId` bị điền bằng `where`) — owner chốt là cột legacy, để lại |
| `Request-Iap-CheckoutFunnel.md` | IAP / InAppValidation | ⏳ Chờ: `validationStatus` trên mọi nhánh verdict |
| `Request-Inventory-ResourceFlow.md` | Inventory / Shop / Lives | ⏳ Chờ ack |
| `Request-Push-OpenSource.md` | Push | ⏳ Chờ ack |
| `Request-Ump-PermissionLog.md` | UMP | ⏳ Chờ ack |

## loader/ — thư gửi loader / data team

| File | Trạng thái |
|---|---|
| `Request-Loader-AdEventV2.md` + `-reply.md` + `-ack.md` | 🔴 **Đang chạy**: loader đã duyệt hình dạng, **đang chờ họ seed mapping rule** cho `f_sdk_ad_show_attempt` và `f_sdk_ad_load_stats`. SDK **chưa publish** cho tới khi rule lên |
| `Request-Loader-FunnelCycleMeta.md` | ⏳ Chờ ack 4 field funnel của 1.3.8 |
| `Request-Loader-UiImpression.md` | ⏳ Chờ ký `f_sdk_ui_impression` — phải ký trước khi game ship Ui API |

## archive/ — đã đóng, giữ để tra số

| File | Vì sao đóng |
|---|---|
| `Request-Loader-AdLoadFloorCheck.md` + `-reply.md` | Đã trả lời xong: `floor` chỉ thiếu ~1%, gần như toàn bộ là unit AdMob không đặt sàn; chênh lệch bên data là do điều kiện query. Kết luận đã gộp vào thư V2 |
| `Request-Loader-AdEventAudit.md` + `-reply.md` | Bộ kiểm A1–A11 / B1–B6 đã chạy xong trên dữ liệu 22/09. **Reply chứa nhiều số đo còn dùng để đối chiếu sau này** |
| `Request-Loader-PendingMarks.md` | Mốc đọc 1.3.8 đã trả; nhịp `OnRequested` đã sửa ở Mediation 1.4.31 (van `AdViewRequestGate`) |
| `Request-Mediation-FloorQuestions.md` | Đã trả lời; phần việc còn lại chuyển sang `Request-Mediation-AdEventV2.md` |

## Quy ước chung

- **Thư có `-reply.md`** là đã có người trả lời. Đọc cả hai trước khi hỏi lại.
- **Thư trong `modules/` và `loader/` xoá khi ship** — bản lưu nằm trong git history.
- **Mọi thay đổi đều có một dòng trong `CHANGELOG.md`** ở gốc module, ghi cả lý do và người chốt.
