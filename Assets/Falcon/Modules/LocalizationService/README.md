# Module Localization Service

## Tổng quan
- **Tên:** `Localization Service`
- **Giới thiệu:** Wrapper của I2Localize, tiện cho việc thay đổi module localize sau này. Hỗ trợ thêm vài tiện ích cho việc Localize các module theo quy trình chuẩn của Falcon.
- **Các thành phần chính:**
    -   `FalconLocManager.cs`: Chứa các API.
    -   `AutoI2TermBinder.cs`: Thay thể cho các component I2Localize.
    -   Cửa sổ merge term.

## Quick Start

- Sử dụng Component `AutoI2TermBinder` thay cho các component I2Localize (gắn vào text). Component này hỗ trợ tự động tạo term hàng loạt (Select nhiều object chứa TMP cùng lúc và bấm tạo term là được).
- Ngoài cách chọn từng TMP ở trên ra, bạn có thể chọn nhiều prefab cùng lúc, sau đó chọn `Falcon/Modules/LocalizationService/Localize TMPs in Selecteds(Prefabs and Folders) (skip DoNotLocalize in object's name)` để localize toàn bộ TMP trong đó (ngoại trừ các object trong tên có `DoNotLocalize`, hoặc dịch xong không thấy thay đổi)
- Bạn có thể bấm preview để thử Add `I2Localize`. Thực tế không cần add `I2Localize` vào trước vì lúc runtime tôi tự add cho bạn rồi.
- Tôi đã gom lại các API chính ở I2 vào `FalconLocManager.cs`. Bạn gọi API ở đây nhé.
- `Falcon/Modules/LocalizationService/LanguageSourceAsset Term Merger` để mở cửa sổ merge LanguageSourceAsset. Tác dụng dùng để Add các term của module vào LanguageSourceAsset tổng của bạn.

## Danh sách đầy đủ API
- Xem `FalconLocManager.cs`.

## Chi tiết
Cơ chế tạo term tự động hoạt động như sau:
- Dựa vào địa chỉ object của bạn, tự Detect LanguageSourceAsset mặc định (bạn có thể chủ động thay).
- Dựa vào địa chỉ LanguageSourceAsset, xác định module đó là module nào (VD Clan), và đặt làm tiền tố (Term sẽ có tiền tố Clan/)
- Dựa vào string trong TMP, đảm bảo string đó là tiếng Anh, tạo hậu tố của term và dịch ra các ngôn ngữ khác bằng string đó. 
- Các lý do không dịch: dịch ra 3 ngôn ngữ zh-CN, zh-TW, es không thấy thay đổi gì.

Cơ chế tạo term hàng loạt hoạt động như sau:
- Duyệt qua tất cả TMP của prefab.
- Các lý do không dịch: chứa `DoNotLocalize` trong tên, hoặc đã có sẵn component `AutoI2TermBinder`, hoặc dịch ra 3 ngôn ngữ zh-CN, zh-TW, es không thấy thay đổi gì.
- Tôi đã nghĩ đủ cách để tự detect và lọc ra các TMP không cần Localize (set trong code) nhưng có quá nhiều trường hợp biên và fuk tạp => Detect bằng `DoNotLocalize` cho dễ. Sau này nếu TMP public thêm vài event nữa thì tôi sẽ xem xét lại, hiện tại tù quá.

## Lỗi thường gặp