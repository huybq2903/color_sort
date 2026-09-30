# Hướng dẫn tích hợp log ad cho module Mediation — BigData ≥ 1.4.0

> **Đọc kèm `Request-Mediation-AdEventV2.md`** (việc đang chờ làm). File này là hướng dẫn NỀN cho
> cả vòng đời ad; hai chỗ đã đổi từ 23/09 và đã sửa trong file này:
> - Telemetry load giờ đi MỘT event gộp `f_sdk_ad_load_stats` (mảng `units[]`), thay
>   `f_sdk_ad_load_success` + `f_sdk_ad_load_fail`. **Chữ ký `OnLoadResult` không đổi.**
> - Trường `floor` của `AdLoadResultParam` **đổi nghĩa và tách đôi**: `tier` = BẬC trong config,
>   `floor` = GIÁ SÀN THẬT (USD). Xem bảng map bên dưới.
> - Thêm mốc `OnShowAttempt` ở đầu hàm show (mốc phía CẦU) — chi tiết ở thư V2.

Gửi owner `Core/ThirdParty/Mediation` (+ MediationMO). Mục tiêu: server thấy trọn vòng đời một
lần xem ad (`request → show → impression → close`, chung một `adViewId` SDK tự sinh) + telemetry
load-result không spam.

## Bảng tra NHANH: callback nào → chép dòng nào

| Chỗ trong code Mediation | Dòng cần thêm |
|---|---|
| Ngay trước `MaxSdk.LoadInterstitial(...)` (lần xin ĐẦU của đợt) | `FalconBigDataController.Ad.OnRequested(AdType.Interstitial, adMediation: "Max");` |
| `OnAdLoadedEvent` | `FalconBigDataController.Ad.OnLoadResult(new AdLoadResultParam { ... success = true ... });` |
| `OnAdLoadFailedEvent` (mỗi lần fail) | `FalconBigDataController.Ad.OnLoadResult(new AdLoadResultParam { ... success = false ... });` |
| Khi BỎ CUỘC hẳn cả đợt load | `FalconBigDataController.Ad.OnLoadFailed(AdType.Interstitial, errorCode);` |
| Ngay trước `MaxSdk.ShowInterstitial(...)` | `FalconBigDataController.Ad.OnShown(AdType.Interstitial, adWhere: placement, adWhen: lyDoChieu);` |
| `OnAdDisplayFailedEvent` | `FalconBigDataController.Ad.OnShowFailed(AdType.Interstitial, errorCode);` |
| `OnAdRevenuePaidEvent` (SAU khi cộng sổ LTV) | `FalconBigDataController.Ad.OnImpression(new AdParam { ... });` |
| `OnAdClickedEvent` | `FalconBigDataController.Ad.OnClicked(AdType.Interstitial);` |
| `OnAdHiddenEvent` | `FalconBigDataController.Ad.OnClosed(AdType.Interstitial);` |
| Lúc dựng `CSAdStart`/`CSAdFinish` | gắn thêm `FalconBigDataController.Ad.CurrentViewId(AdType.Interstitial)` |

Ví dụ dưới viết cho Interstitial + MAX; Rewarded/AppOpen y hệt (đổi `AdType`), IronSource/AdMob
lấy callback tương đương.

⚠ **Tự vệ id từ 1.3.8** (án 5% `ad_view_id` bị tính tiền nhiều lần trên server, 03/09): show hoặc
impression thứ hai đổ vào view đã có impression (tức thiếu `OnRequested` cho đợt load mới) thì
SDK **tự xoay `adViewId` mới** cho lần chiếu đó + warning trong log — dữ liệu không trùng mã nữa
nhưng mất mối nối request→impression (fillLatency null). Thấy warning này là sửa nhịp
`OnRequested`, đừng coi nó là chuyện của SDK. Banner không bao giờ xoay — id banner là id
instance theo hợp đồng.

## Code mẫu đầy đủ — chép được luôn

