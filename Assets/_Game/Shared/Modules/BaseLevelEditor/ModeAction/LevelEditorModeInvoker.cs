/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2026-01-29
 */

using UnityEngine;

namespace Falcon.Shared.BaseLevelEditor
{
    public class LevelEditorModeInvoker : MonoBehaviour, IEditorManager
    {
        private IModeAction _currentMode;
        
        public void Initialized()
        {
            _currentMode = null;
        }
        
        public void SetMode(IModeAction mode)
        {
            _currentMode?.OnExit();
            _currentMode = mode;
            _currentMode?.OnEnter();
        }
    }
}