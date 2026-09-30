# Mediation — TODO cho bộ event quảng cáo v2

> **Đọc phần TODO là đủ để sửa.** Mỗi việc: sửa file nào, chép đoạn code nào, kiểm ra sao.
> Tick `[x]` khi xong. Giải thích "vì sao" nằm ở nửa dưới.
> Viết 23/09/2026, đối chiếu code **Mediation 1.4.31** và dữ liệu thật ngày 22/09 của loader
> (`com.fc.sdk.block.escape`, 951.125 dòng event ad).
> Hướng dẫn nền về cả vòng đời ad: `Request-Mediation-AdLifecycle.md`.

**Cần pin `falcon.modules.core.bigdata` ≥ 1.4.1** (bản có `OnShowAttempt` + `tier`). Hiện
`package.json` đang pin `1.3.6`.

⚠ **Chưa publish vội**: loader phải khai báo 2 event mới trước, nếu không dữ liệu những ngày đầu
hỏng vĩnh viễn. Bên BigData sẽ báo khi rule đã lên.

# ✅ Việc phải làm

## [ ] V1 — Đổi `floor` thành `tier` ở 9 chỗ gọi `OnLoadResult`

Trường `floor` **đổi nghĩa**: giờ `floor` là **giá sàn thật bằng USD**, còn hệ số bậc dời sang
**`tier`**. Code hiện tại gán hệ số vào `floor` nên **vẫn compile nhưng sai nghĩa**.

**Sửa ở 9 chỗ:**

| File | Dòng |
|---|---|
| `Service/Max/FalconMaxService.cs` | 298, 313, 668, 688, 1313, 1332 |
| `Service/MultiCall/Providers/Max/MultiCallMaxProviderParent.cs` | 195, 281, 416, 464 |

```csharp
// TRƯỚC (MAX multicall)
floor = a.multiplier,

// SAU
tier = a.multiplier,          // BẬC trong config — hệ số 1.5 / 3.0 / 5.0
```

```csharp
// TRƯỚC (MAX không multicall — FalconMaxService)
floor = null,

// SAU
tier = null,                  // MAX không multicall: không đặt bậc nào
```

**Kiểm:** chạy QA, console **không** được có cảnh báo `có floor ... nhưng không có tier`. Cảnh báo
đó nghĩa là còn sót một chỗ chưa đổi.

## [ ] V2 — Truyền `adMediation` ở đúng 9 chỗ đó

Thêm một dòng vào cùng các lời gọi trên:

```csharp
FalconBigDataController.Ad.OnLoadResult(new AdLoadResultParam
{
    adType = AdType.Interstitial,
    adUnitId = adUnitId,
    success = true,
    adMediation = "Max",          // ← THÊM. "Max" ở FalconMaxService + MultiCallMax*,
                                  //   "Admob" ở AdmobMediationFalconBridge
    tier = a.multiplier,
    networkName = adInfo.NetworkName,
    loadingMs = adInfo.LatencyMillis
});
```

**Kiểm:** log `f_sdk_ad_load_stats` có `ad_mediation` ở cấp trên của bản tin.

## [ ] V3 — AdMob gửi thêm `tier`, giữ `floor` là giá thật

`Service/MetaAdmob/AdmobMediationFalconBridge.cs` dòng **558** (`ReportLoadResult`):

```csharp
FalconBigDataController.Ad.OnLoadResult(new AdLoadResultParam
{
    adType = ToFalconAdType(result.format),
    adUnitId = result.adUnitId,
    success = result.success,
    adMediation = _MEDIATION,        // ← V2
    tier = result.multiplier,        // ← THÊM: hệ số của biến thể (BuildFloorVariants)
    floor = result.floorEcpmUsd,     // ← GIỮ: AdMob biết giá sàn thật, USD/1000 impression
    networkName = result.network,
    errorMess = result.errorMessage,
    loadingMs = result.loadingMs
});
```

`AdmobMediationHooks.AdmobLoadResult` cần thêm field `multiplier` — giá trị có sẵn ở
`AdmobFullscreenAds.RequestFloorGroup` (`variants[i].multiplier`, chính hệ số đang nhân với
revenue để ra giá sàn), chỉ cần chở thêm xuống kết quả load.