```csharp
using Falcon.Helpers.Devkit;          // AdType
using Falcon.Modules.Core.BigData;    // FalconBigDataController, AdLoadResultParam, AdParam

// ───────────────────────── 1. LOAD ─────────────────────────

void LoadInterstitial()   // hàm load hiện có của bạn
{
    // CHỈ gọi ở lần xin ĐẦU của mỗi đợt (preload sau boot / refill sau khi show xong).
    // Vòng RETRY TỰ ĐỘNG bên trong KHÔNG gọi lại dòng này — retry là volume không chặn trên,
    // số attempt đã được đếm ở OnLoadResult bên dưới.
    FalconBigDataController.Ad.OnRequested(AdType.Interstitial, adMediation: "Max");
    // ⚠ KHÔNG truyền adWhere/adWhen ở đây — lúc load chưa biết sẽ chiếu ở đâu/vì sao,
    //   điền bừa là làm sai phân tích placement. Hai cái đó khai ở OnShown (mục 2).

    MaxSdk.LoadInterstitial(adUnitId);
}

void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
{
    // Báo kết quả load của UNIT này.
    // Gọi MỖI lần thoải mái — SDK tự gộp cụm, flush lúc app pause, không spam server.
    // Game KHÔNG dùng MultiCall (máy yếu/3G, một unit đơn) VẪN gọi — bỏ dòng floor là xong.
    FalconBigDataController.Ad.OnLoadResult(new AdLoadResultParam
    {
        adType = AdType.Interstitial,
        success = true,                         // set TƯỜNG MINH — default là false đó!
        adUnitId = adUnitId,
        tier = 1.5,                             // BẬC trong config (hệ số). Prefix là string thì
                                                // parse bằng CultureInfo.InvariantCulture
                                                // (vi-VN đảo dấu thập phân: "1.2" thành 12!).
                                                // Không đặt sàn → để null, ĐỪNG điền 0
        // floor = 0.42,                        // chỉ điền nếu mediation BIẾT giá sàn thật (USD);
                                                // MAX không biết → bỏ dòng này
        networkName = adInfo.NetworkName,
        loadingMs = adInfo.LatencyMillis        // MAX đo hộ thời gian của CHÍNH attempt này
                                                // (từ lúc bắt đầu load tới callback) — không
                                                // cần stopwatch riêng. SDK cộng dồn thành
                                                // totalLoadingMs, server chia count ra trung bình.
    });
    // HẾT — "load thành công" KHÔNG có mốc nào khác phải gọi (giải thích ở cuối doc).
}

void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
{
    // Mỗi lần fail của mỗi unit: báo load-result (SDK tự gộp — spam thoải mái)
    FalconBigDataController.Ad.OnLoadResult(new AdLoadResultParam
    {
        adType = AdType.Interstitial,
        success = false,
        adUnitId = adUnitId,
        tier = 1.5,                             // bậc trong config — như nhánh success
        errorMess = errorInfo.Code.ToString(),  // Code chứ đừng Message — xem bảng map
        loadingMs = errorInfo.WaterfallInfo?.LatencyMillis ?? 0  // không có số → 0,
                                                // cộng dồn 0 không làm bẩn tổng
    });

    // ⚠ CHỈ KHI BỎ CUỘC HẲN (mọi floor đã cạn, không retry nữa) mới gọi thêm dòng này —
    //   MỘT lần cho cả đợt. Gọi mỗi lần unit fail là spam kênh nhãn:
    if (DaCanMoiFloorVaThoiRetry())
        FalconBigDataController.Ad.OnLoadFailed(AdType.Interstitial, errorInfo.Code.ToString());
}

// ───────────────────────── 2. SHOW ─────────────────────────

void ShowInterstitial(string placement, string lyDoChieu)   // gameplay quyết định chiếu
{
    // ĐÂY là chỗ khai adWhere/adWhen (không phải lúc load):
    //   adWhere = chiếu Ở ĐÂU trong game ("level_end", "home_button"...)
    //   adWhen  = VÌ SAO chiếu ("level_fail", "level_complete"...)
    FalconBigDataController.Ad.OnShown(AdType.Interstitial,
        adWhere: placement, adWhen: lyDoChieu);

    MaxSdk.ShowInterstitial(adUnitId);
}

// Nếu biết chỗ chiếu TRƯỚC khi gọi show (chọn placement xong) thì báo sớm cũng được:
//   FalconBigDataController.Ad.UpdateContext(AdType.Interstitial, adWhere: placement);
// — các mốc sau tự mang theo.

void OnInterstitialAdFailedToDisplayEvent(
    string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
{
    FalconBigDataController.Ad.OnShowFailed(AdType.Interstitial, errorInfo.Code.ToString());
}

// ─────────────────── 3. TIỀN VỀ (impression) ───────────────────

void OnInterstitialAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
{
    // BƯỚC 1: cộng sổ LTV như hiện tại (GameData4AdInfo) — PHẢI TRƯỚC bước 2,
    // không thì typeCount/adLtv trên log trễ một impression.
    CongSoGameData4AdInfo(adInfo);   // code hiện có của bạn

    // BƯỚC 2: log doanh thu. Điền được gì điền nấy, không biết thì BỎ TRỐNG (đừng đoán).
    FalconBigDataController.Ad.OnImpression(new AdParam
    {
        type = AdType.Interstitial,
        adRev = adInfo.Revenue,
        adNetwork = adInfo.NetworkName,
        adPrecision = adInfo.RevenuePrecision,
        adCountry = MaxSdk.GetSdkConfiguration().CountryCode,
        adUnitId = adUnitId,
        adMediation = "Max"
        // adWhere/adWhen KHÔNG cần điền — SDK tự lấy từ lúc OnShown
        // typeCount/adLtv CẤM tự điền — decorator đọc từ sổ bạn vừa cộng
    });

    // ⚠ XOÁ dòng FAdLogMediation cũ ở đây — để cả hai là ĐẾM ĐÔI DOANH THU.
    // Banner: cũng gọi OnImpression bình thường — SDK tự gộp cụm banner, không cần
    // gọi BannerLogService trực tiếp nữa.
}

// ─────────────────── 4. CLICK & ĐÓNG ───────────────────

void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
{
    FalconBigDataController.Ad.OnClicked(AdType.Interstitial);
}

void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
{
    FalconBigDataController.Ad.OnClosed(AdType.Interstitial);
    // Rewarded thì thêm adCompleted (user có được trả thưởng không):
    //   FalconBigDataController.Ad.OnClosed(AdType.Reward, adCompleted: daNhanThuong);
}
```

