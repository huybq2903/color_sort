using System.Collections;
using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Falcon.Helpers.UI
{
    public class LoadPrefabCoroutineManager : MonoBehaviour
    {
        static LoadPrefabCoroutineManager pInstance
        {
            get{
                if (mInstance==null)
                {
                    GameObject GO = new GameObject( "_UILoadPrefab" );
                    GO.hideFlags = HideFlags.HideAndDontSave;
                    mInstance = GO.AddComponent<LoadPrefabCoroutineManager>();
                    if (Application.isPlaying)
                        DontDestroyOnLoad(GO);
                }
                return mInstance;
            }
        }
        
        static LoadPrefabCoroutineManager mInstance;

        public List<Coroutine> runningCoroutines = new List<Coroutine>();

        private void Awake()
        {
            if (Application.isPlaying)
                DontDestroyOnLoad(gameObject);
        }

        public static Coroutine Start(IEnumerator coroutine)
        {
            #if UNITY_EDITOR
                // Special case to allow coroutines to run in the Editor
                if (!Application.isPlaying)
                {
                    EditorApplication.CallbackFunction delg=null;
                    delg = delegate
                    {
                        if (!coroutine.MoveNext())
                            EditorApplication.update -= delg;
                    };
                    EditorApplication.update += delg;
                    return null;
                }
            #endif

            var coroutineInstance = pInstance.StartCoroutine(coroutine);
            pInstance.runningCoroutines.Add(coroutineInstance);
            return coroutineInstance;
        }

        public static void Stop(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                pInstance.StopCoroutine(coroutine);
                pInstance.runningCoroutines.Remove(coroutine);
            }
        }
    }
}