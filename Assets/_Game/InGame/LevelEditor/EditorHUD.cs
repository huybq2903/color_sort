// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-09-09

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Falcon.InGame.UI
{
    /// <summary>HUD thay thế khi playtest từ Level Editor, xem ModeEditorPlaytest.</summary>
    public class EditorHUD : MonoBehaviour
    {
        [SerializeField, Tooltip("Nút quay về EditorScene.")]
        private Button btnBackToEditor;

        private const string EDITOR_SCENE = "EditorScene";

        private void Awake() => btnBackToEditor.onClick.AddListener(() => SceneManager.LoadScene(EDITOR_SCENE));
    }
}
