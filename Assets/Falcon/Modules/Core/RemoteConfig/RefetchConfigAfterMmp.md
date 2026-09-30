# Cập nhật Remote Config sau khi MMP init (opt-in)

## Bối cảnh

Mặc định, remote config **fetch 1 lần lúc khởi động, chạy độc lập với MMP** — cố ý như vậy
để config về sớm nhất có thể, không bị chặn bởi attribution (MMP init + conversion data có
thể mất nhiều giây, thậm chí fail). Đổi lại: bản fetch đầu tiên **chưa mang thông tin MMP**,
nên server không lọc/segment config theo nguồn UA (media source, campaign...) được.

Một số team cần config lọc theo MMP. Vì mỗi team nhu cầu khác nhau (có team cần sớm, có team
cần đúng segment), SDK **không bật mặc định** mà cung cấp cơ chế opt-in để game tự quyết.

## Cách dùng — đúng 1 dòng

Khai báo một subclass rỗng của `ARefetchConfigAfterMmp` ở bất kỳ đâu trong project game:

```csharp
public class MyConfigRefetchAfterMmp : ARefetchConfigAfterMmp { }
```

Xong. Singleton container tự phát hiện subclass → sau khi MMP init xong (event
`falcon_mmp_started` từ module Mmp), SDK tự `TryFetch()` config **một lần nữa**. Bản fetch
này mang user params mới nhất (gồm các param attribution mà MMP đã đóng góp vào central user
params) → server lọc theo MMP được.

Muốn nhận biết config đã cập nhật để áp dụng lại:

```csharp
FConfigController.Instance.OnUpdateFromNet += () =>
{
    var cfg = FConfigController.Instance.Config<MyGameConfig>();
    // ... áp dụng giá trị mới (tự quyết thời điểm áp: ngay / màn kế tiếp)
};
```

Không subclass = không có listener nào được đăng ký, hành vi mặc định giữ nguyên 100%.

## Cơ chế bên dưới

```
MMP init xong ──event──► đợi Init phase xong (tránh đụng lần fetch đầu, single-flight)
                              └─► FConfigInitService.TryFetch()
                                       └─ thành công → repository cập nhật → OnUpdateFromNet
```

- `TryFetch` là single-flight: nếu trùng lúc có fetch khác đang chạy sẽ trả `false`
  (có warning trong log dev).
- Nếu game cần đẩy THÊM param riêng vào request config (ngoài những gì MMP tự đóng góp):
  implement `IFCustomInfoRepository` — param sẽ vào central user params và đi theo mọi
  request config + mọi log.

## ⚠⚠ CẢNH BÁO QUAN TRỌNG: an toàn cho CONFIG VALUES — KHÔNG an toàn cho A/B TESTING

Cơ chế này **chỉ dùng để lấy giá trị config** (remote tuning, feature flag, content...).
**TUYỆT ĐỐI không dựa vào nó cho A/B testing**, vì:

1. **User đã exposed với variant của bản fetch đầu.** Phiên chơi đã chạy vài giây/phút với
   config chưa lọc — user có thể đã thấy UI, hưởng balance của variant A. Refetch đổi sang
   variant B **giữa phiên** = exposure bị nhiễm, số liệu experiment sai không cứu được.
2. **Log đầu phiên đã stamp variant cũ.** `abTestingValue/abTestingVariable` được bơm vào
   central user params và đóng lên mọi log từ đầu phiên — refetch đổi assignment thì log
   đầu phiên mang variant A, cuối phiên mang variant B → DWH phân tích theo variant bị lẫn
   ngay trong một session.
3. **Segment theo MMP luôn trễ hơn exposure với user mới.** Install mới: attribution chỉ về
   sau vài giây → những khoảnh khắc đầu (first open, tutorial...) LUÔN chạy config chưa lọc.
   Thí nghiệm nhắm theo nguồn UA mà đo các khoảnh khắc đó là đo sai theo thiết kế.

**Nếu cần A/B theo segment MMP**, đó là bài toán thiết kế experiment với team data, không
phải bài toán client refetch. Hai hướng đúng: (a) server-side tự join attribution theo device
để assignment đúng ngay từ fetch ĐẦU của phiên sau; (b) chấp nhận experiment chỉ tính từ
session thứ 2 trở đi (variant đã ổn định từ đầu phiên). Bàn với team loader trước khi làm.

## Trade-off cần biết trước khi opt-in

- Thêm 1 request config mỗi phiên (sau MMP init).
- Giá trị config có thể **đổi giữa phiên** — game phải tự quyết thời điểm áp dụng
  (áp ngay hay đợi màn kế tiếp) qua `OnUpdateFromNet`; đừng đọc config một lần lúc boot
  rồi cache cứng nếu đã opt-in cơ chế này.
- Cần module Mmp có mặt trong project (nguồn phát event `falcon_mmp_started`); không có
  Mmp thì subclass vô hại nhưng không có gì chạy.
