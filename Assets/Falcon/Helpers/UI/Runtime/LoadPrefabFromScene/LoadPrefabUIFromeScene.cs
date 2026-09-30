using System;
using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Falcon.Helpers.UI
{
    public class LoadPrefabUIFromeScene : MonoBehaviour
    {
        public bool isStartOnAwake = true;
        public string sceneName;
        public UnityEvent onLoaded;

        private void Awake()
        {
            if (!isStartOnAwake)
            {
                return;
            }

            Load();
        }

        public void Load()
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                return;
            }

            LoadPrefabCoroutineManager.Start(IELoadPopupFromScene(sceneName));
        }

        IEnumerator IELoadPopupFromScene(string popupName)
        {
            var asyncLoad = SceneManager.LoadSceneAsync(popupName, LoadSceneMode.Additive);
            if (asyncLoad == null) yield break;
            while (!asyncLoad.isDone) yield return new WaitForEndOfFrame();
            
            try
            {
                var scenePopup = SceneManager.GetSceneByName(popupName);
                var rootObjects = scenePopup.GetRootGameObjects();
                if (rootObjects != null && rootObjects.Length > 0)
                {
                    var prefab = rootObjects[0].transform;
                    prefab.SetParent(transform);

                    var rect = prefab.GetComponent<RectTransform>();
                    rect.offsetMin = Vector2.zero;
                    rect.offsetMax = Vector2.zero;
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition3D = Vector3.zero;

                    onLoaded?.Invoke();
                }

                
            }
            catch
            {
                Debug.LogWarning($"Load prefab from scene {popupName} failed!");
            }

            yield return new WaitForEndOfFrame();
            
            try
            {
                SceneManager.UnloadSceneAsync(popupName);
            }
            catch
            {
                Debug.LogWarning($"Unload scene {popupName} failed!");
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

                    //Remove This script after create scene
                    var prefabInScene = UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath(path).GetRootGameObjects()[0];
                    var loadPrefabFromScene = prefabInScene.GetComponent<LoadPrefabFromScene>();
                    DestroyImmediate(loadPrefabFromScene);
                }
            }
    #endif
    }
}
