## 1.1.6
- Sync SaveLoadLibPool with IDataPool.TrySync()
----------------------------------


## 1.1.5
- Thêm cơ chế opt-in refetch config sau MMP init: game khai báo subclass ARefetchConfigAfterMmp (1 dòng) → sau event falcon_mmp_started SDK tự TryFetch lần nữa để server lọc config theo attribution; kèm doc RefetchConfigAfterMmp.md với cảnh báo CHỈ an toàn cho config values, KHÔNG cho A/B testing; module thêm dependency falcon.helpers.eventbus
- Thêm test assembly đầu tiên cho module (ConfigRefetchAfterMmpTests — 5 case qua 3 seam virtual của service)
----------------------------------


## 1.1.4
- Thêm TryFetch() cho phép fetch lại remote config theo yêu cầu (FConfigInitService/FConfigController/FConfig); single-flight — chỉ 1 fetch chạy tại 1 thời điểm, gọi song song trả về false
- Sửa README (vi+en): namespace IFalconConfig, MySingleton, mô tả đúng cơ chế fetch 1 lần/phiên + TryFetch
----------------------------------


## 1.1.3
- Chuyển trạng thái init sau khi gọi callback
----------------------------------


## 1.1.2
- Update FConfigInitService
----------------------------------


## 1.1.1
- Không update remoteConfig khi không có mạng
----------------------------------


## 1.1.0
- Thay đổi theo cấu trúc Devkit mới
----------------------------------


## 1.0.9
- Disable FDataPool
----------------------------------


## 1.0.8
- Dùng repeatAction để đồng bộ SaveLoadLibPool mỗi 5p
----------------------------------


## 1.0.7
- Chuyển SaveLoadLibPool tới folder Config
- Trừu tượng hóa ConfigRepo với IFConfigRepository để hỗ trợ việc thay thế
- Thêm creator header cho 1 số file còn thiếu
----------------------------------


## 1.0.6
Thêm class FConfig như 1 cơ chế để thực hiện lấy config try catch
----------------------------------


## 1.0.5
- Thêm FAbTestCustomInfo để inject các thông tin về abTest vào user param
- Chuyển FConfigInitService sang dùng FCentralUserParamService chứ không tạo user param độc lập nữa
----------------------------------


## 1.0.4
- update lại SaveLoadLibPool do không implement ITerminal nên không lưu dữ liệu
- update lại FConfigInitService để try catch lại việc gọi callback
----------------------------------


## 1.0.3
*format lại REAME.md theo template chung
*thêm author doc vào 1 số file còn thiếu
----------------------------------