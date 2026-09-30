# Khuôn tracking LIVE-OP — form mẫu cho mọi event (BigData ≥ 1.3.8)

Đúc từ ba event đầu (Master League · Race Event · Sky Path) và các vết sẹo của chúng. Mục tiêu:
event mới thì Request-Game chỉ còn là **phiếu điền theo form này** — dev học khuôn một lần,
visualize dựng một bộ dashboard generic, data team nhận một kiểu vocab.

Triết lý một câu: **ghi KHOẢNH KHẮC, đừng ghi trạng thái** — trạng thái ("đang tham gia",
"đã xong") là thứ server tự suy từ các khoảnh khắc, suy sai còn sửa được bằng một câu SQL;
state stamp sai trên wire là sai vĩnh viễn không dấu vết. Cần tiện `GROUP BY` thì yêu cầu
**cột enrichment phía kho** (materialize từ event stream), không đẻ param `eventXxx*` trên wire.

## 0. Phân loại mốc TRƯỚC KHI viết doc — TRẠNG THÁI vs KHOẢNH KHẮC

Bài học đắt nhất của Master League (`eligible` trói vào khoảnh-khắc-xong-chapter → kỳ cựu vô
hình, nhóm đối chứng sụp, dev làm *đúng doc sai*):

| Loại mốc | Định nghĩa | Luật bắn |
|---|---|---|
| **KHOẢNH KHẮC** | Một hành động xảy ra tại một thời điểm (bấm Start, thắng chặng, nhận thưởng) | Bắn tại chỗ, một lần |
| **TRẠNG THÁI theo chu kỳ** | Một điều kiện đúng trong suốt chu kỳ (đủ tư cách tham gia, đã unlock) | Bắn MỖI PHIÊN khi điều kiện đang đúng (kể cả đúng từ lâu) — van `OncePerCycle` tự nén về một dòng/chu kỳ |

Mỗi mốc trong phiếu điền PHẢI khai nó thuộc loại nào. Nghi ngờ = hỏi: *"user đạt điều kiện từ
THÁNG TRƯỚC thì chu kỳ này có dòng log không?"* — không có là đang trói trạng thái vào khoảnh khắc.

## 1. Khai báo event (mục 0 của mọi Request-Game)

```
Tên funnel      : event_<tên>            ← prefix event_ BẮT BUỘC với event mới (chốt 04/09):
                                           quên Register thì rơi về OncePerCycle (nâng cấp 05/09
                                           khi Repeatable nghỉ hưu — bước có cycleId được LỌC
                                           ĐÚNG luôn, thiếu cycleId van tự hạ pass-through)
                                           thay vì OnceOrdered (chết trắng).
                                           Lưới 3 lớp: prefix + Register + funnelShape tự thú.
Chu kỳ (cycleId): mùa? bảng đấu? ngày?   ← đơn vị mà "chơi lại" mở ra một vòng mới. Event cho
                                           chơi nhiều vòng trong một đợt: cycleId = VÒNG
                                           (bài Race Event — lấy id đợt là vòng 2 bị van nuốt)
Tiền tố key     : <prefix>_                ← BẮT BUỘC cho MỌI key extras/detail riêng của event
                                           (chốt 17/09): ngắn, duy nhất per event, khai ở đây
                                           (vd `league_` Master League, `race_` Race Event).
                                           Lý do: extras trải PHẲNG lên log chung theo luật
                                           ai-ghi-trước-giữ — hai event cùng gắn `stage` lên một
                                           ván level thường là event sau mất số im lặng; cùng tên
                                           khác kiểu (`stage` chữ vs số) hỏng cột kho. Key định
                                           danh cũng theo tiền tố (`race_id`, `league_id`).
                                           Không áp cho field CHUẨN của SDK (`difficulty` level,
                                           `cycleIndex`…) và giá trị (chỉ áp tên key)
Register        : Funnel.Register("event_<tên>", FunnelShape.OncePerCycle) — khởi động app,
                  khuôn ISingletonServiceReady (TrackingGuide §Funnel có class demo)
Catalog         : báo data team: tên funnel + SHAPE + các action + vocab event_when + key extras
```

## 2. Form 3 câu hỏi × 3 loại log

### 2a. Ai vào, đi tới đâu, ai về đích? → Funnel `event_<tên>` (OncePerCycle)

