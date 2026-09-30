## 2.1.0
Sửa lại cơ chế loop. Nếu class nào sử dụng LoopListViewManager
cần kiểm tra lại vì có thể sẽ null trong trường hợp _maxItemPerRow = 1.
----------------------------------


## 2.0.0
** Lưu ý: Đây là bản cập nhật lớn. Prefab nào đang sử dụng LoopListViewManager
sẽ bị null _loopListView và số _maxItemPerRow về mặc định.
Nên sau khi update hãy kiểm tra lại toàn bộ prefab đó và sửa lại.

- Thêm và cập nhật một số chức năng. Xem chi tiết trong class LoopListViewManager.

- Sửa một số lỗi và tối ưu.
----------------------------------