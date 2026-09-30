using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

#if UNITY_IOS
using AOT;
#endif

public enum FsnNativeAdType
{
    Native = 0,
    NativeFakeClose = 1,
    NativeCountdown = 2,
    Collapsible = 3
}

public enum FsnNativeAdRatio
{
    Any = 0,
    Landscape = 1,
    Portrait = 2,
    Square = 3
}

public enum FsnOverlayPosition
{
    TopRight = 0,
    TopLeft = 1,
    BottomLeft = 2,
    BottomRight = 3
}

public class FsnAd
{
#if UNITY_ANDROID
    private readonly AndroidJavaObject androidFsnAd;

    // Keep proxy references to avoid GC breaking Java callbacks.
    private AndroidOnFsnNativeLoadListener _nativeLoadListenerProxy;
    private AndroidOnFsnNativeCompletedListener _nativeCompletedListenerProxy;
#endif

    private static readonly object _lock = new object();
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static FsnMainThreadDispatcher _dispatcher;
    private static bool _isInitialized = false;

    private FsnNativeAdType _nativeType = FsnNativeAdType.Native;
    private FsnNativeAdRatio _nativeRatio = FsnNativeAdRatio.Any;
    private int _styleIndex = 2;
    private string _adUnitId = string.Empty;
    private int _countDownSec = 0;
    private long _delayForCountDown = 0;

    public int DebugStyleIndex => _styleIndex;
    public string DebugStyleName => GetStyleDebugName(_styleIndex);
    public FsnNativeAdType DebugNativeType => _nativeType;
    public FsnNativeAdRatio DebugNativeRatio => _nativeRatio;
    public string DebugAdUnitId => _adUnitId;

    public string GetDebugSummary()
    {
        return $"Type={_nativeType}, Ratio={_nativeRatio}, Style={_styleIndex}({GetStyleDebugName(_styleIndex)}), AdUnitId={_adUnitId}, CountDown={_countDownSec}, DelayCountdown={_delayForCountDown}";
    }

    private static string GetStyleDebugName(int styleIndex)
    {
        return styleIndex switch
        {
            0 => "Style1-FullMediaDark",
            1 => "Style2-TopMediaWhite",
            2 => "Style3-FullscreenVideoBottomInfo",
            3 => "Style4-CenteredPopup",
            _ => "Unknown-FallbackStyle3"
        };
    }

    private static string GetPositionDebugName(int position)
    {
        return position switch
        {
            0 => "TopRight",
            1 => "TopLeft",
            2 => "BottomLeft",
            3 => "BottomRight",
            _ => "Unknown"
        };
    }

#if UNITY_IOS
    private static int _nextIOSRequestId = 0;
    private readonly int _iosRequestId;

    private static bool _iosCallbacksRegistered = false;

    private sealed class IOSNativeCallbacks
    {
        public Action<int, string> OnLoadingCompleted;
        public Action OnLoadingStarted;
        public Action<string, string, long, string> OnAdPaid;
        public Action<string> OnAdCompleted;

        public FsnNativeAdType NativeType;
        public FsnNativeAdRatio NativeRatio;
        public int StyleIndex;
        public string AdUnitId;

        public string DebugSummary =>
            $"Type={NativeType}, Ratio={NativeRatio}, Style={StyleIndex}({GetStyleDebugName(StyleIndex)}), AdUnitId={AdUnitId}";
    }

    private static readonly Dictionary<int, IOSNativeCallbacks> _iosCallbacksByRequestId =
        new Dictionary<int, IOSNativeCallbacks>();

    // Giữ delegate instance static để tránh bị GC khi native gọi ngược về.
    private static readonly IOSLoadingCompletedDelegate _iosLoadingCompletedDelegate = IOS_OnLoadingCompleted;
    private static readonly IOSLoadingStartedDelegate _iosLoadingStartedDelegate = IOS_OnLoadingStarted;
    private static readonly IOSAdPaidDelegate _iosAdPaidDelegate = IOS_OnAdPaid;
    private static readonly IOSAdCompletedDelegate _iosAdCompletedDelegate = IOS_OnAdCompleted;
#endif

    public FsnAd()
    {
        EnsureDispatcherExists();

#if UNITY_IOS
        lock (_lock)
        {
            _nextIOSRequestId++;
            _iosRequestId = _nextIOSRequestId;
        }

        EnsureIOSCallbacksRegistered();
#endif

#if UNITY_ANDROID
        if (Application.platform == RuntimePlatform.Android)
        {
            try
            {
                androidFsnAd = new AndroidJavaObject("falcon.modules.core.thirdparty.mediation.fsnads.FsnAd");
                Debug.Log("[FsnAd] AndroidJavaObject created successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FsnAd] Failed to create AndroidJavaObject: {e.Message}\n{e.StackTrace}");
            }
        }
#endif
    }

