using System;
using Falcon.Shared.BaseInGame;
using Imui.Core;

namespace Falcon.Shared.BaseLevelEditor
{
    /// <summary>
    /// Inspector ứng với 1 loại entity — tương tự CustomEditor của Unity.
    /// Đăng ký vào panel theo TargetType; panel lookup theo type của Selected để paint.
    /// </summary>
    public interface IEntityInspector
    {
        /// <summary>Kiểu entity inspector này phụ trách — key trong dict.</summary>
        Type TargetType { get; }

        /// <summary>Entity đang được vẽ (panel set trước khi gọi OnInspectorGUI).</summary>
        IEntityDataContainer Target { get; set; }

        /// <summary>Vẽ inspector cho Target. Tự kiểm tra Target còn sống.</summary>
        void OnInspectorGUI(ImGui gui);
    }
}
