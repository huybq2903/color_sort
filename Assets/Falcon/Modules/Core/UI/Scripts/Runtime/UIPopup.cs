/*
 * Author: dongvv
 * Email: dongvv@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
*/

using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace Falcon.Modules.Core.UI.Runtime
{
    public class UIPopup : UIBase
    {
        [HorizontalGroup("TypePopup"), ToggleLeft]
        public bool isFullScreen;

        [HorizontalGroup("TypePopup"), ToggleLeft]
        public bool isHalfScreen;

        // OnUIPopupStart has been broadcast for the first time
        public bool HasStarted { get; private set; }

        public void MarkStarted() => HasStarted = true;

        public override void ChangeVisibility(bool visible)
        {
            if (!_initialized)
                InitializeElements();

            base.visible = visible;

            if (visible) gameObject.SetActive(true);

            if (visible)
            {
                ShowAnimation();
                onShow?.Invoke();

                StartCoroutine(IEOnShowLayout());
                IEnumerator IEOnShowLayout()
                {
                    yield return new WaitForEndOfFrame();
                    onShowLayoutCompleted?.Invoke();
                }
            }
            else if (!visible)
            {
                HideAnimation();
                onHide?.Invoke();
            }

            if (deactivateWhileInvisible)
            {
                if (!visible)
                    Invoke(nameof(DeactivateMe), hidingTime);
                else
                    CancelInvoke(nameof(DeactivateMe));
            }
        }

#if UNITY_EDITOR      
        [HorizontalGroup("Buttons"), Button(ButtonSizes.Medium)]
        private void CreateScene()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene);
            scene.name = gameObject.name;
            UnityEditor.PrefabUtility.InstantiatePrefab(gameObject, scene);

            string path = UnityEditor.EditorUtility.SaveFilePanel(
                "Save Popup Scene",
                "Assets",
                scene.name,
                "unity");

            if (!string.IsNullOrEmpty(path))
            {
                // Convert absolute path to relative project path if needed
                if (path.StartsWith(UnityEngine.Application.dataPath))
                {
                    path = "Assets" + path.Substring(UnityEngine.Application.dataPath.Length);
                }
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path);
                UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
            }
        }

        [HorizontalGroup("Buttons"), Button(ButtonSizes.Medium)]
        private void AddHandlerAddressable()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog(
                "Add Addressable Handler",
                "This will add the 'UIPopupAddressableHandler' component to this GameObject if it doesn't already exist.\n\nDo you want to proceed?",
                "Yes, Add Handler",
                "Cancel")) return;

            if (GetComponent<UIPopupAddressableHandler>() == null) gameObject.AddComponent<UIPopupAddressableHandler>();
        }
#endif
    }
}
