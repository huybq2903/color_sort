/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using System;
using System.Threading.Tasks;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class MainGameObj : MonoBehaviour
    {

        private static MainGameObj _instance;
        private static readonly MyLock kLock = new();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private static MainGameObj FindGameObj()
        {
            var gObject = GameObject.Find("FGameObj");
            if (!gObject)
            {
                gObject = new GameObject("FGameObj");
                if (Application.isPlaying) DontDestroyOnLoad(gObject);
            }

            var instance = gObject.GetOrAddComponent<MainGameObj>();
            instance.enabled = true;
            return instance;
        }

        public static MainGameObj Instance
        {
            get
            {
                if (_instance != null) return _instance;
                using (kLock.Lock())
                {
                    return _instance ??= FindGameObj();
                }
            }
        }
        
        public static void DoTask(Func<Task> task)
        {
            task().ContinueWith(t => {
                if (t.IsFaulted)
                {
                    GameObjLogger.Instance.Error(t.Exception);
                }
            });
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            Instance.gameObject.GetInstanceID();
        }
    }
}