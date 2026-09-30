# Module Falcon Mediation

## Tổng quan
- **Tên:** `Falcon Mediation`
- **Giới thiệu:** Module cho mediation. Phục vụ hiển thị quảng cáo.
- **Các thành phần chính:**
    - LevelPlay SDK (IronSource cũ)
    - Max SDK

## Quick Start

### chú ý:
- Module này sử dụng module `Level Core` để lấy các tham số level để quản lý Monitize Ads, bắt buộc phải sử dụng module `Level Core` để quản lý level.

### 1. Cài đặt thêm SDK (cài cả Max và LevelPlay)
- Cài max (8.5.1)
- LevelPlay (9.2.0)

### 2. Cấu hình Module Mediation (LevelPlay, Max)
- Lưu ý: cài đặt thêm các SDK của LevelPlay hoặc Max trước, rồi mới làm bước này. Nếu cấu hình module này trước, thì sau khi cài các SDK tương ứng, phải vào cấu hình rồi ấn nút "Save" để lưu lại.
- Vào File cấu hình trên menu "Falcon/Modules/ThirdParty/Mediation settings"
- Chọn Network sẽ sử dụng, điền thông tin tương ứng do UA cung cấp.
- nếu điền ID của app open thì sẽ tự động kích hoạt app open. LevelPlay không có appopen, sẽ sử dụng AppOpen của GoogleMobileAds.
- App Open có cooldown time, sau thời gian cooldown mới được hiện appOpen tiếp theo, tránh spam AppOpen, gây khó chịu người dùng (new)
- Phần Banner có option "Show button close" phía góc trên bên phải của layout banner, nếu ấn vào đây sẽ hiển thị popup remove ads (new), tích chọn thì sẽ hiện nút này.
- Nếu chọn Auto install adapter, sẽ tự động tải adapter tương ứng với version hiện tại của network (Max, LevelPlay).
- Các mediation được cài sau khi chọn auto instal : 
  + với LevelPlay : Ironsource; Applovin; Digital Turbine (DT Exchange); Google (AdMob and Ad Manager); InMobi; Liftoff Monetize; Meta; Mintegral; Pangle; Smaato; Unity Ads; BidMachine; Moloco, Yandex
  + với Max : Ironsource, Applovin; Digital Turbine (DT Exchange); Google (AdMob and Ad Manager); InMobi; Liftoff Monetize; Meta; Mintegral; Pangle; Smaato; Unity Ads; BidMachine; Moloco; Verve; Yandex; Bigo Ads
- Sau khi cấu hình xong, nhấn nút "Save" để lưu lại.

### 3. Cấu hình MultiCall (chỉ hỗ trợ Max)
- Vào File cấu hình trên menu "Falcon/Modules/ThirdParty/Mediation settings"
- Nếu sử dụng multicall (Max), cần liên hệ MO để nhờ tạo chuỗi multicall này và điền vào

### 4. Khởi tạo Mediation
- Mediation sẽ tự động được khởi tạo sau khi hiện UMP