| Mốc | action | Loại (mục 0) | Chủ gọi | Ghi chú |
|---|---|---|---|---|
| Đủ tư cách | `"eligible"` | **TRẠNG THÁI** | thường là GAME (module không biết điều kiện) — ghi rõ trong phiếu | |
| Mở UI event | `"open"` | khoảnh khắc (lặp — van nén 1 dòng/chu kỳ) | | Mẫu số phễu "% tham gia sau khi ĐÃ XEM" = join/open — grain phễu chỉ cần "đã xem ít nhất một lần", van nén là ĐÚNG. Đọc drop-off eligible→open (thèm mở không) và open→join (UI thuyết phục không) |
| Tham gia | `FFunnelAction.Join` | khoảnh khắc | | ĐÚNG hằng số — SDK ghi sổ ngày join (funnelDay/LastJoinDay). **Điền bộ meta chu kỳ TẠI ĐÂY**: `cycleIndex` + `cycleStartTs`/`cycleEndTs` (epoch ms UTC, từ config — đúng số UI countdown vẽ; server tự tính countdown-lúc-join, khỏi dim-table) |
| Qua chặng N | `FFunnelAction.Milestone`, priority = N | khoảnh khắc | | extras: số đo thắng-sát-nút nếu có (bài bot-tuning) — `<prefix>_user_time_sec`… |
| Về đích | `FFunnelAction.Complete` | khoảnh khắc | | extras: `<prefix>_rank` (vắng mặt nếu chưa biết) |
| Nhận thưởng | `FFunnelAction.Claim` | khoảnh khắc | | |

Cần số ĐẾM exposure (tổng impression theo NGÀY, CTR) → KHÔNG đi funnel (funnel là MỐC, không
phải máy đếm) — dùng `Ui.OnImpression/OnClicked(surfaceId)` (BigData ≥ 1.3.9): event
`f_sdk_ui_impression` dạng CỤM (surface × action → count, flush lúc pause) — van gộp BẮT BUỘC,
per-lần-hiện không có đường lên wire (chốt owner 05/09: 5–30 lần/user/ngày × surface × DAU =
hàng triệu dòng, đúng họ án mo_multiple_floor). Phần lớn metric "% sau khi xem" chỉ cần bước
`open`; chỉ mở máy đếm khi GD đòi đích danh tổng-số/CTR.

### 2b. Từng ván chơi thế nào? → Level thường + extras

Ván trong event = `Level.OnStart/OnPass/OnFail` như mọi ván, extras chỉ chở **số game PHẢI
đúng để chạy event** (sai nó là event sai → gameplay chính là bộ test): id event/vòng,
`<prefix>_attempt` (lần thử thứ mấy), `<prefix>_streak_pos`/`<prefix>_round`,
`<prefix>_retry_reason` (chỉ ván đầu attempt ≥ 2).
KHÔNG funnel step per ván (double coverage — phễu ván JOIN qua extras/playTurnId).

### 2c. Tiền vào ra vì event? → Resource, `resourceWhen = "event_<tên>"`

Luật chung: `amount` = số **THỰC nhận** (đã nhân x2/boost — server không biết ai có boost);
`<prefix>_multiplier`/`<prefix>_stage`/`<prefix>_step` đi `detail`; các vế cùng một mẻ claim chung `exchangeId`;
**một giao dịch MỘT đường log** (bài log-đôi rank reward ML).

Hai chế độ tích hợp — chọn theo chỗ đứng của code grant:

- **Chế độ A — event tự cộng sổ**: gọi thẳng `Resource.OnEarned/OnSpent(ResourceParam)` với
  đầy đủ when/where/detail/exchangeId. Đơn giản nhất, dùng khi event không đi qua hệ grant chung.
