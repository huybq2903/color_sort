using UnityEngine;
using System.Collections;
/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-04
 */
namespace Falcon.Modules.Core.Network
{
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;

        // Singleton pattern to ensure there's only one instance of CoroutineRunner
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject obj = new GameObject("Falcon_Coroutine_Runner");
                    _instance = obj.AddComponent<CoroutineRunner>();

                    DontDestroyOnLoad(obj);
                    obj.hideFlags = HideFlags.HideInHierarchy;
                }
                return _instance;
            }
        }

        public Coroutine StartRoutine(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        public Coroutine RunEverySecond(System.Action action)
        {
            return Instance.StartCoroutine(RunEverySecondCoroutine(action));
        }

        private IEnumerator RunEverySecondCoroutine(System.Action action)
        {
            while (true)
            {
                action?.Invoke();
                yield return new WaitForSeconds(1f); // Wait for 1 second
            }
        }
    }

}
