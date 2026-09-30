# Module @FAntiCheat

## Tổng quan
- **Tên:** `Falcon AntiCheat`
- **Giới thiệu:** Hỗ trợ các kiểu Obsecured của thư viện Anti-Cheat trong FGameData

## Quick Start

Ví dụ

```csharp
    [FGameDataType("game_data_1")]
    public class GameData1 : FGameData<GameData1>
    {
        public ObscuredInt int1 = 10;
        public ObscuredString string1 = "123";
        
        public override void OnUpdateFromServer()
        {

        }
    }
```