    private static void EnsureDispatcherExists()
    {
        if (_isInitialized && _dispatcher != null)
            return;

        _dispatcher = UnityEngine.Object.FindObjectOfType<FsnMainThreadDispatcher>();

        if (_dispatcher == null)
        {
            var go = new GameObject("FsnMainThreadDispatcher");
            _dispatcher = go.AddComponent<FsnMainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(go);
            Debug.Log("[FsnAd] FsnMainThreadDispatcher created");
        }

        _isInitialized = true;
    }

    public static void ExecuteOnMainThread(Action action)
    {
        if (action == null)
            return;

        lock (_lock)
        {
            _executionQueue.Enqueue(action);
        }
    }

    public static void ProcessQueue()
    {
        Action[] actionsToExecute;

        lock (_lock)
        {
            if (_executionQueue.Count == 0)
                return;

            actionsToExecute = _executionQueue.ToArray();
            _executionQueue.Clear();
        }

        foreach (var action in actionsToExecute)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[FsnAd] Error executing action on main thread: {e.Message}\n{e.StackTrace}");
            }
        }
    }

#if UNITY_ANDROID
    private bool IsAndroidReady(string methodName, bool invokeFallbackCallback = false, Action fallbackCallback = null)
    {
        if (Application.platform != RuntimePlatform.Android)
            return false;

        if (androidFsnAd == null)
        {
            Debug.LogError($"[FsnAd] {methodName}() failed - androidFsnAd is null");

            if (invokeFallbackCallback)
                ExecuteOnMainThread(() => fallbackCallback?.Invoke());

            return false;
        }

        return true;
    }

    private AndroidJavaObject GetCurrentActivity()
    {
        using AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        return unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
    }
#endif

    // =========================
    // iOS External Methods
    // =========================

#if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void FsnAdIOS_InitNative(int requestId,int type,int ratio,string adUnitId,int countDownSec,long delayForCountDown);

    [DllImport("__Internal")]
    private static extern void FsnAdIOS_LoadNativeAd(int requestId,int ratio);

    [DllImport("__Internal")]
    private static extern void FsnAdIOS_ShowNativeAd(int requestId,int layoutType,int overlayOpenStorePos,int overlayClosePos);

    [DllImport("__Internal")]
    private static extern void FsnAdIOS_HideNativeAd(int requestId);

    [DllImport("__Internal")]
    private static extern bool FsnAdIOS_IsNativeAdReady(int requestId);

    public delegate void IOSLoadingCompletedDelegate(int requestId, int errorCode, string errorMessage);
    public delegate void IOSLoadingStartedDelegate(int requestId);
    public delegate void IOSAdPaidDelegate(int requestId, string adSource, string adUnitId, long valueMicros, string currencyCode);
    public delegate void IOSAdCompletedDelegate(int requestId, string errorMessage);

    [DllImport("__Internal")]
    private static extern void FsnAdIOS_SetCallbacks(IOSLoadingCompletedDelegate onCompleted,IOSLoadingStartedDelegate onStarted,IOSAdPaidDelegate onPaid,IOSAdCompletedDelegate onAdCompleted);

    private static void EnsureIOSCallbacksRegistered()
    {
        if (_iosCallbacksRegistered)
            return;

        FsnAdIOS_SetCallbacks(_iosLoadingCompletedDelegate,_iosLoadingStartedDelegate,_iosAdPaidDelegate,_iosAdCompletedDelegate);

        _iosCallbacksRegistered = true;

        Debug.Log("[FsnAd] iOS native callbacks registered");
    }

    private static IOSNativeCallbacks GetOrCreateIOSCallbacks(int requestId)
    {
        if (!_iosCallbacksByRequestId.TryGetValue(requestId, out var callbacks))
        {
            callbacks = new IOSNativeCallbacks();
            _iosCallbacksByRequestId[requestId] = callbacks;
        }

        return callbacks;
    }

    private static bool TryGetIOSCallbacks(int requestId, out IOSNativeCallbacks callbacks)
    {
        return _iosCallbacksByRequestId.TryGetValue(requestId, out callbacks);
    }

    private static void RemoveIOSCallbacks(int requestId)
    {
        if (_iosCallbacksByRequestId.ContainsKey(requestId))
        {
            _iosCallbacksByRequestId.Remove(requestId);
        }
    }
