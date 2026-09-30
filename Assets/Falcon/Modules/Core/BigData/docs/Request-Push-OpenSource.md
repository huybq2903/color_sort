# Yêu cầu cập nhật — module Push/Deeplink ↔ BigData 1.3.2 (`open_source` §B4)

Gửi owner module Push notification + deeplink. BigData 1.3.2 có event `f_sdk_app_open_data` cho
mỗi lần app vào foreground, kèm field `openSource` ("mở app TỪ ĐÂU") — nhưng Unity không đọc được
intent/launchOptions, nên **không ai báo thì field vắng mặt** và câu "push có kéo người chơi quay
lại không" tiếp tục không trả lời được.

## Việc cần làm — một dòng cho mỗi đường vào

| Đường vào | Gọi gì |
|---|---|
| App mở/resume từ notification | `FalconBigDataController.App.ReportOpenSource(OpenSource.Push, pushCampaignId: campaignId)` |
| App mở từ deeplink | `FalconBigDataController.App.ReportOpenSource(OpenSource.Deeplink)` |
| Widget (nếu có) | `FalconBigDataController.App.ReportOpenSource(OpenSource.Widget)` |
| Metadata thêm của lần mở (message id, deep link đích...) | tham số thứ ba: `ReportOpenSource(OpenSource.Push, campaignId, extraMeta: new() { ["message_id"] = msgId })` — server lưu `event_extra_props`; extras đi **cùng chuyến** với nguồn (cùng TTL 10s, nguồn quá hạn bị vứt thì extras vứt cùng) |

- Gọi **càng sớm càng tốt** trong luồng xử lý mở — SDK giữ giá trị cho đúng lần foreground hiện
  tại. KHÔNG ai báo thì field **vắng mặt** khỏi payload — SDK cố ý không mặc định `icon`, vì
  "mở từ icon" mặc định chỉ đang đếm những game chưa wire.
- `pushCampaignId` là id campaign trong hệ thống push của mình — server join ngược về campaign.
- Gọi trùng vô hại. Gọi MUỘN (sau khi bản tin app_open đã bắn): SDK cảnh báo và lần mở đó không
  mang nguồn — không dán sang lần mở sau (dán là số liệu sai); sửa bằng cách gọi sớm hơn.
- KHÔNG gọi khi không chắc nguồn — sai đắt hơn thiếu. Thread nào cũng được.

Tham chiếu: `DWH-EventLog-API-Spec.md` §5.14; ATT/consent của UMP xem file riêng
`Request-Ump-PermissionLog.md`.