- **Chế độ B — grant qua hệ chung** (EventRewardGranter/ResourceCollector — hệ này TỰ log một
  dòng thô): KHÔNG log thêm dòng thứ hai (log đôi). Đường đúng là **context đi CÙNG lời gọi,
  xuyên chuỗi tường minh**: event dựng một `ResourceParam` chở
  `resourceWhen`/`resourceWhere`/`exchangeId`/`detail` và truyền vào tham số `context` của
  `Grant(...)` — chuỗi module chuyển tiếp xuống, `ExtendResourceLog` vốn GIỮ nguyên các field
  đó khi nhận param. Ai gọi người đó chịu — nhiều bên phát tài nguyên song song, cascade kiểu
  gì cũng không lây nhãn. (Chuỗi cần hai tham số optional chuyển tiếp phía EventsCore/GameData
  — xem 2 thư `Request-EventsCore-GrantContext` + `Request-GameData-ResourceParam`; chưa có thì tạm chấp nhận dòng thô,
  ĐỪNG log dòng thứ hai.)
  <br/>⚠ Phương án AMBIENT SCOPE ("bọc grant trong using, decorator dán ngữ cảnh lên log đi
  qua trong scope") đã đề xuất và **BÁC** (05/09): `GameEvent.Emit` đồng bộ + listener của
  `ResourceCollector` bắn TRƯỚC khi log → một lời grant kích cascade (piggy bank, quest tự
  thưởng...) và log của bên khác lọt vào scope, dán nhầm nguyên nhân. Scope thời-gian dù ngắn
  vẫn là stamp thời-gian — ĐỒNG THỜI ≠ NGUYÊN NHÂN không có ngoại lệ cửa-sổ-ngắn.

Sink trong ván event: log như thường — `playTurnId` tự nối, cấm stamp `event_*` lên chi tiêu
shop chung (ĐỒNG THỜI ≠ NGUYÊN NHÂN; ngoại lệ duy nhất: gói bày TRONG UI event → `where` của
Iap đặt placement event).

## 3. Bảng ĐỪNG-LÀM (mỗi dòng một vết sẹo thật)

| Đừng | Vết sẹo gốc |
|---|---|
| State-param `eventXxxJoining/Finished...` trên wire | Đề xuất Sky Path — derive được, sai im lặng, vocab nở theo số event; cần tiện thì cột enrichment kho |
| Funnel step per ván | Double coverage với level |
| `cycleId` = id đợt khi event cho chơi nhiều vòng | Vòng 2 bị van OncePerCycle nuốt (Race Event) |
| Log qua CẢ hai đường cho một mẻ thưởng | Log đôi rank reward ML — DWH đếm đôi tiền |
| Trói mốc trạng thái vào khoảnh khắc một-lần-đời | `eligible` ML — nhóm đối chứng sụp |
| Hàm tracking không ghi chủ gọi | `OnEligible` viết xong mồ côi, zero call site |
| Log cosmetic ăn theo tiến độ (badge/skin) | Biết tiến độ là tự biết — log là thừa |
| `amount` chưa nhân boost / đơn vị tuỳ hứng (Lives: phút vs "lần") | Server không biết ai có x2; cột thành nồi lẩu |
| Đếm hộ server (CTR, retry, percentile ở client) | Client chở sự kiện, mọi phép tính là việc kho |
| Ambient scope / ThreadStatic context quanh grant | GrantScope bị bác 05/09 — GameEvent đồng bộ cascade grant của bên khác vào scope, dán nhầm nguyên nhân; context phải đi CÙNG lời gọi |
| Key extras/detail không tiền tố (`stage`, `round`, `attempt`…) | Master League + Race cùng dùng `stage` (chữ vs số) — đụng cột kho; Race gắn lên level thường nơi event khác cũng gắn → ai-ghi-trước-giữ, mất số im lặng |
| Exposure per-lần-hiện raw lên wire | Volume không chặn trên (họ án mo_multiple_floor) — Ui API gộp cụm là đường DUY NHẤT cho đếm exposure |

## 4. Checklist nghiệm thu (trước khi ship event)

1. Mọi hàm tracking có **call site** (grep — bài OnEligible mồ côi).
2. Console QA sạch warning BigData (van funnel, turn, van xoay id... đều tự cảnh báo).
3. `funnelShape` trên log khớp shape đã đăng ký catalog (quên Register lộ ngay tại đây).
4. Một mẻ thưởng → đếm số dòng resource trong QA = số vế, không hơn (soi log đôi).
5. User "đủ điều kiện từ lâu" đăng nhập giữa mùa → có dòng `eligible` của mùa (soi mục 0).
6. Mọi key extras/detail riêng của event mang tiền tố đã khai (grep `KEY_*` của module).
7. Data team đã nhận: tên funnel + shape + action + vocab `event_when` + key extras.

## 5. Data team đọc gì (công thức generic — dashboard dựng MỘT lần)

```
tham gia / chu kỳ        = distinct user có Join per cycleId; %DAU = chia DAU
pass chặng N             = distinct user có Milestone priority N (per user) / đếm mọi dòng (per lượt)
retry                    = max(<prefix>_attempt) − 1 per (user, vòng)
drop-vì-bí               = max(<prefix>_attempt) cuối trước khi user biến mất khỏi event
source từ event          = SUM(amount) WHERE event_when='event_<tên>' GROUP BY currency, detail.<prefix>_*
sink trong ván event     = resource sink JOIN playTurnId → ván có extras event
countdown bucket         = cycleEndTs − ts(Join)   (field chuẩn, không cần lịch ops)
event-retention ngày N   = user có log event ở ngày join+N / user join (funnelDay/league_start_day)
so sánh join vs không    = tệp eligible ∖ join làm đối chứng (CÙNG tư cách — non-joiner toàn
                           server là selection bias); trước/sau = cắt theo ts(Join)
cột "đang tham gia"      = ENRICHMENT phía kho từ cặp Join → Complete/end (yêu cầu chính thức
                           của khuôn — thay thế mọi đề xuất state-param)
```

## 6. Ba tầng tích hợp — điền phiếu theo tầng

| Tầng | Gồm | Khi nào |
|---|---|---|
| **Tối thiểu** (3 điểm chạm) | extras trên ván + OnEarned khi phát thưởng (+ Join nếu có nút tham gia) | Event nhẹ, metric đếm theo ngày — Sky Path là bằng chứng đủ 6/6 metric |
| **Chuẩn** | + funnel đủ 4-5 mốc + bộ meta chu kỳ | Event có phễu tham gia/đối chứng — Master League |
| **Đầy đủ** | + event exposure đếm (`f_sdk_ui_impression` — chốt khi cần) + số đo bot/thắng-sát-nút | Event cần tuning + CTR per-lần-hiện — Race Event |

Điền phiếu từ tầng thấp lên: mỗi mốc thêm phải trả lời được "metric nào cần nó?" — không có
câu trả lời thì đừng thêm.
