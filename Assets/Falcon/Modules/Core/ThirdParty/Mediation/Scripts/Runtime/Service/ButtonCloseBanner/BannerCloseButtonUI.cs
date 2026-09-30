using System;
using System.Linq;
using Falcon.Modules.Core.ThirdParty.Mediation.Runtime;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class BannerCloseButtonUI : MonoBehaviour
{
    // ===== STATIC API =====

    private static BannerCloseButtonUI _instance;
    private const string ResourcePath = "ButtonCloseBanner";
    private bool _useSafeAreaBottom = true;

    /// <summary>
    /// Gọi hàm này sau khi banner load xong.
    /// bannerHeightPx: chiều cao banner (pixel màn hình).
    /// onClose: callback khi user bấm nút X (ẩn/hủy banner SDK).
    /// </summary>
    public static void Show(float bannerHeightPx, bool useSafeAreaBottom = true)
    {
        if (_instance == null)
        {
            var prefab = Resources.Load<BannerCloseButtonUI>(ResourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[BannerCloseButtonUI] Không tìm thấy prefab trong Resources/{ResourcePath}.prefab");
                return;
            }

            _instance = Instantiate(prefab);
            _instance.name = prefab.name + " (Runtime)";
            _instance.InitRuntime();
        }

        _instance._useSafeAreaBottom = useSafeAreaBottom;
        _instance.InternalShow(bannerHeightPx);
    }

    public static void Hide()
    {
        if (_instance == null) return;
        _instance.InternalHide();
    }

    // ===== INSTANCE PART =====

    [Header("Offsets (đơn vị Canvas)")] [SerializeField]
    private float marginX = 16f; // lệch vào trong màn hình (trái)

    [SerializeField] private float marginY = 8f; // khoảng cách cách đỉnh banner

    private RectTransform _rect;
    private Canvas _targetCanvas;
    private CanvasScaler _targetCanvasScaler;

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();
    }

    private void OnDestroy()
    {
        if (_instance == this)
            _instance = null;
    }

    /// <summary>
    /// Gọi một lần sau khi instance runtime được tạo ra.
    /// </summary>
    private void InitRuntime()
    {
        // Tìm canvas tốt nhất trong scene
        _targetCanvas = FindBestCanvas();
        if (_targetCanvas == null)
        {
            Debug.LogError("[BannerCloseButtonUI] Không tìm thấy Canvas trong scene.");
            return;
        }

        _targetCanvasScaler = _targetCanvas.GetComponent<CanvasScaler>();

        // Đặt làm con của canvas, giữ nguyên local scale/anchors
        transform.SetParent(_targetCanvas.transform, false);

        // Setup button click
        var btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClickInternal);
        }
    }

    private void InternalShow(float bannerHeightPx)
    {
        if (_targetCanvas == null)
        {
            Debug.LogWarning("[BannerCloseButtonUI] Canvas null, thử tìm lại.");
            _targetCanvas = FindBestCanvas();
            if (_targetCanvas == null) return;
            _targetCanvasScaler = _targetCanvas.GetComponent<CanvasScaler>();
            transform.SetParent(_targetCanvas.transform, false);
        }

        SetButtonPosition(bannerHeightPx);

        gameObject.SetActive(true);
    }

    private void InternalHide()
    {
        gameObject.SetActive(false);
    }

    private void OnClickInternal()
    {
        MediationManager.Instance.onButtonRemoveAds?.Invoke();
        // InternalHide();
    }

    /// <summary>
    /// Banner ở BOTTOM, nút X nằm ngay phía trên, sát góc phải.
    /// </summary>
    private void SetButtonPosition(float bannerHeightPx)
    {
        if (_rect == null || _targetCanvas == null)
            return;

        // Anchor & pivot tại BOTTOM-RIGHT
        _rect.anchorMin = new Vector2(1f, 0f);
        _rect.anchorMax = new Vector2(1f, 0f);
        _rect.pivot = new Vector2(1f, 0f);

        // 1) baseOffsetPx: nếu banner bám safe area thì cộng safeArea.yMin, 
        //    nếu banner bám đáy screen thì để 0
        float baseOffsetPx = _useSafeAreaBottom ? Screen.safeArea.yMin : 0f;

        // 2) convert sang đơn vị Canvas
        float bannerHeightCanvas = ConvertPxToCanvasUnit(bannerHeightPx);
        float baseOffsetCanvas = ConvertPxToCanvasUnit(baseOffsetPx);

        // 3) tổng khoảng cách từ đáy screen tới đỉnh banner
        float x = -marginX;
        float y = baseOffsetCanvas + bannerHeightCanvas + marginY;

        _rect.anchoredPosition = new Vector2(x, y);
    }

    private float ConvertPxToCanvasUnit(float px)
    {
        // Sử dụng scaleFactor của Canvas:
        // Dùng được cho cả Screen Space - Overlay và Screen Space - Camera
        float scaleFactor = _targetCanvas != null ? _targetCanvas.scaleFactor : 1f;
        if (scaleFactor <= 0f) scaleFactor = 1f;
        return px / scaleFactor;
    }

    /// <summary>
    /// Tìm canvas "phù hợp nhất":
    /// 1. Ưu tiên ScreenSpaceOverlay (sortingOrder cao nhất)
    /// 2. Nếu không có, dùng ScreenSpaceCamera (sortingOrder cao nhất)
    /// 3. Nếu vẫn không có, lấy cái root canvas bất kỳ sort cao nhất
    /// </summary>
    private Canvas FindBestCanvas()
    {
        var canvases = FindObjectsOfType<Canvas>(true)
            .Where(c => c.isRootCanvas && c.gameObject.activeInHierarchy)
            .ToList();

        if (canvases.Count == 0)
            return null;

        Canvas overlay = canvases
            .Where(c => c.renderMode == RenderMode.ScreenSpaceOverlay)
            .OrderByDescending(c => c.sortingOrder)
            .FirstOrDefault();

        if (overlay != null)
            return overlay;

        Canvas cameraSpace = canvases
            .Where(c => c.renderMode == RenderMode.ScreenSpaceCamera)
            .OrderByDescending(c => c.sortingOrder)
            .FirstOrDefault();

        if (cameraSpace != null)
            return cameraSpace;

        return canvases.OrderByDescending(c => c.sortingOrder).First();
    }
}