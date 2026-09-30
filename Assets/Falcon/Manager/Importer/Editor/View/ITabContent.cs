/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-02-25
     */


using Falcon.Manager.Shared;
using UnityEditor;

// ReSharper disable once CheckNamespace
namespace Falcon.Manager.Importer
{
    public interface ITabContent
    {
        void OnGUI(EditorWindow window);
        bool TryGetNextController(out IViewController controller);
    }
}
