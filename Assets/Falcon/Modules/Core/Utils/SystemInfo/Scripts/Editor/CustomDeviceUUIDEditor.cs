/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2025-09-09
     */


using UnityEngine;

namespace Falcon.Modules.Core.SystemInformation.Editor
{
	using UnityEditor;
	using Falcon.Modules.Core.SystemInformation.Runtime;

	public class CustomDeviceUUIDEditor : EditorWindow
    {
	    private string _currentDeviceUUID = "";

	    [MenuItem(@"Falcon/Modules/System Info/Custom Device UUID", false, 301)]
	    public static void ShowWindow()
	    {
		    var window = GetWindow<CustomDeviceUUIDEditor>("Custom Device UUID");
		    window.minSize = new Vector2(500, 100);
		    window.maxSize = new Vector2(500, 100);
	    }

	    private void OnEnable() { LoadCurrentValue(); }

	    void OnGUI()
	    {
		    GUILayout.Label("Device UUID", EditorStyles.boldLabel);
		    GUILayout.Space(5);

		    _currentDeviceUUID = EditorGUILayout.TextField("Custom UUID", _currentDeviceUUID);

		    GUILayout.Space(10);
		    GUI.enabled = !string.IsNullOrEmpty(_currentDeviceUUID);
		    if (GUILayout.Button("Apply Changes"))
		    {
			    EditorPrefs.SetString(SystemInformation.Device.kCustomDeviceUUIDKey, _currentDeviceUUID);
			    EditorUtility.DisplayDialog("Apply Changes", "Device UUID changed!", "OK");
		    }
		    
		    GUI.enabled = true;
		    
		    GUILayout.Space(2);
		    if (GUILayout.Button("Reset Changes"))
		    {
			    _currentDeviceUUID = SystemInfo.deviceUniqueIdentifier + "-editor";
			    EditorPrefs.SetString(SystemInformation.Device.kCustomDeviceUUIDKey, string.Empty);
		    }
	    }

	    private void LoadCurrentValue()
	    {
		    var value = EditorPrefs.GetString(SystemInformation.Device.kCustomDeviceUUIDKey, string.Empty);
		    if (string.IsNullOrEmpty(value))
		    {
			    _currentDeviceUUID = SystemInfo.deviceUniqueIdentifier + "-editor";
		    }
		    else
		    {
			    _currentDeviceUUID = value;
		    }
	    }
    }
}
