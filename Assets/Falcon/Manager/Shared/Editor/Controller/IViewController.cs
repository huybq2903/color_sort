/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-19
 */

using System;
using UnityEditor;
// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Shared
{
    public interface IViewController : IDisposable
    {
        void Edit(EditorWindow window);
        bool TryMoveNextController(out IViewController viewController);
    }
}