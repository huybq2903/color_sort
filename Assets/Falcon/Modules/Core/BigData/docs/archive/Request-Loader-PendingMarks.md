# Gửi loader — MỐC ĐỌC chính thức: BigData 1.3.8 publish 05/09 (2026-09-05)

Trả nợ mốc đã hứa trong vòng thư ad_view_id:

1. **Fix xoay `ad_view_id` nằm trong BigData 1.3.8, publish 05/09/2026** — kho gắn mốc đọc tại
   đây: game lên bản chứa SDK ≥ 1.3.8 thì non-banner không thể có 2 impression chung id nữa,
   bất kể mediation tích hợp kiểu gì. Vệt nhận dạng sau mốc như đã ký: impression không match
   request row + `fill_latency_ms` vắng = mediation còn sai nhịp `OnRequested` (DQ per game/bản
   của các bạn đo được thẳng).
2. Cùng chuyến 1.3.8 (đã ký/đã báo ở thư FunnelCycleMeta): 4 field funnel mới
   (`cycleIndex`/`cycleStartTs`/`cycleEndTs`/`funnelShape`), biên turn level ĐỔI (terminal đóng
   lượt), rename funnel `event_master_league`. Chưa ack thì nhắc giúp — thư đó vẫn chờ.
3. **Còn nợ MỘT mốc**: số bản module Mediation sửa nhịp `OnRequested` (việc của owner
   Mediation) — có là báo ngay bằng thư chót rồi xoá cả file này theo lệ.

---
*Bản lưu ở git history — như lệ.*
