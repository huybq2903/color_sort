using System;
using System.Collections.Generic;
using Falcon.Shared.BaseLevelEditor;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Bản chụp các điểm của đường đang vẽ hoặc sửa, để undo/redo từng bước của bút.</summary>
    internal sealed class PenState
    {
        public readonly PenAnchor[] anchors;
        public readonly int sel;

        public PenState(IReadOnlyList<PenAnchor> source, int sel)
        {
            anchors = new PenAnchor[source.Count];
            for (var i = 0; i < anchors.Length; i++) anchors[i] = source[i];
            this.sel = sel;
        }

        // Cùng vị trí và tay cầm từng điểm (không xét điểm đang chọn)
        public bool SameAnchors(PenState o)
        {
            if (o.anchors.Length != anchors.Length) return false;
            for (var i = 0; i < anchors.Length; i++)
                if (anchors[i].pos != o.anchors[i].pos || anchors[i].outH != o.anchors[i].outH || anchors[i].inH != o.anchors[i].inH) return false;
            return true;
        }
    }

    /// <summary>Một bước của bút (đặt, kéo, chèn, xoá điểm): undo trả về trạng thái trước, redo áp lại trạng thái sau.</summary>
    internal sealed class PenStateCommand : ICommand
    {
        private readonly Action<PenState> _apply;
        private readonly PenState _before;
        private readonly PenState _after;

        public PenStateCommand(Action<PenState> apply, PenState before, PenState after)
        {
            _apply = apply;
            _before = before;
            _after = after;
        }

        public bool Execute()
        {
            _apply(_after);
            return true;
        }

        public void Undo() => _apply(_before);
    }
}