## Map `MaxSdkBase.AdInfo` → `AdParam` (cho mốc impression)

| AdInfo | AdParam | Ghi chú |
|---|---|---|
| `AdUnitIdentifier` | `adUnitId` | thẳng |
| `NetworkName` | `adNetwork` | network thắng |
| `Revenue` | `adRev` | USD |
| `RevenuePrecision` | `adPrecision` | giữ nguyên chuỗi MAX (`exact`/`estimated`/...) |
| `AdFormat` | `adFormatRaw` | format THÔ theo MAX — khác vai `type` (enum chuẩn SDK, tự map theo context); giữ cả hai để đối chiếu khi phân loại lệch |
| `NetworkPlacement` | `adInstanceName` | tên instance phía network dashboard |
| `WaterfallInfo.NetworkResponses` | `waterfallPosition` | TỰ TÍNH theo mẫu dưới — 1-based, lấy LOADED **đầu tiên**, không thấy thì để **null** (đừng gửi -1: field `int?` tự vắng mặt, số bịa lọt vào avg là hỏng) |
| `CreativeIdentifier` / `DspName` | `extraMeta["creative_id"]` / `extraMeta["dsp"]` | chưa có cột riêng — đổ vào `event_extra_props`, key snake_case như bên kho. RỖNG thì ĐỪNG thêm key (§H4): `if (!string.IsNullOrEmpty(adInfo.CreativeIdentifier)) extras["creative_id"] = ...` — creative_id chỉ vài network hỗ trợ (cần bật Ad Review), dsp chỉ có nghĩa với AppLovin Exchange |
| `LatencyMillis` | ❌ | chỉ dùng cho `AdLoadResultParam.loadingMs` — impression đã có `fillLatencyMs` SDK tự đo |
| `Placement` (MAX) | `adWhere` CÓ ĐIỀU KIỆN | chỉ khi game thật sự truyền placement gameplay vào MAX API; không thì để trống, SDK lấy từ `OnShown` |

