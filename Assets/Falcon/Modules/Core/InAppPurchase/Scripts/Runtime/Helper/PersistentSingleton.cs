/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-03
 */
using UnityEngine;

namespace Falcon.Modules.Core.InAppPurchase.Runtime
{
    /// <summary>
    /// Singleton dạng MonoBehaviour có thể tồn tại vĩnh viễn.
    /// Đảm bảo chỉ có một instance tồn tại và không bị hủy khi chuyển scene.
    /// Tự động tạo nếu không tìm thấy trong scene.
    /// </summary>
    public class PersistentSingleton <T> : MonoBehaviour where T : Component
    {
        protected static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    if (!_instance)
                    {
                        var obj = new GameObject();
                        obj.name = typeof(T).Name + "_AutoCreated";
                        _instance = obj.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Khi Awake, kiểm tra xem có đối tượng nào cùng loại đã tồn tại chưa.
        /// Nếu có rồi thì hủy object hiện tại.
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        /// <summary>
        /// Khởi tạo singleton.
        /// </summary>
        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (!_instance)
            {
                _instance = this as T;
                DontDestroyOnLoad(transform.gameObject);
            }
            else
            {
                if (this != _instance)
                {
                    Destroy(gameObject);
                }
            }
        }
        protected virtual void OnDestroy()
        {
            _instance = null;
        }
    }
}