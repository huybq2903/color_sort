using System;

namespace Falcon.Shared.BaseInGame
{
    public interface ISubManager
    {
        public void Initialized() { }
    }

    /// <summary>
    /// Đánh dấu field kiểu ISubManager để InGameManager tự inject khi Awake,
    /// thay cho việc gọi InGameManager.Get<T>() thủ công trong Initialized().
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class InjectAttribute : Attribute { }
}