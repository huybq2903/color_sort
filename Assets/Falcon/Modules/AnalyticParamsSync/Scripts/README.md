# Module AnalyticParamsSync

## Tổng quan
- **Tên:** `Analytic Params Sync`
- **Giới thiệu:** Phụ trách việc đồng bộ các tham số Analytic liên quan tới người chơi và lưu lên server
## Quick Start
- Không phải cài đặt hay code gì thêm, chỉ với việc có mặt module này trong code là các tham số đã được đồng bộ.
---

## Ghi chú

* Module này chủ yếu lo việc lưu và đồng các tham số trong Devkit lên server
* Các tham số phân tích trước đó vốn được lưu ở local nên sẽ bị mất khi user xóa game cài lại. Module này lưu dữ liệu (phần chưa có) lên server để khắc phục tình trạng này.
* Phần đồng bộ này được tách ra làm 1 module riêng do:
  * Không phù hợp để đặt vào Devkit do liên đới rất nhiều module khác
  * Đề phòng nhu cầu cần đồng bộ các dữ liệu khác trong tương lai
  * Hỗ trợ tách phần đồng bộ này để sử dụng 1 phần hệ thống module cho các game cũ (cấu trúc dữ liệu lưu lên server không giống như trong framework)
* Các Module sử dụng các tham số trong Devkit (bao gồm):
  * Bigdata
  * Remote config
  * CDN
* Các module cung cấp tham số để đồng bộ vào Devkit (bao gồm):
  * Mediation(lấy các thông tin về việc xem quảng cáo của người dùng)
  * InAppPurchase(lấy các thông tin về việc nạp iap của người dùng)
  * AccountData(lấy code người dùng, đồng thời dùng để đẩy dữ liệu lên server)
  * Level(lấy thông tin level của người dùng)
  * Network(đồng bộ thời gian cho Devkit theo thời gian lấy từ game server)