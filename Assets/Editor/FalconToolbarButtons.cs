/*
 * 2 nút cho Custom Toolbar (com.opalstudio.customtoolbar).
 * Sau khi compile: Project Settings > Custom Toolbar > thêm element vào group và bật lên.
 */

using Falcon.Modules.Core.SaveLoad.Editor;
using OpalStudio.CustomToolbar.Editor.ToolbarElements;
using UnityEditor;
using UnityEngine;

// Bản sao của ToolbarStyles.CommandButtonStyle trong package (class đó internal nên không dùng lại được).
// Thiếu margin 2px là nút lệch lên trên so với các nút khác trên toolbar.
internal static class FalconToolbarStyles
{
    private static GUIStyle _button;

    // Tạo lazy: new GUIStyle("ToolbarButton") cần GUI skin, chưa có lúc OnInit chạy.
    public static GUIStyle Button => _button ??= new GUIStyle("ToolbarButton")
    {
        alignment = TextAnchor.MiddleCenter,
        margin = new RectOffset(2, 2, 2, 2),
    };
}

internal sealed class ToolbarServerConfig : BaseToolbarElement
{
    private GUIContent _content;
    private bool _widthReady;

    protected override string Name => "Server Config";
    protected override string Tooltip => "Mở cửa sổ đổi server (Resources/NetworkSettings.asset).";

    public override void OnInit()
    {
        _content = new GUIContent("Server Config", this.Tooltip);
    }

    public override void OnDrawInToolbar()
    {
        if (!_widthReady)
        {
            this.Width = FalconToolbarStyles.Button.CalcSize(_content).x + 8f;
            _widthReady = true;
        }

        // Không cho đổi server giữa lúc đang chạy: SaveGame.Clear() trong cửa sổ sẽ xoá data của session.
        this.Enabled = !EditorApplication.isPlayingOrWillChangePlaymode;

        using (new EditorGUI.DisabledScope(!this.Enabled))
        {
            if (GUILayout.Button(_content, FalconToolbarStyles.Button, GUILayout.Width(this.Width)))
            {
                NetworkSettingsWindow.ClearData(); // tên hàm là ClearData nhưng chỉ mở cửa sổ
            }
        }
    }
}

internal sealed class ToolbarClearData : BaseToolbarElement
{
    private GUIContent _content;
    private bool _widthReady;

    protected override string Name => "Clear Data";
    protected override string Tooltip => "Xoá toàn bộ save data (SaveGame.Clear). Có hỏi xác nhận.";

    public override void OnInit()
    {
        _content = new GUIContent("Clear Data", EditorGUIUtility.IconContent("d_TreeEditor.Trash").image, this.Tooltip);
    }

    public override void OnDrawInToolbar()
    {
        if (!_widthReady)
        {
            this.Width = FalconToolbarStyles.Button.CalcSize(_content).x + 8f;
            _widthReady = true;
        }

        if (GUILayout.Button(_content, FalconToolbarStyles.Button, GUILayout.Width(this.Width)))
        {
            SaveLoadConfigsEditor.ClearData(); // dialog xác nhận nằm sẵn trong hàm này
        }
    }
}
