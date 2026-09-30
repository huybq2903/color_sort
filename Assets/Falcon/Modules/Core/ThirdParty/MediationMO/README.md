# Module Falcon MediationMO

## Tổng quan
- **Tên:** `Falcon Mediation MO`
- **Giới thiệu:** Module add-on cho mediation. Phục vụ tối ưu hiển thị quảng cáo.

## Quick Start

### 1. Cấu hình Module Mediation MO
- Lưu ý : phần Mediation MO này cần điền tham số mặc định, dành cho trường hợp không lấy được giá trị trên cms, sau này vẫn có thể sửa trên cms được.
- Vào File cấu hình trên menu "Falcon/Modules/ThirdParty/Ads MO settings"
- Điền Settings Ads MO, là cấu hình quảng cáo theo từng game, từng user, để tối ưu doanh thu.
link cms để cấu hình: http://puzzle.data4game.com:5173/ads/remote-config (Tài khoản lấy từ trungvh)

### 2. Khởi tạo Mediation MO
- Mediation MO sẽ tự động được khởi tạo sau khi hiện UMP

### 3. Chú ý:
- khi đến màn hình game play, thì bắn event bus `event_bus_scene_level_start` để cấu hình quảng cáo được reset lại đúng giá trị, thì mới chạy đúng.
- bắn event này khi load scene gameplay xong, event này khác với thời điểm bắt đầu chạm vào màn hình và bắt đầu đếm lùi thời gian (khác event `FLevelManager.Instance.OnLevelStart()` )

```csharp
private const string EVENT_BUS_SCENE_LEVEL_START = "event_bus_scene_level_start";
//bắn event bus khi vào đến màn hình gameplay, phân biệt với lúc bắt đầu đếm lùi thời gian chơi, event này được gọi trước
GameEvent.Emit(EVENT_BUS_SCENE_LEVEL_START);
```

✅ **Các biến remote config của Mediation MO**
```csharp
- public Dictionary<string, AdsConfig> configAds; //config ads được dùng để cấu hình quảng cáo từ remote, tối ưu quảng cáo (dùng cms để chỉnh sửa)

```
link tham khảo : 
https://docs.google.com/spreadsheets/d/1Cq2PCDwv8yH6K3H9uwree1JdRGIVlOTqErJfw5AaKS4/edit?gid=1073199400#gid=1073199400
https://docs.google.com/spreadsheets/d/1Cq2PCDwv8yH6K3H9uwree1JdRGIVlOTqErJfw5AaKS4/edit?gid=2084739888#gid=2084739888
Ví dụ về chuỗi configAds trên khi ở dạng bảng :
PlacementId                 LevelUnlock     IsActive    SessionLimit    DayLimit    LevelLimit  IntervalTime    IntervalLevel   IntervalBetweenIV    IsAction             RewardID          RewardValue
RewardAdsLives              10              Yes         -1              -1          -1          -1              -1              60                   false                lives;gold        1;20
RewardAdsBooster 1          10              Yes         -1              -1          -1          -1              -1              90                   false                booster_1         1
RewardAdsBooster 2          10              Yes         -1              -1          -1          -1              -1              90                   false                booster_2         1
RewardAdsBooster 3          10              Yes         -1              -1          -1          -1              -1              90                   false                booster_3         1
RewardWinGame               6               Yes         -1              -1           1          -1              -1              90                   false                gold              40
RewardLoseGameByTime        10              Yes         -1              -1           1          -1              -1              90                   true                 Add20sLevel       0
RewardLoseGameByBomb        10              Yes         -1              -1           1          -1              -1              90                   true                 Add20sBomb        0
RewardLoseGameByGate        10              Yes         -1              -1           1          -1              -1              90                   true                 ChangeGateState   0
IntertitialWinGame          10              Yes         -1              -1          -1          -1              -1              60                   false                gold              2
IntertitialQuit             15              Yes         -1              -1          -1          -1              -1              60                   false                gold              2
IntertitialRetry            20              Yes         -1              -1          -1          -1              -1              60                   false                gold              2
Banner                      10              Yes         -1              -1          -1          -1              -1              -1                   false                ""                0

PlacementId
	- Là phần ID của placement quảng cáo
	- Mỗi khi muốn đặt một placement mới thì sẽ code riêng rồi truyền vào đây để quản lý
    - lưu ý: riêng phần banner thì để tên placementId = "Banner", để phân biệt được với rewarded và interstitial
LevelUnlock	
	- Config xem placement này sẽ unlock ở level nào
IsActive	
	- Config bật tắt placement này
SessionLimit	
	- Config xem trong 1 session thì vị trí ads này limit bao nhiêu lần xem
DayLimit	
	- Config xem trong 1 ngày thì vị trí ads này sẽ limit bao nhiêu lần xem
LevelLimit	
	- Config xem trong 1 level thì vị trí ads này sẽ có bao nhiêu lần hiện
