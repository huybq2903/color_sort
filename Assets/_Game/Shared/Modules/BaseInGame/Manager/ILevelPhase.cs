/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-07-31
 */

using Cysharp.Threading.Tasks;

namespace Falcon.Shared.BaseInGame
{
    // Chuỗi pha khi start level, chạy tuần tự trong InGameManager.Start(). Manager chỉ implement pha nó cần.

    /// <summary>LevelData sẵn sàng, chưa entity nào. Chỗ duy nhất đổi được data/prefab của chúng.</summary>
    public interface ILevelBeforeSpawn
    {
        UniTask BeforeSpawn();
    }

    /// <summary>Dựng entity và layout. Cấm animate.</summary>
    public interface ILevelSpawn
    {
        UniTask Spawn();
    }

    /// <summary>Entity đã có mặt, chỗ bám lên chúng. Cấm animate. Xong pha này trạng thái là chuẩn.</summary>
    public interface ILevelAfterSpawn
    {
        UniTask AfterSpawn();
    }
}
