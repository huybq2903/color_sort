/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-09-10
 */

using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// EditorScene -> GameView maximize + Free Aspect. StartScene/GameScene -> thu gọn + 1080x1920.
// Lúc play chỉ đổi aspect, không đụng maximize.
[InitializeOnLoad]
internal static class EditorSceneGameViewSetup
{
    private const string EditorSceneName = "EditorScene";
    private const int PortraitWidth = 1080;
    private const int PortraitHeight = 1920;
    private const string PortraitLabel = "Portrait";
    private const int FreeAspectIndex = 0; // Free Aspect luôn đứng đầu mọi size group

    private static readonly Assembly EditorAsm = typeof(EditorWindow).Assembly;

    static EditorSceneGameViewSetup()
    {
        EditorSceneManager.sceneOpened += (scene, mode) =>
        {
            if (mode == OpenSceneMode.Single) ApplyFor(scene.name);
        };

        // Play/Back To Editor đổi scene bằng SceneManager runtime, sceneOpened không bắn
        SceneManager.activeSceneChanged += (_, next) => ApplyFor(next.name);

        // Thoát play mode không bắn sceneOpened -> tự khôi phục theo scene đang mở
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode) ApplyFor(SceneManager.GetActiveScene().name);
        };
    }

    private static void ApplyFor(string sceneName)
    {
        switch (sceneName)
        {
            case EditorSceneName:
                // delayCall: đổi layout ngay trong callback bị layout rebuild nuốt mất
                EditorApplication.delayCall += () => Apply(FreeAspectIndex, true);
                break;
            case "StartScene":
            case "GameScene":
                EditorApplication.delayCall += () => Apply(GetPortraitIndex(), false);
                break;
        }
    }

    private static void Apply(int sizeIndex, bool maximize)
    {
        var type = EditorAsm.GetType("UnityEditor.GameView");
        if (type == null) return;

        // Không dùng GetWindow: nó TẠO GameView mới, cướp play mode view của Simulator.
        var views = Resources.FindObjectsOfTypeAll(type);
        if (views.Length == 0) return;

        var view = (EditorWindow)views[0];

        // Method public nhưng nằm trong type internal -> phải có cả Public lẫn NonPublic
        var setSize = type.GetMethod("SizeSelectionCallback",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new[] { typeof(int), typeof(object) }, null);

        if (setSize == null)
        {
            Debug.LogWarning($"{nameof(EditorSceneGameViewSetup)}: không thấy GameView.SizeSelectionCallback, bỏ qua đổi aspect.");
        }
        else if (sizeIndex >= 0 && (sizeIndex == FreeAspectIndex || IsFreeAspect(type, view)))
        {
            setSize.Invoke(view, new object[] { sizeIndex, null });
        }

        // Đổi maximized lúc play làm Unity dựng lại GameView -> canvas layout lại một nhịp
        if (EditorApplication.isPlaying) return;

        if (view.maximized != maximize) view.maximized = maximize;
    }

    // Chỉ ép về portrait khi đang Free Aspect; không đọc được index thì cứ ép như cũ.
    private static bool IsFreeAspect(Type gameViewType, EditorWindow view)
    {
        var prop = gameViewType.GetProperty("selectedSizeIndex",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return prop == null || (int)prop.GetValue(view) == FreeAspectIndex;
    }

    // Tìm size 1080x1920 trong group hiện tại, chưa có thì tự thêm custom size.
    private static int GetPortraitIndex()
    {
        var sizesType = EditorAsm.GetType("UnityEditor.GameViewSizes");
        var sizeType = EditorAsm.GetType("UnityEditor.GameViewSize");
        var sizeTypeEnum = EditorAsm.GetType("UnityEditor.GameViewSizeType");
        if (sizesType == null || sizeType == null || sizeTypeEnum == null) return -1;

        var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var instance = singleton.GetProperty("instance", BindingFlags.Static | BindingFlags.Public)?.GetValue(null);
        var group = sizesType.GetProperty("currentGroup")?.GetValue(instance);
        if (group == null) return -1;

        var groupType = group.GetType();
        var count = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
        var getSize = groupType.GetMethod("GetGameViewSize");
        var widthProp = sizeType.GetProperty("width");
        var heightProp = sizeType.GetProperty("height");

        for (var i = 0; i < count; i++)
        {
            var size = getSize.Invoke(group, new object[] { i });
            if ((int)widthProp.GetValue(size) == PortraitWidth && (int)heightProp.GetValue(size) == PortraitHeight)
                return i;
        }

        var fixedResolution = Enum.Parse(sizeTypeEnum, "FixedResolution");
        var created = Activator.CreateInstance(sizeType, fixedResolution, PortraitWidth, PortraitHeight, PortraitLabel);
        groupType.GetMethod("AddCustomSize").Invoke(group, new[] { created });
        sizesType.GetMethod("SaveToHDD").Invoke(instance, null);

        return (int)groupType.GetMethod("IndexOf").Invoke(group, new[] { created });
    }
}