```csharp
// waterfallPosition — mẫu chuẩn:
int? waterfallPosition = null;                        // không tìm thấy → null, field tự vắng mặt
var responses = adInfo.WaterfallInfo?.NetworkResponses;
if (responses != null)
    for (int i = 0; i < responses.Count; i++)
        if (responses[i].AdLoadState == MaxSdkBase.MaxAdLoadState.AdLoaded)
        {
            waterfallPosition = i + 1;                // hợp đồng 1-based
            break;                                    // người thắng = LOADED ĐẦU TIÊN theo thứ tự
        }
```

Không lấy từ AdInfo: `adCountry` ← `MaxSdk.GetSdkConfiguration().CountryCode`; `adMediation` ←
hằng `"Max"`; `adWhere`/`adWhen` ← cache từ `OnShown`; `currentLevel` ← game state. ĐỂ TRỐNG:
`hasClick` (paid event chưa biết click — `OnClicked` lo), `timeShow` (legacy — đã có
`shownDurationSec` SDK đo ở close). CẤM điền: `networkType`, `typeCount`, `adLtv`.

## Map → `AdViewParam`/`AdShowParam`/`AdCloseParam` (3 mốc mỏng)

Ba mốc này **KHÔNG map từ `AdInfo`** — request/show gọi TRƯỚC khi MAX trả gì, close thì số đo
(`shownDurationSec`, `adViewId`...) SDK tự lo. Nguồn = context gameplay + cache SDK; bảng ngắn
là đúng thiết kế, không phải thiếu.

| Field | `OnRequested` (trước `LoadAd`) | `OnShown` (trước `ShowAd`) | `OnClosed` (`OnAdHiddenEvent`) |
|---|---|---|---|
| `type` | context format của mediation | như request | như request |
| `adWhere` | ❌ để trống — preload chưa có ngữ cảnh gameplay | ✓ placement từ **GAMEPLAY** ("level_end"...), không phải từ MAX | để trống — cache tự mang từ show |
| `adWhen` | ❌ để trống — cùng lý do | ✓ lý do gameplay chiếu ("level_fail"...) | để trống — cache |
| `adMediation` | ✓ hằng `"Max"` — hạ tầng, biết từ load | để trống được — cache từ request | để trống — cache |
| `hasClick` | — | — | ❌ để trống nếu đã nối `OnClicked` (cờ tự chảy vào; truyền tường minh sẽ THẮNG cờ) |
| `adCompleted` | — | — | Rewarded: giữ `bool` từ `OnAdReceivedRewardEvent` rồi truyền vào đây (Hidden đến sau Reward). Inter: để trống — không có callback đo "xem hết", đừng đoán (§H4) |
| `extraMeta` | optional per mốc | optional | optional |
| `networkType` | ❌ CẤM — SDK tự đo | — | — |

Hai cửa lỗi: `OnLoadFailed`/`OnShowFailed` nhận `errorInfo.Code.ToString()` — load-fail lấy code
của lần fail CUỐI khi bỏ cuộc cả đợt (một lần/view), show-fail mỗi lần `OnAdDisplayFailedEvent`.

## Map → `AdLoadResultParam` (mốc load success/fail — tên cũ `AdMultiFloorParam`)

