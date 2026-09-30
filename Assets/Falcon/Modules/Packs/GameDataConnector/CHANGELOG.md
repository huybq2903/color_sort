## 1.1.1
Truyền ResourceParam vào ResourceAdd: resourceWhen = iap, resourceWhere = placement lúc mua, transactionId
----------------------------------


## 1.1.0
Nhận config pack qua EVENT_BUY_SUCCESS_ADD_RESOURCES thay cho EVENT_ADD_REWARD
Thêm transaction_id và pack vào detail của ResourceCollector.ResourceAdd
transaction_id lấy theo platform: Android bóc orderId từ receipt, còn lại dùng Product.transactionID
----------------------------------


## 1.0.2
Thêm placement khi gọi ResourceCollector.Add
----------------------------------


## 1.0.1
- Change asmdef
----------------------------------