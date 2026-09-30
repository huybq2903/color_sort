# Module @Helper Game Data

## Tổng quan
- **Tên:** `Helper Game Data`
- **Giới thiệu:** Sử dụng Reflection truy cập vào 1 đối tượng, để lấy dữ liệu của đối tượng đó thông qua tên dữ liệu

## Chi tiết

//Ví dụ để xử lý FGameDataProfile Module

FGameDataProfile profile = new FGameDataProfile();

// In ra giá trị ban đầu
Debug.Log("=== Initial values ===");
GameDataUtils.PrintFieldValues(profile);

// Gán giá trị mới
GameDataUtils.SetFieldValueByName(profile, "avatarId", 123);
GameDataUtils.SetFieldValueByName(profile, "playerName", "DongVV");
GameDataUtils.SetFieldValueByName(profile, "avatarUrl", "http://example.com/avatar.png");

// In ra giá trị sau khi sửa
Debug.Log("=== After update ===");
GameDataUtils.PrintFieldValues(profile);

// Lấy giá trị cụ thể
object playerName = GameDataUtils.GetFieldValueByName(profile, "playerName");
Debug.Log($"GetFieldValueByName: playerName = {playerName}");