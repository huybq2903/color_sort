# Module `UILevel`

## Tổng quan
- **Tên:** `UILevel`

- **Giới thiệu:**  
  Module dùng để hiển thị danh sách các level (level line) trong UI game, bao gồm thông tin về node level, độ khó, phần thưởng, nút chơi và chuỗi thắng (`winstreak`).

## Quick Start

### 1. Cài đặt và cấu hình
Phần này chỉ dành cho người dùng mới. Người dùng cũ update version thì xem hướng dẫn ở file ChangeLog.md nếu có thay đổi về cấu hình và cài đặt.
- Bước 1: Trên Unity Menu chọn Falcon > Modules > UI > Level > Assets. Bước này tạo asset cần thiết của module.
- Bước 2: Trên Unity Menu chọn Falcon > Modules > UI > Level > Config. Bước này mở ra file config để setup.
- Bước 3: Kéo prefab LevelLineController hoặc instantiate từ addressable lên scene.


### 2. Gọi code
Implement Interface ILevelLine (xem danh sách đầy đủ API bên dưới để biết thêm chi tiết).
Giả sử đặt tên là MyLevelLineImplementer, sau đó gọi để show dây level.

```csharp
LevelLineController.Show(new MyLevelLineImplementer());
```

## Danh sách đầy đủ API

- ILevelLine Interface:

      /// Trả về số lượng level muốn tạo trên dây
      int GetNumberOfLevels();
    
      /// Trả về level hiện tại
      int GetCurrentLevel();
      
      /// Trả về win streak nếu có. Nếu không có thì trả về 0
      int GetWinStreak();
      
      /// Trả về độ khó theo level
      /// <param name="level">Level cần lấy độ khó</param>
      int GetDifficultyAction(int level);
      
      /// Kiểm tra xem level có reward hay không. Với game không làm reward cho level thì return false
      /// <param name="level"></param>
      bool HasRewardsAction(int level);
      
      /// Sự kiện khi click vào 1 level node. Có thể để trống nếu không cho click vào level node
      /// <param name="model"></param>
      void OnClickLevelNode(LevelNodeModel model);
    
      /// Sự kiện khi click button play
      void OnClickBtnPlay();


### Hiển thị node level
- Mỗi `LevelNodeUI` hiển thị số level, màu nền theo độ khó, và callback click.
- Nếu là node hiện tại → Highlight + hiển thị nút chơi.

### Hiển thị Winstreak
- Nếu `WinStreak >= 2` thì hiện icon và số lượng.
- Sprite dùng từ `UILevelConfig`.

### Cập nhật nút chơi
- Nút Play có sprite riêng theo độ khó.
- Skull icon xuất hiện nếu độ khó > 0.