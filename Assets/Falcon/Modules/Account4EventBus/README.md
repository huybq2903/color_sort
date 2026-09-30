# Module Account
## Tổng quan
- **Tên:** `Account for Event Bus`
- **Giới thiệu:** Module dùng để gửi event bus mỗi khi đăng nhập thành công từ module account

## Quick Start
Cài module là xong
Mỗi khi đăng nhập thành công sẽ gửi event bus
`GameEvent<bool>.Emit("falcon.modules.account.login", success);`

Các module có thể bắt sự kiện này



