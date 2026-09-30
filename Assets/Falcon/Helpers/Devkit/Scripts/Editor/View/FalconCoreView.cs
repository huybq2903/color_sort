/*
 * Author: leehuyyhoangg
 * Email: hoanglh@falcongames.com
 * Company: Falcon Games
 * Date: 2025-06-12
 */

using Unity.CodeEditor;
using UnityEditor;
// ReSharper disable once CheckNamespace
namespace Falcon.Helpers.Devkit
{
    public class FalconCoreView : EditorWindow
    {
        [MenuItem("Falcon/Helper/Devkit/DebugLog/Enable")]
        public static void EnableDebugLog()
        {
            DefineSymbolRepository.Add("FALCON_LOG_DEBUG");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
                
            EditorUtility.RequestScriptReload();
            CodeEditor.CurrentEditor.Initialize(CodeEditor.CurrentEditorInstallation);
        }
        
        [MenuItem("Falcon/Helper/Devkit/DebugLog/Disable")]
        public static void DisableDebugLog()
        {
            DefineSymbolRepository.Remove("FALCON_LOG_DEBUG");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
                
            EditorUtility.RequestScriptReload();
            CodeEditor.CurrentEditor.Initialize(CodeEditor.CurrentEditorInstallation);
        }
        
        [MenuItem("Falcon/Helper/Devkit/ClearData")]
        public static void ClearData()
        {
            MySingletonService.Instance<IDataPool>().Clear();
            if(MainGameObj.Instance.gameObject) DestroyImmediate(MainGameObj.Instance.gameObject);
            BaseSystemLogger.Instance.Info("Clear Data Successfully");
        }

    }
}