| Field | SUCCESS (`OnAdLoadedEvent`) | FAIL (`OnAdLoadFailedEvent`) |
|---|---|---|
| `adType` | context của mediation (enum SDK, không đọc từ `AdFormat`) | như success |
| `success` | `true` — **set TƯỜNG MINH, default là `false`**: quên dòng này ở callback loaded là success bị đếm nhầm thành fail, không có lỗi nào báo | `false` |
| `adUnitId` | tham số callback (≡ `adInfo.AdUnitIdentifier`) | tham số callback (`ErrorInfo` không mang unit) |
| `tier` | **BẬC giá sàn trong config** (hệ số 1.5 / 3.0 / 5.0 — mediation nào cũng biết thật). Kiểu `double?`, parse bằng `CultureInfo.InvariantCulture` (culture vi-VN đảo dấu thập phân, "1.2" thành 12). **Unit không đặt sàn → để null**, đừng điền 0: vắng-key là thứ duy nhất phân biệt "không đặt sàn" với "đặt sàn 0" | như success |
| `floor` | **GIÁ SÀN THẬT, USD/1000 impression** — CHỈ mediation biết con số thật mới điền (AdMob tự đặt sàn từng slot); MAX không biết nên **để null**. ⚠ **Cấm** nhân `tier` với doanh thu đo được rồi điền vào đây: nền nhân là giá ad vừa xem nên cùng bậc ra số khác nhau theo máy/theo ngày (công thức cũ đã bỏ, loader đo 22/09). SDK cảnh báo runtime nếu có `floor` mà không có `tier` | như success |
| `networkName` | `adInfo.NetworkName` | **để null** — chưa ai thắng, field vắng mặt, đừng điền UNKNOWN |
| `errorMess` | null | `errorInfo.Code.ToString()` — dùng **Code** chứ đừng `Message` (Message của MAX dài cả đoạn kèm waterfall detail; cụm giữ bản CUỐI làm đại diện nên cần giá trị NGẮN, phân loại được) |
| `loadingMs` | `adInfo.LatencyMillis` (cùng là `long`, gán thẳng) — MAX đo hộ thời gian của CHÍNH attempt này, khỏi stopwatch riêng; SDK cộng dồn `totalLoadingMs`, server chia `count` ra trung bình | `errorInfo.WaterfallInfo?.LatencyMillis ?? 0` — không có số thì 0, cộng dồn 0 không làm bẩn tổng |

Hai điều KHÔNG có trong bảng vì không phải field phải điền:

- `success` không lên wire — nó quyết định đếm vào `ok` hay `fail` của bản ghi unit trong
  `f_sdk_ad_load_stats`, nên bản tin không cần cột success.
- **Game không dùng MultiCall vẫn gọi `OnLoadResult`** (máy yếu/3G tắt multicall, hoặc chỉ
  1 unit): kết quả load của một unit đơn vẫn là kết quả load — chỉ khác `tier`/`floor` để trống.
  Chính nhóm máy yếu/3G là nơi telemetry no-fill/timeout giá trị nhất, và retry dày cỡ nào
  cụm cũng nén về vài dòng (`count` = trăm attempt). Đây là lý do đổi tên khỏi "MultiFloor".

### Banner: cùng dòng `OnImpression`, KHÔNG cần if bên ngoài

`OnImpression` với `type = AdType.Banner` **tự rẽ vào cụm gộp** bên trong SDK — mediation gọi
đúng một dòng cho mọi format. Hai điều phải biết:

- **Field per-impression bị RƠI ở nhánh banner** (không lỗi, chỉ bị vứt — dòng nén N impression
  không có chỗ cho field của từng cái): `waterfallPosition`, `extraMeta`, `adUnitId`,
  `adInstanceName`, `adFormatRaw`. Dòng gộp giữ: where/precision/country/network/mediation/level
  + tổng rev + `impressionCount` + `adViewId`. Đừng mất công điền mấy field kia cho banner.
- **Mốc cho banner chỉ có 2**: `OnRequested` lúc tạo instance (cụm mang `adViewId`, refresh dùng
  lại id) + `OnImpression` mỗi paid event. ĐỪNG gọi `OnShown`/`OnClicked`/`OnClosed` cho banner —
  3 event phễu theo hợp đồng chỉ dành cho inter/rewarded.

## Những dòng phải XOÁ khi chuyển sang cửa mới

| Code cũ | Vì sao xoá |
|---|---|
| `new FAdLogMediation(...).Send()` | `OnImpression` thay thế — cùng event id, chạy cả hai là ĐẾM ĐÔI doanh thu |
| `new MAdLog(...).Send()` (MultiCallMax.LogToServer) | Event `f_sdk_mo_multiple_floor_adunit` **đã bị CHẶN trên server** vì spam — còn gửi chỉ tốn băng thông. `OnLoadResult` thay thế, chỉ cho load success/fail |
| `MAdLog` loại `ADS_DISPLAYED` / `ADS_REVENUE_PAID` | Bỏ hẳn, không thay — trùng 100% với impression (mỗi lượt xem đang đẻ 3 event) |
| `MAdLog` loại `ADS_DISPLAY_FAILED` | Thay bằng `Ad.OnShowFailed` |
| Gọi `BannerLogService.Log(...)` trực tiếp | `OnImpression(AdType.Banner)` tự route vào cụm gộp |

