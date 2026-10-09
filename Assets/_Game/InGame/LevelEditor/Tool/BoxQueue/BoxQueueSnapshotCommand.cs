using System;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Undo/redo bằng cách áp lại bản sao hàng chờ hộp trước/sau thao tác.</summary>
    public class BoxQueueSnapshotCommand : ICommand
    {
        private readonly Action<BoxQueueProperty> _apply;
        private readonly BoxQueueProperty _before, _after;

        public BoxQueueSnapshotCommand(Action<BoxQueueProperty> apply, BoxQueueProperty before, BoxQueueProperty after)
        {
            _apply = apply;
            _before = before.Clone();
            _after = after.Clone();
        }

        public bool Execute()
        {
            _apply(_after.Clone());
            return true;
        }

        public void Undo() => _apply(_before.Clone());
    }
}
