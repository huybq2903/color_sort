using System;
using Falcon.InGame.Core;
using Falcon.Shared.BaseLevelEditor;

namespace Falcon.InGame.LevelEditor
{
    /// <summary>Undo/redo bằng cách áp lại bản sao tranh trước/sau thao tác.</summary>
    public class PictureSnapshotCommand : ICommand
    {
        private readonly Action<PictureProperty> _apply;
        private readonly PictureProperty _before;
        private readonly PictureProperty _after;

        public PictureSnapshotCommand(Action<PictureProperty> apply, PictureProperty before, PictureProperty after)
        {
            _apply = apply;
            _before = before?.Clone();
            _after = after?.Clone();
        }

        public bool Execute()
        {
            _apply(_after?.Clone());
            return true;
        }

        public void Undo() => _apply(_before?.Clone());
    }
}
