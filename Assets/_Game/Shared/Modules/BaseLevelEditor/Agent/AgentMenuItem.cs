namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>Mục Trợ lý trên menu bar: mở hoặc đóng cửa sổ trợ lý.</summary>
    [MenuItem("Trợ lý", 90)]
    public class AgentMenuItem : IMenuItem
    {
        public void OnClick() => LevelEditorManager.Get<LevelEditorAgent>()?.Toggle();
    }
}
