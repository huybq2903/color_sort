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

        // 1. Thực thi một lệnh mới
        public void ExecuteCommand(ICommand command)
        {
            if (!command.Execute()) return; // lệnh bị từ chối -> không ghi history

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
    }
}