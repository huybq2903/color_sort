# Module Level Core

## Tổng quan
- **Tên:** `Level Core`
- **Giới thiệu:** Load động và quản lý các level.

## Quick Start

### 1. Cài đặt
- Cần cài đặt một class để thực thi interface `IFLevelProvider`, có sử dụng Attribute [Primary], giả sử là `FLevelProvider` (nên là singleton)
- Cần đặt Assembly cho code bắt đầu bằng `Falcon.` để chương trình tự tìm được. (VD : `Falcon.Module.GamePlay.Level.asmdef`)

### 2. Cách thức hoạt động
- Client có sẵn các level lưu trữ trong bộ nhớ.
- Khi Client đăng nhập, Server sẽ gửi danh sách các level từ phía Server 
- Các level được tải về từ Server sẽ được lưu trong thư mục /falcon/server_levels/
- Khi đọc dữ liệu level, Client sẽ đọc trước tiên từ thư mục /falcon/server_levels/, nếu không có thì mới lấy dữ liệu có sẵn từ Client

### 3. Các hàm cần implement từ interface IFLevelProvider (như ví dụ trên là file FLevelProvider.cs)
- các hàm implement này cần viết theo từng game, được gọi đến trong FLevelManager.

```csharp
	[Primary]
	public class FLevelProvider : IFLevelProvider
	{
	   public string GetLevelData(int level); //trả về level data phía Client có sẵn
	   public string GetLevelParam(string levelData); //trả về level param từ một dữ liệu levelData full
	   public bool IsValidLevelData(string levelData);
	}

----------------------cách dùng----------------------
	[Primary]
	public class FLevelProvider : IFLevelProvider
	{
	   /// <summary>
	   /// hàm lấy data của level trên client (gồm map data, param, difficulty)
	   /// map data : là cấu trúc của map, có thể là dạng json, ma trận, ...
	   /// param có thể gồm : time = 50, speed = 10, ...
	   /// difficuty gồm : easy, normal, hard, ...
	   /// </summary>
	   /// <param name="level">level cần lấy data</param>
	   public string GetLevelData(int level)
	   {
		  //ví dụ file data level được lưu ở thư mục Resources
		  string levelData = Resources.Load<TextAsset>(level + ".data");
		  return levelData.text;
	   }

	   /// <summary>
	   /// hàm lấy param của level trên client
	   /// param có thể gồm : time = 50, speed = 10, ...
	   /// </summary>
	   /// <param name="level">level cần lấy param</param>
	   public string GetLevelParam(string levelData)
	   {
            TxtLevelData data = JsonConvert.DeserializeObject<TxtLevelData>(levelData);
            return "{" + $"\"time\":{data.time}" + "}";
	   }

	   /// <summary>
	   /// hàm kiểm tra level data có hợp lệ hay ko 
	   /// dạng json thì có parse đc hay ko, dạng ma trận thì có lấy đc data hay ko, tùy theo lưu data dạng gì
	   /// lỗi file hay gặp : do người dùng nhập sai, bị thay đổi cấu trúc, ...
	   /// </summary>
	   /// <param name="levelData">level data của level cần kiểm tra</param>
	   public bool IsValidLevelData(string levelData)
	   {
		  //ví dụ file data level được lưu ở thư mục Resources
		  string levelData = Resources.Load<TextAsset>(level + ".data");
		  //kiểm tra data có hợp lệ hay ko, cấu trúc data tùy mỗi game
		  return true/false;
	   }
   }
```

### 4. Các hàm cần gọi từ FLevelManager 
- Khi start level, verify hoặc win/lose level cần gọi trực tiếp từ class này
Ví dụ: lấy data level 1, gọi 
```csharp
FLevelManager.Instance.GetLevelData(1);
```

```csharp
public class FLevelManager
{  
	public void GetLevelData(int level);
	public void GetLevelParam(int level);
	public void GetLevelDifficulty(int level);
	public void VerifyLevel(string levelData, Action<bool> onResult);
	public void OnLevelReady();
	public void OnLevelStart();
	public void OnLevelResult(bool win, int time, Dictionary<string, int> booster2Number, int score = 0);
	public int[] GetAllLevels();
}
```
----------------------cách dùng----------------------
```csharp
	/// <summary>
   	/// hàm lấy level data
   	/// </summary>
   	/// <param name="level">level cần kiểm tra</param>
	var data = FLevelManager.Instance.GetLevelData(int level);
```
```csharp
	/// <summary>
   	/// hàm lấy level param
   	/// </summary>
   	/// <param name="level">level cần kiểm tra</param>
	var param = FLevelManager.Instance.GetLevelParam(int level);
```
```csharp
	/// <summary>
   	/// hàm lấy level data
   	/// </summary>
   	/// <param name="level">level cần kiểm tra</param>
	var difficulty = FLevelManager.Instance.GetLevelDifficulty(int level);
```
```csharp
	/// <summary>
   	/// hàm xác nhận level có thể qua được, hợp lệ, sẽ gửi lên server
   	/// Khi cộng tác viên xếp level, cần chơi thắng level đó ít nhất 1 lần, sau khi thắng thì Client gọi hàm này để thông báo lên Server rằng levelData này đã được verify
   	/// </summary>
   	/// <param name="levelData">level data của level cần kiểm tra</param>
	FLevelManager.Instance.VerifyLevel(levelData, (isValid) =>
	{
		if (isValid)
			//To-do
		else
			//To-do
	});
```

```csharp
   /// <summary>
   /// gọi hàm này khi vào trong màn hình level nhưng chưa bắt đầu chơi
   /// </summary>
   FLevelManager.Instance.OnLevelReady();
```

```csharp
   /// <summary>
   /// gọi hàm này khi bắt đầu chơi 1 level (khi bắt đầu, khi retry)
   /// là khi bắt đầu di chuyển một item trong màn chơi
   /// </summary>
   FLevelManager.Instance.OnLevelStart();
```
```csharp
   /// <summary>
   /// hàm thống kê lên server khi kết thúc 1 level
   /// gọi hàm này khi kết thúc 1 level (win, lose, quit, retry)
   /// </summary>
   /// <param name="win">trạng thái của level đó (win : true, lose : false), quit hay retry tương đương lose</param>
   /// <param name="time">thời gian chơi level đó (tính bằng giây, không tính thời gian xem quảng cáo, pause), chỉ tính thời gian in game</param>
   /// <param name="booster2Number">dictionary chứa thông tin của booster</param>
   /// <param name="score">điểm của ván chơi (optional, tùy game)</param>
   FLevelManager.Instance.OnLevelResult(bool win, int time, Dictionary<string, int> booster2Number, int score = 0);
   //ví dụ nếu level hiện tại thắng, chơi với 62 giây, sử dụng booster thứ nhất 2 lần, booster thứ hai 3 lần, không sử dụng booster thứ ba, đạt được 570 điểm
   var dict = new Dictionary<string, int>()
   {
      { "booster_1", 2 },
      { "booster_2", 3 }
   };
   FLevelManager.Instance.OnLevelResult(true, 62, dict, 570);
```

```csharp
// Trả về mảng các level đã được tải từ Server về, theo thứ thự tăng dần
// thường sẽ tuần tự từ 1-> n nhưng cũng có thể ngắt quãng
// Ví dụ: [1,3,6,7,8,12,15,17]
public int[] GetAllLevels();

```