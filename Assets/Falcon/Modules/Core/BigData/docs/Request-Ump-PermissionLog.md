# Yêu cầu: log trạng thái ATT & consent — gửi owner module `Core/ThirdParty/Ump`

> **Người gửi:** owner module `Core/BigData`
> **Người nhận:** owner module `Core/ThirdParty/Ump` (`FalconUMP`, v1.2.7)
> **Căn cứ:** hợp đồng `sdk-contract-vnext.md` §D8 (viết lại 2026-08-10)
> **Việc cần làm:** thêm 2 lời gọi. Không đổi luồng, không đổi UI, không thêm thư viện.

## Vì sao

Hai trạng thái này quyết định trực tiếp doanh thu ads mà DWH **hiện không có**:

| Trạng thái | Thiếu nó thì sao |
|---|---|
| ATT (iOS) | eCPM và match-rate attribution phụ thuộc thẳng vào nó. So doanh thu ads giữa hai cohort mà không biết tỉ lệ ATT thì rất có thể đang đo lệch tỉ lệ ATT chứ không phải đo cái mình tưởng. |
| Đồng ý quảng cáo (GDPR/UMP) | Y như trên, cho thị trường EU. |

Module Ump là nơi **duy nhất** biết hai giá trị này — SDK-core không tự đọc được.

## API

```csharp
using Falcon.Modules.Core.BigData;

FalconBigDataController.App.ReportAttStatus(AttStatus.Denied);
FalconBigDataController.App.ReportAdsConsent(granted: true);
```

- Gọi **thoải mái**, kể cả mỗi lần khởi động: SDK nhớ giá trị đã gửi, **báo trùng không đẻ log**.
  Nhờ vậy bắt được cả ca user tự đổi trong Settings của máy rồi quay lại game.
- Log đi vào hàng đợi rồi mới gửi theo batch, nên **không cần chờ BigData init xong**; gọi lúc nào
  cũng được, không chặn luồng.
- Mỗi trạng thái là một event riêng (`f_sdk_permission_att`, `f_sdk_permission_ads_consent`).

## Chỗ gọi đề xuất

### 1. Consent — trong callback của `LoadAndShowConsentFormIfRequired`

Ngay chỗ đang tính `hasConsent` rồi `SaveLoadHandler.Save(FALCON_UMP_HAS_CONSENT, ...)`:

```csharp
var consent = ConsentInformation.ConsentStatus;
var isInEurope = consent == ConsentStatus.Required;
var hasConsent = consent == ConsentStatus.Obtained;
SaveLoadHandler.Save(FALCON_UMP_IS_IN_EUROPE, isInEurope);
SaveLoadHandler.Save(FALCON_UMP_HAS_CONSENT, hasConsent);

// THÊM: chỉ báo khi consent THỰC SỰ có áp dụng
if (consent is ConsentStatus.Required or ConsentStatus.Obtained)
    FalconBigDataController.App.ReportAdsConsent(hasConsent);
```

⚠ **Đừng gửi khi `ConsentStatus.NotRequired`.** User ngoài EU không hề "từ chối" — họ không được
hỏi. Map `NotRequired → denied` sẽ nhuộm toàn bộ fleet ngoài EU thành "denied" và giết luôn ý
nghĩa của con số. Không áp dụng thì để vắng mặt (§H4: không đo được thì đừng điền).

Tương tự với `ConsentStatus.Unknown` (chưa update xong / lỗi) — chưa biết thì chưa gửi.

### 2. ATT — sau khi trạng thái đã xác định

Trong `WaitUntilDetermined()`, ngay trước `GameEvent<bool>.Emit(FALCON_UMP_COMPLETE)`:

```csharp
FalconBigDataController.App.ReportAttStatus(ToFalconAtt(
    ATTrackingStatusBinding.GetAuthorizationTrackingStatus()));
```

Và gọi **thêm một lần lúc khởi động** (trước cả prompt, trong `OnShowPopupATT`) để bắt ca user đã
trả lời từ phiên trước hoặc vừa đổi trong Settings:

```csharp
private static AttStatus ToFalconAtt(ATTrackingStatusBinding.AuthorizationTrackingStatus status)
{
    return status switch
    {
        ATTrackingStatusBinding.AuthorizationTrackingStatus.AUTHORIZED => AttStatus.Authorized,
        ATTrackingStatusBinding.AuthorizationTrackingStatus.DENIED => AttStatus.Denied,
        ATTrackingStatusBinding.AuthorizationTrackingStatus.RESTRICTED => AttStatus.Restricted,
        _ => AttStatus.NotDetermined
    };
}
```

⚠ Chỉ gọi trong nhánh `UNITY_IOS` — Android không có ATT, gửi `not_determined` cho Android là bịa
một trạng thái không tồn tại.

⚠ `RESTRICTED` **không phải** `DENIED`: nó là bị chặn ở tầng thiết bị (parental control, MDM), hỏi
cũng không hiện prompt. Gộp hai cái làm một là mất đúng nhóm user không bao giờ opt-in được.

## Những gì KHÔNG cần làm

- **Không** dùng `FPropertyLog` / `property_data`. Đó là log di sản thời hệ thống còn sơ khai, dùng
  sinh biểu đồ động (dấu vết còn nguyên ở field `priority` = "thứ tự step"); phía server nó thành
  event `f_sdk_property`. Mượn kênh đó là trộn ngữ nghĩa với dữ liệu biểu đồ đời cũ. Hợp đồng §D8
  đã bỏ hướng này ngày 2026-08-10.
- **Không** tự cache/so sánh giá trị cũ để tránh gửi trùng — SDK làm rồi.
- **Không** đổi luồng UMP hay thứ tự prompt. Hai lời gọi này chỉ đọc trạng thái đã có.

## Nghiệm thu

Không có bước "bật công tắc" nào. Event mới xuất hiện trên prod → DQ của loader tự đếm → coverage
per game tự cập nhật. Kiểm nhanh bằng log editor: `AnalyticLogger` in ra `f_sdk_permission_att:{...}`
đúng một lần cho mỗi lần đổi giá trị.
