## 1.3.0
Bỏ EVENT_ADD_REWARD và PlacementResolver, thay bằng EVENT_BUY_SUCCESS_ADD_RESOURCES emit thẳng config của pack
Placement giờ do nơi spawn pack set vào qua APackElement.SetPlacement, mặc định "unknown"
----------------------------------


## 1.2.7
Thêm cơ chế pick reward name trong các SO Packs Config từ 1 SO Reward Name
----------------------------------


## 1.2.6
Tối ưu tốc độ khởi tạo đầu game
----------------------------------


## 1.2.5
Update doc một chút
Chỉnh một số biến sang protected để kế thừa dễ hơn
----------------------------------


## 1.2.4
Sửa: APackElement fix lỗi k lấy được giá tiền khi iap chưa được khởi tạo
----------------------------------


## 1.2.3
Thêm opt để UIPack tự setup ở awake
----------------------------------


## 1.2.2
Trả thêm itemType là idPack mỗi khi mua xong
----------------------------------


## 1.2.1
Sửa lỗi các wrapper không clear config cũ nếu nhận config mới từ server
----------------------------------


## 1.2.0
Chỉnh sửa Event Bus khi Login
Nếu dùng module `Account` thì tải thêm module `Account4EventBus` để khi Login thành công sẽ tự động gửi CSGetAllPacksData lên
UIItemReward giờ đây sẽ lấy được reward để hiển thị kể cả cms điền chữ hoa
----------------------------------


## 1.1.9
Thêm: trong config giờ đây có thêm @param, có thể truyền bất kì biến gì vào để config thêm cho gói
----------------------------------


## 1.1.8
Thêm placement log nhận thưởng khi mua inapp
----------------------------------


## 1.1.7
- Sửa lỗi các wrapper pack k tự lấy được config local nếu k kết nối đc server
----------------------------------


## 1.1.6
Add Localize
----------------------------------


## 1.1.5
Add: thêm PlacementResolver, quản lý việc xác định placement (Home, InGame, LoseGame,...) khi mua pack
----------------------------------


## 1.1.4
Thêm: có thể override FormatHelper.FormatQuantityReward để tự chỉnh format reward
----------------------------------


## 1.1.3
Update readme require namespace
----------------------------------


## 1.1.2
- Update readme
----------------------------------


## 1.1.1
Update readme
----------------------------------
----------------------------------