IntervalTime	
	- Config xem trong khoảng thời gian tối thiểu giữa 2 lần xem ads tại placement này
	- Nếu không thỏa mãn thì sẽ không cho người chơi xem ads
IntervalLevel	
	- Config khoảng level tối thiểu giữa 2 lần xem ads tại placement này
	- Ví dụ config level là 2 thì có nghĩa sau khi người chơi xem ads tại vị trí này thì sau 2 level mới có thể tiếp tục xem ads
IntervalBetween_IV	
	- Config xem sau khi xem xong reward thì tối thiểu bao nhiêu giây sau mới có thể đập intertitial
	- Config này chỉ dùng cho reward ads
Action	
	- Config action xem quảng cáo này sẽ thực hiện hành động gì
RewardID/ RewardValue	
	- RewardID truyền vào ID của các loại reward mà người chơi đc nhận
	- RewardValue là giá trị của loại reward tương ứng ở reward ID

✅ **Cấu trúc AdsConfig, RewardedCallback, RewardedInfo**
```csharp
public class AdsConfig
{
    public int levelUnlock = -1; //level unlock
    public bool isActive = true; //true : bật; false : tắt
    public int sessionLimit = -1; //trong một session thì quảng cáo này hiện bao lần
    public int dayLimit = -1; //trong một ngày thì quảng cáo này được hiện bao lần
    public int levelLimit = -1; //trong một level, một lượt chơi, thì quảng cáo này hiện bao lần
    public float intervalTime = -1; //khoảng thời gian tối thiểu giữa 2 lần xem quảng cáo tại id này
    public int intervalLevel = -1; //level tối thiểu giữa 2 lần xem quảng cáo tại id này
    public float intervalBetweenIV = 60; //thời gian min sau khi xem xong rewarded thì xem đc inters (chỉ cho rewarded)
    public RewardedCallback rewardedCallback;
}
public class RewardedCallback
{
    public bool isAction; //để biết callback trả về này có phải là action hay không.
    public List<RewardedInfo> listRewardedInfo;
}
public class RewardedInfo
{
    public string rewardId; //nếu isAction là true, thì rewardId sẽ là tên action trả về. VD: rewardId = Add20sLevel, Add20sBomb, ChangeGateState...
    //sẽ xử lý action dựa theo rewardId ( cộng thêm 20s cho level, + thêm 20s vào bom, ... )
    //nếu isAction là false, thì rewardId sẽ là phần thưởng trả về cho user, vd : lives, booster_1, booster_2, ....
    //sẽ trả phần thưởng cho user dựa theo rewardId ( cộng thêm lives, booster với giá trị là {rewardValue} )
    public int rewardValue; //giá trị của phần thưởng sẽ được nhận
}

```
✅ **Hàm kiểm tra trạng thái của quảng cáo theo vị trí**
- hàm này dùng để hiển thị button trên UI, kiểm tra xem quảng cáo có đc bật hay ko (ẩn/hiện), có đang bị expried hay ko (hiện nhưng để nút màu xám)
```csharp
    public StatusAds GetStatusAdsFromPlacement(string placementId, bool isRewarded = true);
    //nếu kiểm tra cho rewarded thì truyền isRewarded = true, nếu kiểm tra cho interstitial thì isRewared = false

    public class StatusAds
    {
        public StatusShowAds statusShowAds; //trạng thái của ads tại thời điểm check
        public double remainingTime; //nếu statusShowAds là IntervalTime hoặc IntervalBetweenIv thì trả về thời gian chờ còn lại (đơn vị là giây)
        public int remainingAttempts; //trả về số lượt còn lại có thể hiển thị (sẽ lấy giá trị nhỏ nhất trong 3 giá trị : session limit, day limit, level limit)
    }
    
    public enum StatusShowAds
    {
        Active, //có thể xem quảng cáo
        LevelLimit, //đã hiện đủ quảng cáo trong 1 level
        IntervalTime, //chưa đủ thời gian giữa 2 lần quảng cáo
        IntervalLevel, //chưa đạt đến mức giới hạn level tiếp theo
        IntervalBetweenIv, //vừa show rewarded xong, chưa đủ thời gian để xem inter (cái này chỉ dành cho inter)
        DayLimit, //đã hiện đủ quảng cáo trong 1 ngày
        SessionLimit, //đã hiện đủ quảng cáo trong phiên chơi này
        LevelUnlock, //chưa đạt đủ level
        DeActive //không bật quảng cáo ở vị trí này
    }
```

✅ **Hàm lấy config Ads theo placementId**
```csharp
    AdsConfig config = MediationManager.Instance.GetAdsConfigFromPlacementId(string placementId);
```

✅ **Hàm lấy RewardedCallback theo placementId**
```csharp
    RewardedCallback rewardedCallback = MediationManager.Instance.GetRewardedCallbackFromPlacementId(string placementId);
```