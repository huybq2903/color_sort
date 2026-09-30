/*
 * Author: namnx
 * Email: namnx@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-06
 */

using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using UnityEngine;

namespace Falcon.Modules.Mediation.Sample
{
    public class DemoTestMediation : MonoBehaviour
    {
        private void ProcessRewardedCallback(RewardedCallback rewardedCallback)
        {
            //obj RewardedCallback sẽ đc trả về từ remote config
            //cần xử lý logic cụ thể cho từng game, vd : thêm gold, thêm thời gian ván chơi, thêm gem....
            //list phần thường
            for (var i = 0; i < rewardedCallback.listReward.Count; i++)
            {
                var obj = rewardedCallback.listReward[i];
                Debug.Log($"nhận phần thưởng {obj.name} với giá trị {obj.amount}");
                if (obj.name == "gold")
                {
                    //add gold
                }
                else if (obj.name == "booster_1")
                {
                    //add booster 1
                }
            }
        }

        public void ButtonShowBanner()
        {
            //MO AdsSettings sẽ kiểm tra điều kiện trên remote config để xem có hiện banner hay không
            //người dùng chỉ việc gọi ở nơi muốn show, ko cần tự kiểm tra level có đủ điều kiện hay không
            MediationManager.Instance.ShowBanner("Banner");
        }

        public void ButtonHideBanner()
        {
            //dùng để ẩn banner, ví dụ vào màn hình game không muốn hiện thì gọi hàm này.
            MediationManager.Instance.HideBanner();
        }

        public void ButtonShowInterstitial()
        {
            //placementId lấy từ bảng MO AdsSettings
            var placementId = "IntertitialWinGame";
            MediationManager.Instance.ShowInterstitial(
                placementId: placementId,
                onInterstitialClosed: () =>
                {
                    RewardedCallback rc = MediationManager.Instance.GetRewardedCallbackFromGroupId(placementId);
                    ProcessRewardedCallback(rc);
                    Debug.Log("interstitial closed!");
                },
                onFail: null,
                //nếu trên config, interstitial có trả thưởng, thì mặc định sẽ hiện popup ad Break
                needAdBreak: true
            );
        }

        public void ButtonShowRewarded()
        {
            //placementId lấy từ bảng MO AdsSettings
            var placementId = "RewardAdsBooster1";
            MediationManager.Instance.ShowRewardedVideo(
                placementId: placementId,
                onRewardedComplete: () =>
                {
                    Debug.Log("====rewarded closed! xử lý nhận phần thưởng tại đây=====");
                    RewardedCallback rc = MediationManager.Instance.GetRewardedCallbackFromGroupId(placementId);
                    ProcessRewardedCallback(rc);
                },
                onFail: () => { Debug.Log("TOAST : Tải quảng cáo lỗi"); },
                showInterstitialInstead: true
            );
        }

        public void SetRemoveAds(bool v)
        {
            MediationManager.Instance.SetRemoveAds(v);
        }
    }
}