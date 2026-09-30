using UnityEngine;

public class FPSCounter : MonoBehaviour
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private static FPSCounter instance;

    private GUIStyle style;
    private float timer;
    private int frameCount;
    private int displayFps;

    private const float UpdateInterval = 0.5f;

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        style = new GUIStyle { normal = { textColor = Color.green } };
    }

    private void Update()
    {
        frameCount++;
        timer += Time.unscaledDeltaTime;

        if (timer >= UpdateInterval)
        {
            displayFps = Mathf.RoundToInt(frameCount / timer);
            frameCount = 0;
            timer = 0f;
        }
    }

    private void OnGUI()
    {
        // Cỡ chữ theo % chiều cao màn hình để đọc được trên mọi độ phân giải.
        style.fontSize = Mathf.RoundToInt(Screen.height * 0.03f);
        var pad = Screen.height * 0.003f;
        GUI.Label(new Rect(pad, pad, Screen.width, style.fontSize * 2f), $"{displayFps}", style);
    }
#endif
}
