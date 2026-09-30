/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-09
 */

using System;
using System.Linq;
using BayatGames.SaveGamePro;
using Falcon.Modules.Core.Network;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

[InitializeOnLoad]
public class NetworkSettingsWindow : EditorWindow
{
    // Options[0] LUÔN là server của bản release: NetworkSettingBuildProcessor ghi đè serverIp về nó
    // ở mọi build non-development. Điền IP thật của game rồi thêm server test/local bên dưới.
    public static readonly ServerOption[] Options =
    {
        new("prod", "192.168.0.0"),
    };
    public const string SoResourcesPath = "NetworkSettings"; // Resources/NetworkSettings.asset
    private const string EditorPrefsKey = "NetworkSettings_SelectedIndex";

    private int _selectedIndex = 0;
    private NetworkSettings _so;
    private SerializedObject _soSerialized;
    private SerializedProperty _serverIpProp;

    [MenuItem("Tools/Server Config")]
    public static void ClearData()
    {
        var win = GetWindow<NetworkSettingsWindow>("Server Switcher");
        win.minSize = new Vector2(350, 60);
        win.Show();
    }

    private void OnEnable()
    {
        _so = Resources.Load<NetworkSettings>(SoResourcesPath);
        if (_so != null)
        {
            _soSerialized = new SerializedObject(_so);
            _serverIpProp = _soSerialized.FindProperty("serverIp");
        }

        _selectedIndex = EditorPrefs.GetInt(EditorPrefsKey, 0);
        if (_selectedIndex < 0 || _selectedIndex >= Options.Length)
        {
            // _serverIpProp null khi thiếu Resources/NetworkSettings.asset -> OnGUI đã báo lỗi, đừng NRE ở đây
            _selectedIndex = _serverIpProp == null
                ? 0
                : Mathf.Max(0, Array.FindIndex(Options, o => o.ip == _serverIpProp.stringValue));
        }
    }

    private void OnGUI()
    {
        if (!_so || _serverIpProp == null)
        {
            EditorGUILayout.HelpBox(
                "Không tìm thấy Resources/NetworkSettings.asset.\n" +
                "Tạo SO 'NetworkSettings' và đặt trong Resources.",
                MessageType.Error);
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("ScriptableObject", _so, typeof(NetworkSettings), false);
        }

        var keys = Options.Select(o => o.key).ToArray();
        _selectedIndex = EditorGUILayout.Popup("Server", _selectedIndex, keys);

        EditorGUILayout.LabelField("IP:", Options[_selectedIndex].ip);
        EditorGUILayout.Space();

        if (GUILayout.Button("Save And Clear Cache") && EditorUtility.DisplayDialog(
                "Switch Server",
                "Are you sure you want to clear all saved data and switch server?",
                "Yes",
                "No"))
        {
            ApplySelectionToSO();
            EditorPrefs.SetInt(EditorPrefsKey, _selectedIndex);
            EditorApplication.isPlaying = false;
            SaveGame.Clear();
        }

        EditorGUILayout.HelpBox(
            $"ServerIp hiện tại: {Options[_selectedIndex].ip}",
            MessageType.Info);
    }

    private static void ApplySavedSelection()
    {
        var savedIndex = EditorPrefs.GetInt(EditorPrefsKey, 0);
        if (savedIndex < 0 || savedIndex >= Options.Length)
            savedIndex = 0;

        var so = Resources.Load<NetworkSettings>(SoResourcesPath);
        if (so != null)
        {
            var soSerialized = new SerializedObject(so);
            var serverIpProp = soSerialized.FindProperty("serverIp");
            var desiredIp = Options[savedIndex].ip;

            soSerialized.Update();
            if (serverIpProp.stringValue != desiredIp)
            {
                serverIpProp.stringValue = desiredIp;
                soSerialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void ApplySelectionToSO()
    {
        if (_soSerialized == null || _serverIpProp == null) return;

        var desired = Options[_selectedIndex].ip;
        _soSerialized.Update();

        if (_serverIpProp.stringValue != desired)
        {
            Undo.RecordObject(_so, "Change serverIp (NetworkSettings)");
            _serverIpProp.stringValue = desired;
            _soSerialized.ApplyModifiedProperties();

            EditorUtility.SetDirty(_so);
            AssetDatabase.SaveAssets();
        }
    }
}

[Serializable]
public struct ServerOption
{
    public string key; // key dùng hiển thị và lưu
    public string ip;
    public ServerOption(string key, string ip)
    {
        this.key = key;
        this.ip = ip;
    }
}

public class NetworkSettingBuildProcessor : BuildPlayerProcessor
{
    public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
    {
        if (EditorUserBuildSettings.development)
        {
            Debug.Log("Development build detected - keeping current server settings");
            return;
        }

        var so = Resources.Load<NetworkSettings>(NetworkSettingsWindow.SoResourcesPath);
        if (so)
        {
            var soSerialized = new SerializedObject(so);
            var serverIpProp = soSerialized.FindProperty("serverIp");

            var desired = NetworkSettingsWindow.Options[0].ip;
            soSerialized.Update();

            if (serverIpProp.stringValue != desired)
            {
                serverIpProp.stringValue = desired;
                soSerialized.ApplyModifiedProperties();

                EditorUtility.SetDirty(so);
                AssetDatabase.SaveAssets();
                Debug.Log($"Production build - switched to production server: {desired}");
            }
        }
    }
}