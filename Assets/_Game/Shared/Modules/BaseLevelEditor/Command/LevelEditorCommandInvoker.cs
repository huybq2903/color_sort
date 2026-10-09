/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-19
 */

using System.Collections.Generic;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

// ReSharper disable once CheckNamespace
namespace Falcon.Shared.BaseLevelEditor
{
    //Quản lý việc Undo/Redo
    public class LevelEditorCommandInvoker : MonoBehaviour, IEditorManager
    {
        private LevelEditorInputHandler _input;

        // Hai ngăn xếp cho Undo và Redo
        private readonly Stack<ICommand> _undoStack = new();
        private readonly Stack<ICommand> _redoStack = new();
        
        public void Initialized()
        {
            _input = LevelEditorManager.Get<LevelEditorInputHandler>();
            BindInput();
        }

        private void BindInput()
        {
            _input.HotKeyDown(Key.Z).Subscribe(Undo);
            _input.HotKeyDown(Key.Y).Subscribe(Redo);
        }

        private List<ICommand> _group;

        /// <summary>Các lệnh thực thi từ đây tới EndGroup gộp thành một bước undo.</summary>
        public void BeginGroup() => _group = new List<ICommand>();

        public void EndGroup()
        {
            var g = _group;
            _group = null;
            if (g == null || g.Count == 0) return;
            _undoStack.Push(g.Count == 1 ? g[0] : new CompositeCommand(g));
            _redoStack.Clear();
        }

        private sealed class CompositeCommand : ICommand
        {
            private readonly List<ICommand> _commands;

            public CompositeCommand(List<ICommand> commands) => _commands = commands;

            public bool Execute()
            {
                foreach (var c in _commands) c.Execute();
                return true;
            }

            public void Undo()
            {
                for (var i = _commands.Count - 1; i >= 0; i--) _commands[i].Undo();
            }
        }

        // 1. Thực thi một lệnh mới
        public void ExecuteCommand(ICommand command)
        {
            if (!command.Execute()) return; // lệnh bị từ chối -> không ghi history
            if (_group != null)
            {
                _group.Add(command);
                return;
            }

            _undoStack.Push(command); // Lưu vào lịch sử
            _redoStack.Clear();       // Khi có lệnh mới thì mất chuỗi Redo cũ
        }

        // 2. Hoàn tác
        private void Undo(LevelEditorHotkey hotkey)
        {
            if (_undoStack.Count > 0)
            {
                var cmd = _undoStack.Pop();
                cmd.Undo();           // Đảo ngược lệnh
                _redoStack.Push(cmd); // Đẩy sang Redo
            }
        }

        // 3. Làm lại
        private void Redo(LevelEditorHotkey hotkey)
        {
            if (_redoStack.Count > 0)
            {
                var cmd = _redoStack.Pop();
                if (cmd.Execute())    // Chạy lại lệnh; nếu giờ bị từ chối thì bỏ luôn
                    _undoStack.Push(cmd); // Đẩy về Undo
            }
        }
        
        public void ClearHistory()
        {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        // Bỏ mọi lệnh thoả điều kiện khỏi cả hai ngăn xếp, giữ nguyên thứ tự các lệnh còn lại
        public void RemoveWhere(System.Predicate<ICommand> match)
        {
            Filter(_undoStack, match);
            Filter(_redoStack, match);
        }

        private static void Filter(Stack<ICommand> stack, System.Predicate<ICommand> match)
        {
            var keep = new List<ICommand>(); // thứ tự lấy ra: đỉnh ngăn xếp trước
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (!match(c)) keep.Add(c);
            }
            for (var i = keep.Count - 1; i >= 0; i--) stack.Push(keep[i]);
        }
    }
}