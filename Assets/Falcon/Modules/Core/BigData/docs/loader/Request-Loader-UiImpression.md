# Gửi loader — ký event mới `f_sdk_ui_impression` (BigData 1.3.9, bản tin CỤM)

Máy đếm exposure UI cho live-op (metric "tổng impression + CTR theo ngày" của Race Event, và
mọi event sau). Grain per-lần-hiện **cấm lên wire** từ thiết kế (volume không chặn trên — cùng
án `mo_multiple_floor`), nên event này sinh ra đã là **CỤM**: client gộp theo
(surface × action), flush mỗi app pause.

## Schema

| Field | Kiểu | Nghĩa |
|---|---|---|
| `surfaceId` | string | Bề mặt UI ("race_event_entry", "race_event_popup"…) — vocab per game/event, tên ổn định |
| `uiAction` | string | `impression` / `click` — vocab hằng số phía client |
| `count` | int | Số lần đã gộp trong dòng — **đếm bằng `sum(count)`, không phải `count(*)`** |
| extras (flatten) | — | Ngữ cảnh BẤT BIẾN của surface trong stretch (vd `race_id`) — là bản của LẦN ĐẦU trong cụm |

## Luật đọc

- `CTR theo ngày = sum(count | uiAction='click') / sum(count | uiAction='impression')`
  per `surfaceId` per ngày. Client chỉ NÉN, không tính hộ — mọi phép chia phía các bạn.
- Một stretch có thể ra nhiều dòng cùng (surface × action) khi van xả sớm (trần 32 cụm) —
  cứ `sum(count)`, đừng dedupe.
- Cụm KHÔNG persist qua kill — stretch bị kill cứng mất cụm dở (telemetry exposure, chấp nhận
  — cùng phán quyết với cụm multi-floor đã ký).

## Việc phía các bạn

1. Ack event id `f_sdk_ui_impression` + 2 giá trị `uiAction`.
2. Ghi luật `sum(count)` + ngữ nghĩa extras-lần-đầu vào catalog.

---
*File xoá sau khi ack, bản lưu ở git history — như lệ.*