#endif

    // =========================
    // Init
    // =========================

    public void InitNative(FsnNativeAdType type, FsnNativeAdRatio ratio, string adUnitId, int countDownSec,
        int styleIndex, long delayForCountDown)
    {
        _nativeType = type;
        _nativeRatio = ratio;
        _styleIndex = styleIndex;
        _adUnitId = adUnitId ?? string.Empty;
        _countDownSec = countDownSec;
        _delayForCountDown = delayForCountDown;

#if UNITY_ANDROID
        if (!IsAndroidReady(nameof(InitNative)))
            return;

        try
        {
            androidFsnAd.Call("InitNative", (int)type, (int)ratio, adUnitId, countDownSec, delayForCountDown);
            Debug.Log($"[FsnAd][Init] Android InitNative called. {GetDebugSummary()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FsnAd] InitNative({type}) exception: {e.Message}\n{e.StackTrace}");
        }
#elif UNITY_IOS
        var callbacks = GetOrCreateIOSCallbacks(_iosRequestId);
        callbacks.NativeType = type;
        callbacks.NativeRatio = ratio;
        callbacks.StyleIndex = styleIndex;
        callbacks.AdUnitId = adUnitId ?? string.Empty;

        FsnAdIOS_InitNative(_iosRequestId,(int)type,(int)ratio,adUnitId,countDownSec,delayForCountDown);

        Debug.Log($"[FsnAd][Init] iOS InitNative called. RequestId={_iosRequestId}, {GetDebugSummary()}");
#endif
    }

    // =========================
    // Native
    // =========================

    public void SetNativeListener(Action<int, string> onLoadingCompleted, Action onLoadingStarted,
        Action<string, string, long, string> onAdPaid)
    {
#if UNITY_ANDROID
        if (!IsAndroidReady(nameof(SetNativeListener)))
            return;

        try
        {
            _nativeLoadListenerProxy =
                new AndroidOnFsnNativeLoadListener(onLoadingCompleted, onLoadingStarted, onAdPaid);
            androidFsnAd.Call("SetNativeListener", _nativeLoadListenerProxy);
            Debug.Log("[FsnAd] SetNativeListener() Android called");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FsnAd] SetNativeListener() exception: {e.Message}\n{e.StackTrace}");
        }
#elif UNITY_IOS
        EnsureIOSCallbacksRegistered();

        var callbacks = GetOrCreateIOSCallbacks(_iosRequestId);
        callbacks.OnLoadingCompleted = onLoadingCompleted;
        callbacks.OnLoadingStarted = onLoadingStarted;
        callbacks.OnAdPaid = onAdPaid;

        callbacks.NativeType = _nativeType;
        callbacks.NativeRatio = _nativeRatio;
        callbacks.StyleIndex = _styleIndex;
        callbacks.AdUnitId = _adUnitId;

        Debug.Log($"[FsnAd][Listener] iOS SetNativeListener called. RequestId={_iosRequestId}, {GetDebugSummary()}");
