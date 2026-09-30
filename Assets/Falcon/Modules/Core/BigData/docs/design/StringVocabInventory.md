# Kiểm kê string key/value trên bề mặt FalconBigDataController — input cho đề xuất enum-hoá

Quét 09/09/2026, phủ toàn bộ API group (`Level` · `Ad` · `IapOffer` · `Iap` · `Resource` ·
`Funnel` · `App` · `Label` · `Common` · `Ui`) + các param object + đường `.Send()` custom.
Chia **ba loại theo bản chất** — vì "enum-hoá toàn bộ" sẽ đụng ba câu trả lời khác nhau.

## A. String mang VOCAB — tập giá trị đóng được, là ứng viên enum-hoá thật sự

| API group | Chỗ nhập | Đã có vocab chưa |
|---|---|---|
| **Funnel** | `funnelName` | ⚠️ một nửa — `FFunnelName` (ftue, battle_pass, daily_quest, piggy_bank, lucky_wheel + prefix `event_`); tên event tự chế vẫn string tự do |
| | `action` | ⚠️ một nửa — `FFunnelAction` (join/milestone/claim/complete/tutorial_*); action tự chế (`eligible`, `open`) vẫn string |
| **Resource** | `resourceWhen` | ⚠️ một nửa — `FResourceWhen` (~15 hằng + prefix `event_`) |
| | `resourceWhere` | ❌ vocab riêng từng game, SDK cố ý không khai |
| | `itemType` | ❌ tự do |
| **Iap** | `where` | ⚠️ một nửa — `FIapPlacement` (battle_pass, piggy_bank, event_shop, starter_pack, remove_ads, shop) |
| | `iapWhen`, `purchaseMethod` | ❌ tự do |
| | `validationStatus` | ✅ `FIapValidationStatus` (4 giá trị) |
| **IapOffer** | `offerCategory`, `offerLayout`, `offerSurfaceType`, `placementScene`, `triggerEvent` | ❌ toàn bộ tự do — nhóm "vocab-shaped nhưng chưa ai đóng" DÀY nhất |
| **Level** | `difficulty` | ❌ tự do (docs đang dặn miệng "normal/hard/very_hard") |
| **Ad** | `adWhen`, `adWhere` | ❌ vocab per game |
| | `adMediation` | ❌ tự do ("Max"/"IronSource"… — rất đáng đóng) |
| | `errorCode` (`OnLoadFailed`/`OnShowFailed`) | ❌ tự do (chuỗi từ mediation) |
| **Ui** | `uiAction` | ✅ `FUiAction` (impression/click) — nhưng chữ ký service nhận string nên vẫn gõ tay được |
| **Label** | `labelKey` (User/Session/LevelPlayTurn/AdView/IapOfferImpression/IapPurchase/Level/IapOffer) | ⚠️ một nửa — `FUserLabelKey` (elo…), `FLevelLabelKey` (moves_limit, time_limit_sec), `FAdViewLabelKey` (load_error, show_error); key tự chế vẫn string |
| **App/Permission** | `permissionValue` (đường `ReportPermission` string) | ✅ `FPermissionValue` + 3 wrapper kiểu hoá (Att/AdsConsent/Push) — string chỉ là cửa thoát cho quyền mới |
| **Custom event** (`FEventLog`/`EventParam`, đường `.Send()`) | `eventName`, `eventWhere`, `eventWhen` | ❌ hoàn toàn tự do |

## B. String ĐỊNH DANH — không enum được về nguyên tắc (id/mã tự nhiên, tập giá trị mở)

`playTurnId` · `currentLevelId` · `cycleId` · `exchangeId` · `transactionId` · `purchaseToken` ·
`adViewId` · `adUnitId` · `adInstanceName` · `adNetwork` · `adFormatRaw` · `adPrecision` ·
`adCountry` · `productId` · `offerId` · `offerProductId[]` · `offerCurrencyCode` ·
`isoCurrencyCode` · `currency` · `itemId` · `surfaceId` · `pushCampaignId` · `networkName` ·
`errorMess` · `gameMode` · `key` của `Common.Set`.

## C. Kênh DICTIONARY — key string tự do (kênh mở là thiết kế, nhưng KEY bên trong chưa có chủ)

| Kênh | Ở đâu | Ghi chú |
|---|---|---|
| `extraMeta` | AdView/Iap/IapOffer/Funnel/AppOpen/Level (`ALevelParamV2`)/Ui/Custom | Mở by design — nhưng các key ĐÃ THÀNH QUY ƯỚC trong docs live-op (từ 17/09 mang tiền tố event — `league_id`, `league_round`, `league_start_day`, `league_stage`, `league_step`, `league_rank`, `league_multiplier`, `league_difficulty`, `race_id`, `race_board_id`, `race_stage`, `race_attempt`, `race_streak_pos`, `race_retry_reason`, `race_lap`, `race_attempts_used`, `race_user_time_sec`, `race_best_bot_time_sec`…) hiện chỉ sống trong doc, chưa có const nào |
| `detail` | `ResourceParam` | Cùng tình trạng với extraMeta |
| `boostersUsed` / `preBoostersUsed` (`Dictionary<string,int>`) | Level Start/Pass/Fail | **Key = tên booster, chưa có vocab** — đúng vụ "key revive tên gì" vừa phải đi hỏi dev từng game |
| `offerPrices` / `offerDiscounts` | `IapOfferParam` | Key = productId (định danh — OK, không enum) |
| `offerLabels` | `FIapOfferLog` | Key tự do |

## Đã typed sẵn (phần xong, để đối chiếu)

enum `AdType` · `LevelStatus` · `LevelFailReason` (serialize giữ chuỗi wire qua `EnumMember`) ·
`FlowType` · `FunnelShape` · `OpenSource` · `PermissionType` · `AttStatus` ·
`IapPurchaseFailReason`.

## Ba ghi chú cho người rà — tránh kết luận nhầm

1. **Nhóm B enum-hoá là sai đề** — định danh không có tập giá trị đóng. Đấu trường thật của đề
   xuất là **nhóm A chưa-đóng** (dày nhất: bộ 5 field của IapOffer, `difficulty`,
   `adMediation`, `itemType`) và **key quy-ước-trong-doc của nhóm C** — nâng thành const class
   (kiểu `FLiveOpsMetaKey`, vocab key `boostersUsed`) là rẻ và chặn được typo ngay tại compile.
2. **Vocab per-game ≠ vocab SDK**: `resourceWhere`, `adWhere`/`adWhen`, `surfaceId`,
   `eventWhere`… là từ vựng của từng game — SDK enum-hoá là CHẶN game; mức đúng cho nhóm này
   là convention đặt tên + đăng ký catalog với data team, không phải enum.
3. **Wire không được đổi**: enum-hoá field đang sống bắt buộc serialize ra ĐÚNG chuỗi cũ
   (khuôn `LevelFailReason` + `EnumMember`) — không thì mỗi field đổi là một era mới phải báo
   loader. Nửa đường cũng hợp lệ: giữ chữ ký string + const class (khuôn `FResourceWhen`) —
   compile không chặn nhưng vocab có một nguồn, cái giá rẻ hơn nhiều so với đổi chữ ký API.
