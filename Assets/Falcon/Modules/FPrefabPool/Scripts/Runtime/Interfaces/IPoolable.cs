/*
 * Author: minhddv
 * Email: minhddv@falcongames.com
 * Company: Falcon Games
 * Date: 2026-05-18
 */

namespace Falcon.Modules.FPrefabPool.Runtime
{
    public interface IPoolable
    {
        /// <summary>Gọi sau khi instance đã active + parent đúng. Thứ tự: reparent → SetActive(true) → OnSpawned.</summary>
        void OnSpawned();

        /// <summary>
        /// Gọi trước reparent về holder + SetActive(false). Thứ tự: OnReleased → reparent holder → SetActive(false).
        /// LƯU Ý: lúc này GO còn active, parent vẫn là parent gameplay (KHÔNG phải holder); OnDisable fire SAU OnReleased.
        /// CHỈ chạy sau một OnSpawned thành công — nếu OnSpawned throw, instance được trả về pool mà KHÔNG gọi OnReleased.
        /// </summary>
        void OnReleased();
    }
}