#endif
    }

    public void LoadNativeAd()
    {
#if UNITY_ANDROID
        if (!IsAndroidReady(nameof(LoadNativeAd)))
            return;

        try
        {
            using AndroidJavaObject unityActivity = GetCurrentActivity();
            androidFsnAd.Call("LoadNativeAd", unityActivity, (int)_nativeType, (int)_nativeRatio);
            Debug.Log($"[FsnAd][Load] Android LoadNativeAd called. {GetDebugSummary()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FsnAd] LoadNativeAd({_nativeType}) exception: {e.Message}\n{e.StackTrace}");
        }
#elif UNITY_IOS
        FsnAdIOS_LoadNativeAd(_iosRequestId, (int)_nativeRatio);
        Debug.Log($"[FsnAd][Load] iOS LoadNativeAd called. RequestId={_iosRequestId}, {GetDebugSummary()}");
#endif
    }

    public void ShowNativeAd(Action<string> onAdCompleted, int overlayPositionOpenStoreValue = 0,
        int overlayPositionCloseValue = 0)
    {
#if UNITY_ANDROID
        if (!IsAndroidReady(nameof(ShowNativeAd), true, () => onAdCompleted?.Invoke("")))
            return;

        try
        {
            using AndroidJavaObject unityActivity = GetCurrentActivity();
            _nativeCompletedListenerProxy = new AndroidOnFsnNativeCompletedListener(onAdCompleted);

            Debug.Log($"[FsnAd][Layout] Android ShowNativeAd requested. {GetDebugSummary()}, OpenStorePos={overlayPositionOpenStoreValue}({GetPositionDebugName(overlayPositionOpenStoreValue)}), ClosePos={overlayPositionCloseValue}({GetPositionDebugName(overlayPositionCloseValue)})");

            androidFsnAd.Call("ShowNativeAd", unityActivity, (int)_nativeType, _styleIndex,
                overlayPositionOpenStoreValue, overlayPositionCloseValue, _nativeCompletedListenerProxy);

            Debug.Log($"[FsnAd][Layout] Android ShowNativeAd bridge called. {GetDebugSummary()}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FsnAd] ShowNativeAd({_nativeType}) exception: {e.Message}\n{e.StackTrace}");
            ExecuteOnMainThread(() => onAdCompleted?.Invoke(e.Message));
        }
#elif UNITY_IOS
        EnsureIOSCallbacksRegistered();

        var callbacks = GetOrCreateIOSCallbacks(_iosRequestId);
        callbacks.OnAdCompleted = onAdCompleted;

        callbacks.NativeType = _nativeType;
        callbacks.NativeRatio = _nativeRatio;
        callbacks.StyleIndex = _styleIndex;
        callbacks.AdUnitId = _adUnitId;

        Debug.Log($"[FsnAd][Layout] iOS ShowNativeAd requested. RequestId={_iosRequestId}, {GetDebugSummary()}, OpenStorePos={overlayPositionOpenStoreValue}({GetPositionDebugName(overlayPositionOpenStoreValue)}), ClosePos={overlayPositionCloseValue}({GetPositionDebugName(overlayPositionCloseValue)}), IsReady={IsNativeAdReady()}");

        FsnAdIOS_ShowNativeAd(_iosRequestId,_styleIndex,overlayPositionOpenStoreValue,overlayPositionCloseValue);

        Debug.Log($"[FsnAd][Layout] iOS ShowNativeAd bridge called. RequestId={_iosRequestId}, Style={_styleIndex}({DebugStyleName})");
#endif
    }

    public void HideNativeAd()
    {
#if UNITY_ANDROID
        if (!IsAndroidReady(nameof(HideNativeAd), true))
            return;

        try
        {
            using AndroidJavaObject unityActivity = GetCurrentActivity();
            androidFsnAd.Call("HideNativeAd", (int)_nativeType);
            Debug.Log("[FsnAd] HideNativeAd() Android called.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[FsnAd] HideNativeAd({_nativeType}) exception: {e.Message}\n{e.StackTrace}");
        }
#elif UNITY_IOS
        FsnAdIOS_HideNativeAd(_iosRequestId);
        Debug.Log($"[FsnAd][Hide] iOS HideNativeAd called. RequestId={_iosRequestId}, {GetDebugSummary()}");
#endif
    }

    public bool IsNativeAdReady()
    {
#if UNITY_ANDROID
        if (!IsAndroidReady(nameof(IsNativeAdReady)))
            return false;

        try
        {
            return androidFsnAd.Call<bool>("IsNativeAdReady", (int)_nativeType);
        }
        catch (Exception e)
        {
            Debug.LogError($"[FsnAd] IsNativeAdReady({_nativeType}) exception: {e.Message}\n{e.StackTrace}");
            return false;
        }
#elif UNITY_IOS
        return FsnAdIOS_IsNativeAdReady(_iosRequestId);
#else
        return false;
#endif
    }

    // =========================
    // iOS MonoPInvokeCallbacks
    // =========================