**Kiểm:** dòng AdMob có **cả** `tier` và `floor`; dòng MAX chỉ có `tier`.

## [ ] V4 — Gọi `OnShowAttempt` ở 3 cửa show

Mốc mới: **game muốn chiếu ad**, bắn TRƯỚC khi kiểm tra kho, gọi ở **cả hai nhánh** (có ad và
không có ad).

**Chỗ 1 + 2 — `MediationManager.cs`, hai hàm `ShowInterstitial` (dòng 576) và `ShowRewardedVideo`
(dòng 609).** Đây là cửa duy nhất game gọi vào, nên đặt ở đây là phủ hết MAX / multicall / AdMob:

```csharp
public void ShowInterstitial(
    string placementId, Action onInterstitialClosed = null, Action onFail = null, bool needAdBreak = false)
{
    DebugLog("mediation manager -> show interstitial");

    FalconBigDataController.Ad.OnShowAttempt(                       // ← THÊM (4 dòng)
        AdType.Interstitial,
        adWhere: string.IsNullOrWhiteSpace(placementId) ? null : placementId,
        adAvailable: IsInterstitialReady());

    var provider = GetProvider();
    ...
}
```

```csharp
public void ShowRewardedVideo(
    string placementId, Action onRewardedComplete = null, Action onFail = null,
    bool showInterstitialInstead = true)
{
    DebugLog("mediation manager -> show rewarded");

    FalconBigDataController.Ad.OnShowAttempt(                       // ← THÊM
        AdType.Reward,
        adWhere: string.IsNullOrWhiteSpace(placementId) ? null : placementId,
        adAvailable: IsRewardedVideoReady());

    var provider = GetProvider();
    ...
}
```

**Chỗ 3 — AppOpen**, `Service/Max/FalconMaxService.cs` hàm `ShowAppOpenAdIfReady()` (dòng 604),
đặt ngay trước nhánh `if (MaxSdk.IsAppOpenAdReady(id))` ở dòng 617:

```csharp
#if MAX_ENABLE
    FalconBigDataController.Ad.OnShowAttempt(                       // ← THÊM
        AdType.AppOpen, adWhere: _APP_OPEN_WHERE,
        adAvailable: MaxSdk.IsAppOpenAdReady(id), adMediation: _MAX);

    if (MaxSdk.IsAppOpenAdReady(id))
    ...
```

⚠ Nhánh `IsRemoveAds()` ở đầu hàm (dòng 606) **thoát trước** — không gọi mốc này ở đó, vì user gỡ
quảng cáo thì không phải "muốn chiếu mà hụt ad".

⚠ **Đừng gọi thêm ở tầng dưới** (`MultiCall.ShowInterstitial`, `MultiCallProvider`,
`AdmobMediationFalconBridge`) — gọi hai tầng là đếm đôi.

**Kiểm:**
- Gọi show lúc kho rỗng → có đúng **một** dòng `f_sdk_ad_show_attempt` với `ad_available = false`,
  và **không** có dòng `f_sdk_ad_show_data` nào theo sau.
- Bất biến: số dòng `f_sdk_ad_show_attempt` ≥ số dòng `f_sdk_ad_show_data`.

## [ ] V5 — Pin `package.json` lên BigData ≥ 1.4.1

```json
"falcon.modules.core.bigdata": "1.4.1"
```

Bản 1.3.6 đang pin không có `OnShowAttempt`, không có `tier`, và không có cơ chế tự xoay
`adViewId` (từ 1.3.8). Bộ event ad v2 nằm ở **BigData 1.4.0**, bản vá khoá gộp ở **1.4.1**.

## [ ] V6 — `floor` của AdMob phải là số ỔN ĐỊNH trong một phiên

Hiện `floorEcpmUsd = revenue × hệ số × 1000`, mà `revenue` là giá của ad MAX vừa load nên **đổi
mỗi lần**. Con số như vậy có hai vấn đề:

- **Đọc không có nghĩa**: cùng một bậc, mỗi máy mỗi ngày ra một số khác nhau; trung bình của một
  đống số nhảy loạn cũng khó dùng.
