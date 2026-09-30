/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System;
using UnityEditor;
using UnityEngine;
// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    public abstract class AViewController : IViewController
    {
        public abstract void Edit(EditorWindow window);

        public abstract bool TryMoveNextController(out IViewController viewController);
        
        protected static void GUIHorizon(Action action, GUIStyle styles = null)
        {
            // Cho phép tiếp tục - đây là cách thường dùng
            if (styles == null)
            {
                EditorGUILayout.BeginHorizontal();
            }
            else
            {
                EditorGUILayout.BeginHorizontal(styles);
            }
            action.Invoke();
            EditorGUILayout.EndHorizontal();
        }

        protected static void GUIVertical(Action action, GUIStyle styles = null)
        {
            if (styles == null)
            {
                EditorGUILayout.BeginVertical();
            }
            else
            {
                EditorGUILayout.BeginVertical(styles);
            }
            action.Invoke();
            EditorGUILayout.EndVertical();
        }

        public virtual void Dispose()
        {
        }
    }
}