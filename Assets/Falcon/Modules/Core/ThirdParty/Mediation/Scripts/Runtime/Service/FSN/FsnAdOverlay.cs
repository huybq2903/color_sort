using UnityEngine;
using UnityEngine.UI;

public class FsnAdOverlay : MonoBehaviour
{
    private static FsnAdOverlay _instance;
    private Canvas _canvas;
    private Image _overlay;

    public static FsnAdOverlay Instance
    {
        get
        {
            if (_instance != null) return _instance;
            var go = new GameObject("FsnAdOverlay");
            _instance = go.AddComponent<FsnAdOverlay>();
            DontDestroyOnLoad(go);
            return _instance;
        }
    }

    private void Awake()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 990;

        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var overlayGo = new GameObject("BlackPanel");
        overlayGo.transform.SetParent(transform, false);

        _overlay = overlayGo.AddComponent<Image>();
        _overlay.color = Color.black;

        var rect = _overlay.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        gameObject.SetActive(false);
    }

    public void Show()
    {
        Debug.Log("show overlay");
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        Debug.Log("hide overlay");
        gameObject.SetActive(false);
    }
}