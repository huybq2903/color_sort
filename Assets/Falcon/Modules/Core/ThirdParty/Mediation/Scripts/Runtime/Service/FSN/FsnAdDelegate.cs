using System;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using UnityEngine;

public sealed class FsnRewardedCallbacks
{
    public Action OnRewardReceived { get; }
    public Action OnDisplayFailed { get; }
    public Action OnHidden { get; }

    public FsnRewardedCallbacks(
        Action onRewardReceived,
        Action onDisplayFailed,
        Action onHidden)
    {
        OnRewardReceived = onRewardReceived;
        OnDisplayFailed = onDisplayFailed;
        OnHidden = onHidden;
    }
}

public delegate void FsnShowRewardedDelegate(FsnRewardedCallbacks callbacks);

public sealed class FsnInterstitialCallbacks
{
    public Action OnHidden { get; }
    public Action OnDisplayFailed { get; }

    public FsnInterstitialCallbacks(
        Action onHidden,
        Action onDisplayFailed)
    {
        OnHidden = onHidden;
        OnDisplayFailed = onDisplayFailed;
    }
}

public delegate void FsnShowInterstitialDelegate(FsnInterstitialCallbacks callbacks);

public sealed class FsnRewardedNativeFlow
{
    private readonly FsnShowRewardedDelegate _showRewarded;
    private readonly Action<Action> _showNative;
    private readonly Action _onSuccess;
    private readonly Action _onFailed;

    private bool _rewardReceived;
    private bool _nativeStarted;
    private bool _finished;

    public FsnRewardedNativeFlow(
        FsnShowRewardedDelegate showRewarded,
        Action<Action> showNative,
        Action onSuccess,
        Action onFailed)
    {
        _showRewarded = showRewarded;
        _showNative = showNative;
        _onSuccess = onSuccess;
        _onFailed = onFailed;
    }

    public void Start()
    {
        _rewardReceived = false;
        _nativeStarted = false;
        _finished = false;

        var callbacks = new FsnRewardedCallbacks(
            onRewardReceived: OnRewardReceived,
            onDisplayFailed: OnDisplayFailed,
            onHidden: OnHidden
        );

        try
        {
            _showRewarded?.Invoke(callbacks);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            FinishFailed();
        }
    }

    private void OnRewardReceived()
    {
        if (_finished) return;

        Debug.Log("[FSN][RewardFlow] Reward received");

        _rewardReceived = true;
    }

    private void OnDisplayFailed()
    {
        if (_finished) return;

        Debug.Log("[FSN][RewardFlow] Rewarded display failed");

        FinishFailed();
    }

    private void OnHidden()
    {
        if (_finished) return;

        Debug.Log($"[FSN][RewardFlow] Rewarded hidden. rewardReceived = {_rewardReceived}");

        // if (!_rewardReceived)
        // {
        //     FinishFailed();
        //     return;
        // }

        ShowNative();
    }

    private void ShowNative()
    {
        if (_finished) return;
        if (_nativeStarted) return;

        _nativeStarted = true;

        Debug.Log("[FSN][RewardFlow] Show native after rewarded");

        try
        {
            _showNative?.Invoke(OnNativeDone);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            FinishFailed();
        }
    }

    private void OnNativeDone()
    {
        if (_finished) return;

        Debug.Log("[FSN][RewardFlow] Native done");

        // FinishSuccess();
        if (_rewardReceived)
        {
            // User xem trọn vẹn cả 2 -> Thành công (Nhận quà)
            FinishSuccess();
        }
        else
        {
            // User đã bấm Skip quảng cáo MAX -> Kết thúc luồng dạng Thất bại (Không cho quà)
            FinishFailed();
        }
    }

    // Trong file FsnAdDelegate.cs

    private void FinishSuccess()
    {
        if (_finished) return;
        _finished = true;

        Debug.Log("[FSN][RewardFlow] Flow finished with SUCCESS");

        // SỬA TẠI ĐÂY: Thay vì ẩn ngay lập tức, hãy dùng luồng phụ hoãn lại 1 frame hoặc 0.1 giây 
        // để Android dọn dẹp xong giao diện Ads cũ, tránh bị hẫng đồ họa
        FsnAdManager.Instance.StartCoroutine(DelayHideOverlay(() =>
        {
            try
            {
                _onSuccess?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }));
    }

    private void FinishFailed()
    {
        if (_finished) return;
        _finished = true;

        Debug.Log("[FSN][RewardFlow] Flow finished with FAILED");

        // SỬA TẠI ĐÂY: Tương tự cho luồng thất bại
        FsnAdManager.Instance.StartCoroutine(DelayHideOverlay(() =>
        {
            try
            {
                _onFailed?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }));
    }

    // THÊM HÀM BỔ TRỢ NÀY VÀO TRONG CLASS FsnRewardedNativeFlow
    private System.Collections.IEnumerator DelayHideOverlay(Action callbackOnDone)
    {
        // Chờ 1 frame đồ họa của Unity kết thúc hoàn toàn
        yield return new WaitForEndOfFrame();

        // Hoặc nếu máy cấu hình yếu vẫn bị khựng, bạn có thể chờ một khoảng thời gian ngắn:
        // yield return new WaitForSeconds(0.1f);

        // Tiến hành ẩn màn đen một cách an toàn
        FsnAdOverlay.Instance.Hide();

        // Thực thi callback trả code về cho logic Game chạy tiếp
        callbackOnDone?.Invoke();
    }
}