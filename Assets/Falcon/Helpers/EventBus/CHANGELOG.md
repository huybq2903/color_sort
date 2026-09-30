## 1.0.9
Xóa cả listener rỗng khi gọi hàm UnregisterAll
----------------------------------


## 1.0.8
Thêm API để bắn Event mà không muốn truyền data.
GameEvent.Register("OnEnemyDead", OnEnemyDeadCallback, this);
GameEvent.Unregister("OnEnemyDead", OnEnemyDeadCallback, this);
GameEvent.Emit("OnEnemyDead");
----------------------------------


## 1.0.7
Comment LogWarning
----------------------------------


## 1.0.6
Update Readme
----------------------------------