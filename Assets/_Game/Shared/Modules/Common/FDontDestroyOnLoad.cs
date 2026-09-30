// Author: Bui Quang Huy
// Email: huybq@falcongames.com
// Company: Falcon Games
// Date: 2026-06-15

using UnityEngine;

namespace Falcon.Shared.Common
{
    public class FDontDestroyOnLoad : MonoBehaviour
    {
        protected void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}