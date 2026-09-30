# Yêu cầu Core/GameData — forward `FParam` qua `ResourceCollector`

BigData cần thêm tham số trên log resource (`resourceWhen`/`resourceWhere`/`exchangeId`/
`detail`…) — module event muốn truyền nhưng chuỗi đang bị **blocking** tại `ResourceCollector`.
Điểm mấu chốt: `ResourceLog.LogAdd/LogRemove` **đã nhận `param` từ trước** và phía BigData giữ
nguyên các field đó — chỉ thiếu đường truyền vào. Mọi thay đổi **additive, default `null`**:
contract cũ không vỡ, contract mới mở đường.

## 1. `ResourceAdd` (và `ResourcesAdd`) — thêm 1 tham số, forward

```csharp
// TRƯỚC
public void ResourceAdd(string resourceId, int amount, string data,
    string @where = "", string itemId = "", Dictionary<string, object> detail = null)
...
    ResourceLog.LogAdd(resourceId, amount, valueBefore, value, where, itemId, detail);

// SAU
public void ResourceAdd(string resourceId, int amount, string data,
    string @where = "", string itemId = "", Dictionary<string, object> detail = null,
    FParam param = null)
...
    ResourceLog.LogAdd(resourceId, amount, valueBefore, value, where, itemId, detail, param);
```

## 2. `ResourceRemove` — y hệt (`ResourceSet`/`ResourceReset` mở tương tự nếu tiện)

Thêm `FParam param = null`, forward vào `ResourceLog.LogRemove` (chỗ đó cũng đã nhận sẵn).
Cần cho phần TIÊU của event (phí retry, mua lượt…).

## 3. Cùng đợt mở code — hai chỉnh trong sổ sách (mỗi cái một dòng lý do)

1. **Field lệch vai ở `ResourceLog.LogAdd/LogRemove`**: positional call đang đổ `where` vào
   tham số `itemType` của `ExtendResourceLog`, và `itemId` rỗng fallback = `where`. Sửa map:
   `itemType` = loại thật nếu biết, không thì để UNKNOWN; `where` đưa vào `resourceWhere`
   (caller không truyền param thì tự dựng `new ResourceParam { resourceWhere = where }`);
   bỏ fallback `itemId = where`. ⚠ Chạm data sống — **báo số bản** để BigData báo loader ghi
   era, phần thư loader bên mình lo.
2. **Thứ tự trong `ResourceAdd/Remove`**: đang `Invoke(_onResourceChange)` TRƯỚC khi tính
   `value/valueBefore` — listener cộng tiếp vào cùng resource làm số dư của dòng gốc sai.
   Đảo lại: tính value + gọi Log NGAY sau `Add/Remove`, Invoke listener sau cùng.

---
*Additive (mục 1–2) + 2 chỉnh sổ (mục 3). Ship xong báo số bản. Ack/câu hỏi gửi lại như lệ;
file xoá khi ship.*