## Toàn bộ hàm của `FalconBigDataController.Ad` (để khỏi sót khi có bản mới)

| Hàm | Dùng làm gì |
|---|---|
| `OnRequested(type, adMediation:)` | Mốc xin ad — lần đầu mỗi đợt |
| `OnShown(type, adWhere:, adWhen:)` | Mốc gọi hiển thị — chỗ khai placement |
| `OnImpression(AdParam)` | Mốc doanh thu — sau khi cộng sổ |
| `OnClicked(type)` | Người chơi bấm ad |
| `OnClosed(type, adCompleted:)` | Ad đóng |
| `OnLoadFailed(type, errorCode)` | CẢ ĐỢT load thất bại — một lần/đợt |
| `OnShowFailed(type, errorCode)` | Gọi hiển thị hỏng |
| `OnLoadResult(AdLoadResultParam)` | Kết quả load TỪNG unit (multicall hay không đều gọi) — SDK tự gộp |
| `OnMultiFloorResult(AdMultiFloorParam)` | ⚠ DEPRECATED — tên cũ của `OnLoadResult`, chuyển tiếp y hệt |
| `UpdateContext(type, adWhere:, adWhen:)` | Báo placement khi biết sớm hơn show |
| `CurrentViewId(type)` | Lấy `adViewId` gắn vào CSAdStart/CSAdFinish |
| `CurrentView(type)` | Ảnh chụp lần xem hiện tại (đọc) |

Mọi hàm gọi từ **thread nào cũng được** — BigData tự khoá. Các mốc đều có overload nhận param
object + `extraMeta` (key-value tuỳ ý → `event_extra_props`) khi cần nhét thêm dữ liệu.

SDK tự đo, KHÔNG phải truyền: `adViewId`, `networkType`, `requestToShowMs`, `fillLatencyMs`,
`shownDurationSec`, `impressionCount`/`count` (cụm banner/multi-floor).

## Vì sao "load thành công" và "lên hình thành công" không có mốc riêng

Thiết kế "log cái HIẾM mang thông tin (lỗi), suy ra cái PHỔ BIẾN (thành công)":

- Load-success xảy ra ở gần như mọi request → thêm mốc là gần GẤP ĐÔI volume stream ad mà không
  thêm thông tin: `load thành công = requests − load_error` (và multi-floor đã đếm chi tiết hơn).
- "Lên hình thành công" chính là **impression** — với inter/rewarded, paid event đến trùng lúc
  lên hình.

Số học phễu phía server:

```
fill rate              = 1 − count(load_error) / count(request)
loaded-không-dịp-chiếu = requests − load_error − shows
display fail           = shows − impressions   (nhãn show_error nói VÌ SAO)
thời gian xem thật     = close.ts − impression.ts (cùng adViewId)
floor win rate         = load_success / (load_success + load_fail)  per floor
```

## Trạng thái phía data team

- Đã ký: `f_sdk_ad_request_data` / `f_sdk_ad_show_data` / `f_sdk_ad_close_data`; `f_sdk_ads_data`
  là event sống lâu đời.
- Đã duyệt hình dạng, **chờ loader seed mapping rule** (rồi SDK mới phát hành):
  `f_sdk_ad_load_stats` (bản tin gộp dạng mảng `units[]`) và `f_sdk_ad_show_attempt`.
  Hai event cũ `f_sdk_ad_load_success` / `f_sdk_ad_load_fail` **ngừng gửi** từ cùng bản SDK.
- Mapping kênh nhãn `f_sdk_ad_view_label` (chở `load_error`/`show_error`) — gửi trước an toàn,
  server giữ raw.
- Tham chiếu sâu: `EntityLifecycle-Design.md` §4f, `DWH-EventLog-API-Spec.md` §5.11.