### 5. Các hàm hiện quảng cáo
```csharp
public class MediationManager
{
    public Action onBannerShow;
    public Action onBannerHide;
    public Action onBannerLoaded;
    public Action onButtonRemoveAds;
    public void ShowBanner(string placementId);
    public void HideBanner();
    public bool IsBannerReady();
    public void ShowInterstitial(string placementId, Action onInterstitialClosed = null, Action onFail = null, bool needAdBreak = false);
    public bool IsInterstitialReady();
    public void ShowRewardedVideo(string placementId, Action onRewardedComplete = null, Action onFail = null,  bool showInterstitialInstead = true);
    public bool IsRewardedVideoReady();
    public void SetRemoveAds(bool value);
    public StatusAds GetStatusAdsFromPlacement(string placementId, bool isRewarded = true);
//new
    public void IgnoreNextAoa();
}

#region banner

    /// <summary>
    /// hiện quảng cáo banner
    /// hàm đã tự động kiểm tra điều kiện để show banner, chỉ việc gọi ở màn hình muốn show banner
    /// <param name="placementId">Id của nơi gọi quảng cáo, trường hợp này thường là "Banner", ứng với trên remote config, tác dụng khi áp dụng với module add-on Mediation MO</param>
    /// </summary>
    public void FunctionShowBanner(){
        MediationManager.Instance.ShowBanner(string placementId);
    }

    /// <summary>
    /// ẩn quảng cáo banner
    /// </summary>
    public void FunctionHideBanner(){
        MediationManager.Instance.HideBanner();
    }

    /// <summary>
    /// kiểm tra trạng thái quảng cáo banner
    /// </summary>
    public void FunctionIsBannerReady(){
        MediationManager.Instance.IsBannerReady();
    }

    /// <summary>
    /// callback show/hide quảng cáo banner
    /// </summary>
    public void CallbackHideBanner(){
        MediationManager.Instance.onBannerHide += () => {
            Debug.Log("Hide banner");
        };
    }

    public void CallbackShowBanner(){
        MediationManager.Instance.onBannerShow += () => {
            Debug.Log("Show banner");
        };
    }

    /// <summary>
    /// callback khi banner loaded, callback này sẽ được invoke mỗi khi banner refresh
    /// </summary>
    public void CallbackBannerLoaded(){
        MediationManager.Instance.onBannerLoaded += () => {
            Debug.Log("Banner Loaded");
        };
    }

    /// <summary>
    /// callback khi ấn vào nút X trên banner
    /// thường dùng để show popup remove ads
    /// </summary>
    public void CallbackShowRemoveAds(){
        MediationManager.Instance.onButtonRemoveAds += () => {
            Debug.Log("Show popup remove ads");
        };
    }

#endregion

#region interstitial

    /// <summary>
    /// hiện quảng cáo interstitial
    /// </summary>
    /// <param name="onInterstitialClosed">callback được trả lại khi xem xong quảng cáo, chỉ xử lý logic chơi tiếp level</param>
    /// <param name="onFail">nếu hiển thị quảng cáo bị lỗi</param>
    /// <param name="placementId">Id của nơi gọi quảng cáo, lấy từ bảng cấu hình quảng cáo (VD : "IntertitialWinGame", "IntertitialLoseGame"), dùng làm ID để thiết lập quản lý quảng cáo trên remote config</param>
    /// <param name="needAdBreak">trạng thái có cần hiện popup adbreak trước khi hiện quảng cáo hay không, dành cho ads interstitial trong gameplay kéo dài, tránh hiện quảng cáo đột ngột (false : không bật popup, true : có bật popup)
    /// thời gian hiện popup nằm trong file config của module "UI AdBreak".
    /// Trường hợp interstitial có trả thưởng, thì mặc định sẽ hiện popup Ad Break</param>
    
    public void FunctionShowInterstitial()
    {

        //placementId lấy từ bảng MO AdsSettings
        var placementId = "IntertitialWinGame";
        MediationManager.Instance.ShowInterstitial(
            placementId: placementId,
            onInterstitialClosed: () => {
                Debug.Log("interstitial closed!");
                //xử lý trả thưởng cho interstitial tại đây (nếu có) - trường hợp có ad break thì sẽ trả thưởng ngay khi show popup ad break)
                //chỉ dùng khi sử dụng với module add-on Mediation MO
                //trong trường hợp không sử dụng module add-on Mediation MO thì user tự xử lý phần trả thưởng
                RewardedCallback rc = MediationManager.Instance.GetRewardedCallbackFromPlacementId(placementId);
                ProcessRewardedCallback(rc);
            },
            onFail: null,
            needAdBreak: true
            //nếu trên config, interstitial có trả thưởng, thì mặc định sẽ hiện popup Ad Break
        );
        
    }

    private void ProcessRewardedCallback(RewardedCallback rewardedCallback)
    {
        //tác dụng khi áp dụng với module add-on Mediation MO
        //trong trường hợp không sử dụng module add-on Mediation MO thì user tự xử lý phần trả thưởng
        //obj RewardedCallback sẽ đc trả về từ remote config
        //cần xử lý logic cụ thể cho từng game, vd : thêm gold, thêm thời gian ván chơi, thêm gem....
        if (rewardedCallback.isAction)
        {
            //trả về 1 list action
            for (var i = 0; i < rewardedCallback.listRewardedInfo.Count; i++)
            {
                var obj = rewardedCallback.listRewardedInfo[i];
                Debug.Log($"thực hiện action {obj.rewardId} với giá trị {obj.rewardValue}");
                if (obj.rewardId == "Add20sLevel")
                {
                    //thêm 20s vào gameplay
                }
                else if (obj.rewardId == "ChangeGateState")
                {
                    //thực hiện action changeGateState
                }
                //.....
            }
        }
        else
        {
            //list phần thường
            for (var i = 0; i < rewardedCallback.listRewardedInfo.Count; i++)
            {
                var obj = rewardedCallback.listRewardedInfo[i];
                Debug.Log($"nhận phần thưởng {obj.rewardId} với giá trị {obj.rewardValue}");
                if (obj.rewardId == "gold")
                {
                    //add gold
                }
                else if (obj.rewardId == "booster_1")
                {
                    //add booster 1
                }
            }
        }
    }

    /// <summary>
    /// kiểm tra quảng cáo interstitial có đang sẵn sàng hay không
    /// </summary>
    
    public bool FunctionCheckIsInterstitialReady(){
        return MediationManager.Instance.IsInterstitialReady(); //true (false)
    }
    
#endregion

#region rewarded

    /// <summary>
    /// hiện quảng cáo rewarded
    /// </summary>
    /// <param name="onRewardedComplete">callback được trả lại khi xem xong quảng cáo, chỉ xử lý logic chơi tiếp level, không xử lý logic phần thưởng ở đây</param>
    /// <param name="onFail">nếu hiển thị quảng cáo bị lỗi</param>
    /// <param name="placementId">Id của nơi gọi quảng cáo, lấy từ bảng cấu hình quảng cáo (VD : "RewardAdsBooster1", "RewardAdsBooster2"), dùng làm ID để thiết lập quản lý quảng cáo trên remote config</param>
    /// <param name="showInterstitialInstead">có hiển thị quảng cáo interstitial thay thế nếu Rewarded chưa sẵn sàng hay không</param>
    
    public void FunctionShowRewardedVideo(){
        //placementId lấy từ bảng MO AdsSettings
        var placementId = "RewardAdsBooster1";
            MediationManager.Instance.ShowRewardedVideo(
                placementId: placementId,
                onRewardedComplete: () =>
                {
                    //xử lý trả thưởng cho interstitial tại đây (nếu có) - trường hợp có ad break thì sẽ trả thưởng ngay khi show popup ad break)
                    //chỉ dùng khi sử dụng với module add-on Mediation MO
                    //trong trường hợp không sử dụng module add-on Mediation MO thì user tự xử lý phần trả thưởng
                    RewardedCallback rc = MediationManager.Instance.GetRewardedCallbackFromPlacementId(placementId);
                    ProcessRewardedCallback(rc);
                },
                onFail: () => { Debug.Log("TOAST : Tải quảng cáo lỗi"); },
                showInterstitialInstead: true
            );
    }

    /// <summary>
    /// kiểm tra quảng cáo rewarded có đang sẵn sàng hay không
    /// </summary>
    
    public bool FunctionCheckIsRewardedVideoReady(){
        return MediationManager.Instance.IsRewardedVideoReady(); //true (false)
    }

#endregion

#region remove_ads
    /// <summary>
    /// hàm set remove ads để không hiện quảng cáo inter, banner, app open nếu user đã mua gói remove ads
    /// chỉ cần gọi 1 lần lúc user mua gói remove ads, sau đó sẽ được lưu lại trên server
    /// </summary>
    /// <param name="v">giá trị bool true/false</param>
    
    public void FunctionSetRemoveAds(){
        //nếu user mua gói remove ads thì gọi :
        MediationManager.Instance.SetRemoveAds(true);
        //muốn hủy gói remove ads của user :
        MediationManager.Instance.SetRemoveAds(false);
    }

#endregion

#region App open
    /// <summary>
    /// hàm để ngăn hiển thị App Open ở lần thoát ra tiếp theo
    /// sử dụng khi user vào store ở Popup rate, khi user mua In App, ....
    /// </summary>
    public void FunctionIgnoreNextAoa(){
        Mediation.Instance.IgnoreNextAoa();
    }
#endregion

```
tham khảo ví dụ về phần hiện quảng cáo ở Scene "Assets/Falcon/Modules/Core/ThirdParty/Mediation/Samples/Scenes/Demo.unity"
Code ví dụ ở file [DemoTestMediation.cs](Samples/Scripts/DemoTestMediation.cs)

## Chi tiết

✅ **Các biến remote config của Mediation MO**
```csharp
- public float timeBreak = 0f;  //thời gian hiện popup "ad break time" trước khi hiện quảng cáo interstitial. Nếu để 0 thì sẽ ko hiện popup.
                                //muốn chỉnh sửa giao diện popup thì sửa prefab theo đường dẫn sau : "Assets/Falcon/Modules/UI/UIAdBreak/Prefabs/UIPopup_AdBreak.prefab"