# Module `Event Bus`

## Tổng quan
- **Tên:** `Event Bus`

- **Giới thiệu:**  
  Đây là một hệ thống Event Bus & Request Bus sử dụng generic trong Unity.

- **Các thành phần chính:**
    - `GameEvent<T>`: Bắn sự kiện để để gửi data kiểu T.
    - `GameRequest<T>`: Bắn sự kiện để request data kiểu T. Không cho phép truyền vào tham số.
    - `GameRequest<TP, T>`: Bắn sự kiện để request data kiểu T. Cho phép truyền vào tham số kiểu TP.
    - `EventBusViewer`: Hiển thị listener trên Unity Editor (dùng chung cho event & request).

## Quick Start

### 1. Đăng ký sự kiện để nhận dữ liệu khi cần
```csharp
GameEvent<MyData>.Register("OnEnemyDead", OnEnemyDeadCallback, this);
```
### 2. Hủy đăng ký
```csharp
GameEvent<MyData>.Unregister("OnEnemyDead", OnEnemyDeadCallback, this);
```
### 3. Bắn sự kiện
```csharp
GameEvent<MyData>.Emit("OnEnemyDead", myDataInstance);
```
### 4. Đăng ký để truyền dữ liệu khi có request. (không nhận tham số truyền vào)
```csharp
GameRequest<int>.Register("GetCoinAmount", GetCoinAmountFunc, this);
```
### 5. Hủy đăng ký (không nhận tham số truyền vào)
```csharp
GameRequest<int>.Unregister("GetCoinAmount");
```
### 6. Gửi Request (không nhận tham số truyền vào)
```csharp
int coin = GameRequest<int>.Request("GetCoinAmount");
```

### 7. Đăng ký để truyền dữ liệu khi có request. (nhận tham số truyền vào)
```csharp
GameRequest<int, int>.Register("GetLevelProgress", levelId => GetProgress(levelId), this);
```

### 8. Hủy đăng ký. (nhận tham số truyền vào)
```csharp
GameRequest<int, int>.Unregister("GetLevelProgress");
```

### 9. Gửi Request (nhận tham số truyền vào)
```csharp
int progress = GameRequest<int, int>.Request("GetLevelProgress", 3);
```

### 10. Đăng ký, hủy đăng ký, emit sự kiện mà không cần truyền dữ liệu:
```csharp
GameEvent.Register("OnEnemyDead", OnEnemyDeadCallback, this);
GameEvent.Unregister("OnEnemyDead", OnEnemyDeadCallback, this);
GameEvent.Emit("OnEnemyDead");
```

## Chi tiết

### Cách hoạt động
- Hệ thống sử dụng `Dictionary<string, Action<T>>` để lưu callback theo `eventName` hoặc `requestName`.
- Mỗi listener đều có thể gắn kèm `Component` để hỗ trợ theo dõi và debug trong Editor thông qua `EventBusViewer`.

### Unity Editor Support
- Tự động hiển thị danh sách listener đang active qua `GameObject` hierarchy (chỉ trong Editor).
- Cho phép kiểm tra đối tượng nào đang lắng nghe sự kiện.

## Lỗi thường gặp

| Lỗi | Nguyên nhân & Giải pháp |
|-----|--------------------------|
| `This action has already registered for this event` | Trùng đăng ký `Action<T>` → tránh gọi nhiều lần |
| `This event has no register yet` | Emit hoặc Request sai tên hoặc chưa đăng ký |
| Emit hoặc Register không đồng nhất `T` | Phải chắc chắn kiểu `T` và `eventName` đồng bộ tuyệt đối |
| `(A, B)` khác với `(B, A)` | Tuple thứ tự khác nhau là kiểu khác nhau |