- **Trước 1.4.1 nó còn làm hỏng cơ chế gộp** — SDK đã tự vệ ở 1.4.1 (bỏ `floor` khỏi khoá gộp),
  nhưng yêu cầu về giá trị vẫn còn nguyên.

Chọn **một** trong ba:

| Cách | Làm gì | Ghi chú |
|---|---|---|
| **1 (đề xuất)** | **Không gửi `floor`** (để null), chỉ gửi `tier` | Server ước lại được: doanh thu thật ở `f_sdk_ads_data` × `tier`. Sửa ít nhất |
| 2 | Làm tròn về **lưới tương đối 10%** trước khi gửi | Vẫn thấy tiền, sai số ±5%. Đừng làm tròn theo bước CỐ ĐỊNH: dải giá trị chạy từ 0,01 tới hơn 5.000 |
| 3 | Khai **giá danh nghĩa của mỗi bậc** trong config rồi gửi số đó | Ổn định và vẫn có tiền, nhưng phải thêm cấu hình |

Code cho cách 2, nếu chọn:

```csharp
private static double? RoundFloorToGrid(double? floorUsd)
{
    if (floorUsd is not { } v || v <= 0d) return null;   // không đặt sàn -> null, ĐỪNG gửi 0
    const double step = 1.10;                            // lưới 10%
    var k = Math.Round(Math.Log(v) / Math.Log(step));
    return Math.Round(Math.Pow(step, k), 4);
}
```

**Kiểm:** chạy QA 30 phút, đếm số giá trị `floor` khác nhau của **cùng một ad unit** trong một lần
gửi. Ra 1–3 là ổn; trên 5 thì nới lưới, hoặc chuyển hẳn sang cách 1.

# ❓ Câu phải trả lời

## [ ] H1 — `loadingMs` của lần load THÀNH CÔNG đo cái gì?

| | Thời gian trung bình mỗi lần load |
|---|---:|
| Thành công (Interstitial / Reward) | **17,8 s / 19,9 s** |
| Thất bại (Interstitial / Reward) | 4,4 s / 5,4 s |

Thành công lâu gấp 4 lần thất bại là ngược đời. Nghi `adInfo.LatencyMillis` đo từ đầu cả waterfall
chứ không phải của lần load ấy. Đúng không, và có lấy được số của riêng lần load không?

## [ ] H2 — `errorMess` đang trộn 3 dạng

| Dạng | Ví dụ | Số dòng |
|---|---|---:|
| Mã chữ ngắn | `NoFill`, `NetworkError` | 258k |
| **Số trần, không có từ điển** | `0`, `1`, `3` | **108k** |
| **Câu dài tới 126 ký tự** | `No ads meet eCPM…` | 6,9k |

Nhờ chuẩn hoá về mã chữ ngắn. Dạng số (nghi của AdMob) thì gửi kèm **từ điển mã → nghĩa**.

## [ ] H3 — `adCompleted` của Interstitial có nghĩa không?

Đo được: **8.484 dòng `true`, 0 dòng `false`, vắng 46%**. Nếu interstitial không có khái niệm "xem
hết" thì **bỏ hẳn** tham số này ở mốc close của interstitial, chỉ giữ cho rewarded (rewarded đang
đúng: 10.721 true / 478 false).

## [ ] H4 — AppOpen không có một dòng nào

Code 1.4.31 đã nối vòng đời AppOpen, nhưng dữ liệu 22/09 **không có dòng nào** mang
`type = AppOpen`. Game có bật AppOpen không, hay có nhánh nào chặn?

## [ ] H5 — IronSource và GMA bao giờ chuyển?

`Service/IronSource/FalconIronSourceService.cs` và `Service/GoogleMobileAds/FalconGmaService.cs`
vẫn log thẳng bằng `BannerLogService` / `ExtendAdLog` và hai log cũ `FAdCalledLog` /
`FAdDisplayFailedLog`, không có mốc vòng đời nào. Riêng `f_sdk_ad_called` đang chiếm **51% lượng
dòng quảng cáo của iOS**. Có kế hoạch chuyển không?

