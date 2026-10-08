using System;
using System.Collections.Generic;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.BaseLevelEditor;
using Imui.Core;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Trạng thái của panel Inspector tranh: mỗi kiểu con ứng với một inspector.</summary>
    public abstract class PictureSelection : IEntityDataContainer
    {
        private static readonly Dictionary<Type, PictureSelection> Cache = new();

        public EntityData BaseData { get; private set; }

        /// <summary>Selection dùng lại theo kiểu, gán entity đang được chọn.</summary>
        public static T Of<T>(EntityData data) where T : PictureSelection, new()
        {
            if (!Cache.TryGetValue(typeof(T), out var s)) Cache[typeof(T)] = s = new T();
            s.BaseData = data;
            return (T)s;
        }
    }

    /// <summary>Nét trang trí đang xem: id lưu trong PictureProperty.lineIds, index là vị trí hiện tại.</summary>
    public sealed class LineData : EntityData
    {
        public int index = -1;
    }

    /// <summary>Lỗ đang chọn (click biên lỗ): region = chỉ số mảnh, hole = chỉ số lỗ trong mảnh.</summary>
    public sealed class HoleData : EntityData
    {
        public int region = -1, hole = -1;
    }

    /// <summary>Thông tin chung của tranh (chỉ trong editor, không lưu vào level).</summary>
    public sealed class PictureInfoData : EntityData
    {
        public PictureInfoData() => id = "picture";
    }

    public sealed class NoPictureSelection : PictureSelection { }
    public sealed class PieceSelection : PictureSelection { }
    public sealed class LineSelection : PictureSelection { }
    public sealed class HoleSelection : PictureSelection { }
    public sealed class PictureInfoSelection : PictureSelection { }

    /// <summary>Inspector của một trạng thái tranh: vẽ bằng callback, tiêu đề cố định.</summary>
    public sealed class PictureInspector<T> : IEntityInspector where T : PictureSelection
    {
        private readonly Action _draw;

        public PictureInspector(Action draw) => _draw = draw;

        public Type TargetType => typeof(T);
        public IEntityDataContainer Target { get; set; }
        public string Title => "Inspector";

        public void OnInspectorGUI(ImGui gui) => _draw();
    }
}
