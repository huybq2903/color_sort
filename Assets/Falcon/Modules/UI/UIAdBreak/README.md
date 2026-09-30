# Module UI AdBreak

## Tổng quan
- **Tên:** `UI AdBreak`
- **Giới thiệu:** UI Quản lý Show Ad Break trước khi hiện interstitial.
popup AdBreak sẽ hiển thị trước khi hiện quảng cáo Interstitial vài giây, sau đó tự động ẩn đi, rồi hiện quảng cáo Interstitial sau đó.

### 1. Cấu hình thời gian hiện popup AdBreak
- Vào File cấu hình trên menu "Falcon/Modules/ThirdParty/Ads MO settings"
- Nhập thời gian hiện popup AdBreak ở ô `Time Break`

### 2. Import Prefab UIPopup_AdBreak.prefab
- Vào Menu `Falcon/Modules/UI/AdBreak/Assets` để import `UIPopup_AdBreak.prefab`

### 3. Chỉnh sửa giao diện popup AdBreak
- Vào file `UIPopup_AdBreak.prefab` theo đường dẫn `Assets/FalconAssets/Modules/UI/UIAdBreak/Addressable/UIPopup_AdBreak.prefab` để sửa trực tiếp theo ý mình
- Module đã được sử dụng từ bên module FalconMediation, chỉ cần cấu hình thời gian và giao diện, sẽ tự động chạy.