---

# 📖 Giải thích (không cần đọc để sửa)

## Vì sao tách `tier` và `floor`

| | Biết BẬC trong config | Biết GIÁ SÀN THẬT (USD) |
|---|---|---|
| MAX | ✅ (hệ số đặt tên unit) | ❌ — nằm trên dashboard MAX |
| AdMob | ✅ | ✅ — tự đặt sàn từng slot |

Ép một trường là hỏng một bên: bắt MAX ghi tiền thì nó phải nhân hệ số với giá ad vừa xem — cùng
một bậc ra số khác nhau theo máy và theo ngày, đầu phiên còn ra `0` vì chưa có ad làm nền. Loader
đo ngày 21/09 thấy đúng hiện tượng đó (giá trị liên tục từ `0,02` tới `1.926`).

Từ app 2.7.1, MAX đã gửi hệ số (rời rạc `1,1–13,0`), còn AdMob vẫn gửi tiền — **cùng một cột đang
chở hai nghĩa**. Tách đôi là để hết chuyện đó.

Ba điều cấm:
1. **Cấm** nhân hệ số với doanh thu rồi điền vào `floor`.
2. **Cấm** điền `0` khi không có giá trị — để `null`. Vắng-key là thứ duy nhất phân biệt "không
   đặt sàn" (`tier` vắng) và "không biết giá thật" (`floor` vắng) với "đặt sàn 0".
3. Code cũ `floor = a.multiplier` vẫn compile sau khi đổi tên. SDK **cảnh báo lúc chạy** cho ca
   này — đó là lưới an toàn duy nhất, đừng bỏ qua cảnh báo đó trong log QA.

## Vì sao cần mốc `OnShowAttempt`

Từ khi module nạp sẵn ad (van `AdViewRequestGate` của các bạn — làm rất tốt), một đợt xin ad phục
vụ nhiều lần chiếu. Loader đo: khoảng cách từ xin ad tới lúc chiếu tăng từ 26 giây lên **219 giây**,
và **15%** số lần show không có dòng request đi kèm.

Hệ quả: `request` giờ là "một đợt nạp hàng", không còn là "một lần user cần ad". Ca **kho rỗng lúc
game muốn chiếu** không có dòng log nào — mất hẳn mẫu số của fill rate.

Mốc mới lấp đúng chỗ đó, và cho chỉ số mà MO cần:

```
view_fill_rate = số dòng show_attempt có adAvailable = true / tổng số dòng show_attempt
```

Bên BigData đã cân nhắc phương án rẻ hơn (chỉ bắn khi kho rỗng) nhưng bỏ: mốc chỉ-ghi-khi-hỏng mà
quên gọi một luồng thì phần hỏng biến mất im lặng, fill rate đẹp lên giả tạo, không có bất biến nào
soi được.

## Phía BigData đã đổi gì (các bạn không phải làm)

- Hai event `f_sdk_ad_load_success` + `f_sdk_ad_load_fail` **gộp thành một**
  `f_sdk_ad_load_stats`: một dòng cho mỗi (loại ad × mediation) mỗi lần xả, chi tiết từng unit nằm
  trong mảng. Giảm từ ~158 xuống ~20 dòng/user/ngày. **Chữ ký `OnLoadResult` không đổi.**
- `hasClick` ở mốc close giờ gửi `false` tường minh thay vì bỏ trống.

## Những việc đợt trước đã làm xong (ghi nhận)

Van `AdViewRequestGate` (một mốc request cho mỗi đợt), mốc bỏ cuộc `OnLoadFailed`, lấy
`CurrentViewId` gắn vào `CSAdStart`/`CSAdFinish`, rewarded bắn close cả khi user bỏ ngang kèm
`adCompleted` thật, ngừng gửi `MAdLog`, banner nối vòng đời. Loader đo lại đã xác nhận: tỉ lệ
request/impression từ **36,9:1 xuống 1,4:1**, id trùng từ 5% xuống 0,17%.

---
*Trả lời vào `Request-Mediation-AdEventV2-reply.md` cùng thư mục. File xoá khi ship.*
