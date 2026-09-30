# Yêu cầu Events/Core — mở tham số context cho `EventRewardGranter`

BigData cần thêm tham số trên log resource (`resourceWhen`/`resourceWhere`/`exchangeId`/
`detail`…) — module event muốn truyền nhưng đang bị **blocking** vì `Grant` không có chỗ nhận.
Mọi thay đổi đều **additive, default `null`**: contract cũ không vỡ (caller cũ không sửa một
chữ), contract mới mở đường.

## 1. `Grant` — thêm 1 tham số optional, chuyển tiếp nguyên trạng

```csharp
// TRƯỚC
public static void Grant(EventRewardConfig reward, string place, string itemId = "")
{
    if (TryGrantSpecial(reward)) return;
    ResourceCollector.Instance.ResourceAdd(reward.name, reward.amount, reward.data, place, itemId);
}

// SAU
public static void Grant(EventRewardConfig reward, string place, string itemId = "",
    ResourceParam context = null)
{
    if (TryGrantSpecial(reward)) return;
    ResourceCollector.Instance.ResourceAdd(reward.name, reward.amount, reward.data, place, itemId,
        context?.detail, context);
}
```

(`ResourceAdd` nhận thêm 2 đối số cuối — GameData mở cùng đợt, thư riêng đã gửi họ.)

## 2. Overload `Grant(IEnumerable<...>)` — sửa y hệt

Thêm `ResourceParam context = null`, truyền xuống từng `ResourceAdd`. Lưu ý: **cùng một
`context` cho cả mẻ** (trong đó có `exchangeId` để server ghép các vế một lần claim) — đừng
làm rơi giữa vòng lặp.

## 3. Hook `Custom.GrantReward` — nhận và chuyển tiếp

```csharp
public virtual void GrantReward(MasterLeagueReward reward, ResourceParam context = null)
```

Game override hook này mà signature không có `context` thì chuỗi đứt tại đó — thêm optional
để override cũ vẫn compile (họ nhận context khi nào cần).

## 4. Một câu xác nhận

`TryGrantSpecial` return sớm, không qua `ResourceCollector` — **nhánh này hiện có log resource
không?** Nếu không, các thưởng đi đường đó (vd Free Lives dạng phút) đang không có mặt trong
kho — báo lại để bàn cách bổ sung (một lời gọi `Resource.OnEarned` với context là đủ).

---
*Additive toàn bộ — ship xong báo số bản. Ack/câu hỏi gửi lại như lệ; file xoá khi ship.*
