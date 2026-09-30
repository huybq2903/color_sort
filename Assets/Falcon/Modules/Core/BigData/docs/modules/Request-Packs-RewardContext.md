# Yêu cầu Packs (Core + GameDataConnector) — chở ngữ cảnh log khi cộng quà pack

BigData cần thêm tham số trên log resource (`resourceWhen` / `resourceWhere` / `detail` /
`transactionId`) cho nội dung pack đã mua — pack muốn truyền nhưng đang bị **blocking**: payload
`EVENT_ADD_REWARD` không có chỗ chở ngữ cảnh. Mọi thay đổi **additive, default `null`**: pack cũ
không sửa một chữ, pack mới mở đường.

## 1. `APackElement` (Packs Core) — thêm hook + payload mới

```csharp
// Hook cho pack con khai ngữ cảnh log. Mặc định null = hành vi như cũ.
protected virtual ResourceParam GetRewardContext() => null;
```

Trong `OnBuySuccess()`, phát thêm **một** payload chở context (giữ nguyên 2 payload đang có):

```csharp
var ctx = GetRewardContext();
var ls3 = new (string name, int amount, string data, string where, ResourceParam context)[n];
for (var i = 0; i < n; i++)
    ls3[i] = (_config.rewards[i].name, _config.rewards[i].amount, _config.rewards[i].data,
              PlacementResolver.Get(), ctx);

GameEvent<(string name, int amount, string data, string where, ResourceParam context)[]>
    .Emit(PacksConstant.EVENT_ADD_REWARD, ls3);
```

`ResourceParam` nằm trong `Falcon.Modules.Core.BigData` — Packs Core thêm reference asmdef tới
BigData runtime.

## 2. `GameDataConnector` — nghe payload mới, truyền xuống kho

```csharp
GameEvent<(string name, int amount, string data, string where, ResourceParam context)[]>
    .Register(EVENT_ADD_REWARD, OnRewardWithContext, null);

private static void OnRewardWithContext(
    (string name, int amount, string data, string where, ResourceParam context)[] rewards)
{
    foreach (var r in rewards)
        ResourceCollector.Instance.ResourceAdd(r.name, r.amount, r.data, r.where, param: r.context);
    ResourceCollector.Instance.SaveResourcesAndUpdateToServer();
}
```

Cần GameData ≥ 1.1.7 (`ResourceAdd` có tham số `param`).

⚠ **Một món quà chỉ cộng một lần**: connector nghe payload mới thì **thôi** cộng quà từ payload cũ
(listener nào đang nghe bản 4/5 phần tử phải bỏ đăng ký hoặc bỏ qua). Không thì kho cộng đôi và
log ra hai dòng.

## 3. Hai điểm cần owner xác nhận

1. `GameDataConnector` 1.0.1 trong repo đăng ký payload **3 phần tử** `(name, amount, data)`,
   trong khi `APackElement` phát bản **4 và 5 phần tử** — `GameEvent<T>` tách listener theo từng
   kiểu `T` nên connector này không nhận được event. Game đang cộng quà pack bằng listener nào?
2. `OnBuySuccess()` có lấy được `transactionId` của giao dịch không? Có thì pack con điền vào
   `context.transactionId` — nối được "tiền bỏ ra" với "đồ nhận về".

---
*Additive toàn bộ — ship xong báo số bản. Ack/câu hỏi gửi lại như lệ; file xoá khi ship.*