#if UNITY_IOS
    [MonoPInvokeCallback(typeof(IOSLoadingCompletedDelegate))]
    private static void IOS_OnLoadingCompleted(int requestId, int errorCode, string errorMessage)
    {
        ExecuteOnMainThread(() =>
        {
            if (!TryGetIOSCallbacks(requestId, out var callbacks))
            {
                Debug.LogWarning($"[FsnAd] iOS OnLoadingCompleted ignored. Unknown RequestId={requestId}, ErrorCode={errorCode}, Msg={errorMessage}");
                return;
            }

            Debug.Log($"[FsnAd][iOSCallback] OnLoadingCompleted. RequestId={requestId}, ErrorCode={errorCode}, Msg={errorMessage}, {callbacks.DebugSummary}");

            callbacks.OnLoadingCompleted?.Invoke(errorCode, errorMessage);

            // Nếu load fail thì instance này không còn dùng nữa.
            // Load success thì giữ callback lại để còn nhận paid/completed.
            if (errorCode != 0 || !string.IsNullOrEmpty(errorMessage))
            {
                RemoveIOSCallbacks(requestId);
            }
        });
    }

    [MonoPInvokeCallback(typeof(IOSLoadingStartedDelegate))]
    private static void IOS_OnLoadingStarted(int requestId)
    {
        ExecuteOnMainThread(() =>
        {
            if (!TryGetIOSCallbacks(requestId, out var callbacks))
            {
                Debug.LogWarning($"[FsnAd] iOS OnLoadingStarted ignored. Unknown RequestId={requestId}");
                return;
            }

            Debug.Log($"[FsnAd][iOSCallback] OnLoadingStarted. RequestId={requestId}, {callbacks.DebugSummary}");

            callbacks.OnLoadingStarted?.Invoke();
        });
    }

    [MonoPInvokeCallback(typeof(IOSAdPaidDelegate))]
    private static void IOS_OnAdPaid(int requestId,string adSource,string adUnitId,long valueMicros,string currencyCode)
    {
        ExecuteOnMainThread(() =>
        {
            if (!TryGetIOSCallbacks(requestId, out var callbacks))
            {
                Debug.LogWarning($"[FsnAd] iOS OnAdPaid ignored. Unknown RequestId={requestId}");
                return;
            }

            Debug.Log($"[FsnAd][iOSCallback] OnAdPaid. RequestId={requestId}, Source={adSource}, Unit={adUnitId}, ValueMicros={valueMicros}, Currency={currencyCode}, {callbacks.DebugSummary}");

            callbacks.OnAdPaid?.Invoke(adSource, adUnitId, valueMicros, currencyCode);
        });
    }

    [MonoPInvokeCallback(typeof(IOSAdCompletedDelegate))]
    private static void IOS_OnAdCompleted(int requestId, string errorMessage)
    {
        ExecuteOnMainThread(() =>
        {
            if (!TryGetIOSCallbacks(requestId, out var callbacks))
            {
                Debug.LogWarning($"[FsnAd] iOS OnAdCompleted ignored. Unknown RequestId={requestId}, Msg={errorMessage}");
                return;
            }

            Debug.Log($"[FsnAd][iOSCallback] OnAdCompleted. RequestId={requestId}, Msg={errorMessage}, {callbacks.DebugSummary}");

            callbacks.OnAdCompleted?.Invoke(errorMessage);

            // Show xong/đóng xong thì dọn callback của request này.
            RemoveIOSCallbacks(requestId);
        });
    }
#endif

    // =========================
    // Android Java proxies
    // =========================

#if UNITY_ANDROID
    private sealed class AndroidOnFsnNativeLoadListener : AndroidJavaProxy
    {
        private readonly Action<int, string> onLoadingCompleted;
        private readonly Action onLoadingStarted;
        private readonly Action<string, string, long, string> onAdPaid;

        public AndroidOnFsnNativeLoadListener(
            Action<int, string> onLoadingCompleted,
            Action onLoadingStarted,
            Action<string, string, long, string> onAdPaid)
            : base("falcon.modules.core.thirdparty.mediation.fsnads.OnFsnNativeLoadListener")
        {
            this.onLoadingCompleted = onLoadingCompleted;
            this.onLoadingStarted = onLoadingStarted;
            this.onAdPaid = onAdPaid;
        }

        public void OnLoadingCompleted(int errorCode, string errorMessage)
        {
            ExecuteOnMainThread(() => { onLoadingCompleted?.Invoke(errorCode, errorMessage); });
        }

        public void OnLoadingStarted()
        {
            ExecuteOnMainThread(() => { onLoadingStarted?.Invoke(); });
        }

        public void OnAdPaid(string adSource, string adUnitId, long valueMicros, string currencyCode)
        {
            ExecuteOnMainThread(() => { onAdPaid?.Invoke(adSource, adUnitId, valueMicros, currencyCode); });
        }
    }

    private sealed class AndroidOnFsnNativeCompletedListener : AndroidJavaProxy
    {
        private readonly Action<string> onAdCompleted;

        public AndroidOnFsnNativeCompletedListener(Action<string> onAdCompleted)
            : base("falcon.modules.core.thirdparty.mediation.fsnads.OnFsnNativeCompletedListener")
        {
            this.onAdCompleted = onAdCompleted;
        }

        public void OnAdCompleted(string errorMessage)
        {
            ExecuteOnMainThread(() => { onAdCompleted?.Invoke(errorMessage); });
        }
    }
#endif
}

public class FsnMainThreadDispatcher : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[FsnMainThreadDispatcher] Awake");
    }

    private void Update()
    {
        FsnAd.ProcessQueue();
    }

    private void OnDestroy()
    {
        Debug.Log("[FsnMainThreadDispatcher] OnDestroy");
    }
}