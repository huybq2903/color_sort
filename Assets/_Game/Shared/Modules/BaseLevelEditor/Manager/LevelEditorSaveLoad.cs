/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-12-22
 */

using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using Falcon.Shared.BaseInGame;
using Falcon.Shared.Common;
using SFB;
using Sirenix.OdinInspector;
using UnityEngine;
using System.Text.RegularExpressions;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Falcon.Shared.BaseLevelEditor
{
    public class SaveCondition
    {
        public string GetFalseMessage { get; }
        public Func<bool> IsNotValid { get; }

        public SaveCondition(string falseMessage, Func<bool> isNotValid)
        {
            GetFalseMessage = falseMessage;
            IsNotValid = isNotValid;
        }
    }

    public class LevelEditorSaveLoad : MonoBehaviour, IEditorManager
    {
        private const string FolderPathKey = "LevelEditor_LastFolderPath";

        [SerializeField] private string folderPath;
        [ShowInInspector, ReadOnly] private LevelData _levelData;

        private LevelEditorCommandInvoker _commandInvoker;
        private string _originJsonData;
        private string _cachedFolderPath;
        private readonly List<SaveCondition> _saveConditions = new();
        private readonly List<string> _failedMessages = new();

        private void Awake()
        {
            _cachedFolderPath = PlayerPrefs.GetString(FolderPathKey, null);
        }

        public void Initialized()
        {
            _commandInvoker = LevelEditorManager.Get<LevelEditorCommandInvoker>();
            DOVirtual.DelayedCall(0.1f, ReloadLevel);
        }

        private void ReloadLevel()
        {
            if (DataTempExtensions<bool>.Get("from_editor"))
            {
                var json = DataTempExtensions<string>.Get("level_data");
                var path = DataTempExtensions<string>.Get("level_path");
                _levelData = LevelData.FromJson(json);
                _originJsonData = DataTempExtensions<string>.Get("level_data_origin");
                LevelName = DataTempExtensions<string>.Get("level_name");
                FilePath = string.IsNullOrEmpty(path) ? null : path;

                DataTempExtensions<bool>.Remove("from_editor");
                DataTempExtensions<string>.Remove("level_data");
                DataTempExtensions<string>.Remove("level_data_origin");
                DataTempExtensions<string>.Remove("level_name");
                DataTempExtensions<string>.Remove("level_path");

                Messenger<OnLoadLevel>.Emit(new OnLoadLevel { levelData = _levelData });
                UpdateChangesStatus();
            }
        }

        /// <summary>Hàm chụp preview của level hiện tại (null = không có preview), gọi mỗi lần Save.</summary>
        public Func<Texture2D> ThumbnailCapture { get; set; }

        public string LevelName { get; private set; }
        public string CurrentPath => FilePath;
        public bool HasUnsavedChanges { get; private set; }
        public bool HasError { get; private set; }
        private string FilePath { get; set; }

        private void UpdateChangesStatus()
        {
            HasUnsavedChanges = _levelData != null && _levelData.ToJson() != _originJsonData;
        }

        public List<string> GetFailedConditions() => GetFailedSaveConditionIndices();

        public void RegisterSaveCondition(SaveCondition condition)
        {
            if (condition == null) return;
            if (!_saveConditions.Contains(condition)) _saveConditions.Add(condition);
        }

        public void UnregisterSaveCondition(SaveCondition condition)
        {
            if (condition == null) return;
            _saveConditions.Remove(condition);
        }

        private List<string> GetFailedSaveConditionIndices()
        {
            _failedMessages.Clear();
            foreach (var condition in _saveConditions)
            {
                if (condition.IsNotValid()) _failedMessages.Add(condition.GetFalseMessage);
            }
            return _failedMessages;
        }

        public void NewFile()
        {
            LoadLevel(new LevelData());
            FilePath = null;
            UpdateChangesStatus();
            _commandInvoker.ClearHistory();
            LevelName = "New Level";
        }

        public void OpenFile()
        {
            var initialFolder = string.IsNullOrEmpty(_cachedFolderPath) ? folderPath : _cachedFolderPath;
            var extensions = new[] { new ExtensionFilter("Text", "txt") };
            var paths = StandaloneFileBrowser.OpenFilePanel("Open Level", initialFolder, extensions, false);
            if (paths is { Length: > 0 } && !string.IsNullOrEmpty(paths[0]))
            {
                var path = paths[0];
                _cachedFolderPath = Path.GetDirectoryName(path);
                PlayerPrefs.SetString(FolderPathKey, _cachedFolderPath);
                var json = File.ReadAllText(path).Decompress();
                var levelData = LevelData.FromJson(json) ?? new LevelData();
                FilePath = path;
                LoadLevel(levelData);
                LevelName = Path.GetFileNameWithoutExtension(path);
                AddRecent(path, false);
            }
        }

        public string OpenFile(string path)
        {
            var json = File.ReadAllText(path).Decompress();
            var levelData = LevelData.FromJson(json) ?? new LevelData();
            FilePath = path;
            LoadLevel(levelData);
            LevelName = Path.GetFileNameWithoutExtension(path);
            AddRecent(path, false);
            return LevelName;
        }

        public void SaveFileAs(out List<string> failed)
        {
            failed = GetFailedSaveConditionIndices();
            if (failed.Count > 0) return;
            var initialFolder = string.IsNullOrEmpty(_cachedFolderPath) ? folderPath : _cachedFolderPath;

            var path = StandaloneFileBrowser.SaveFilePanel("Save Level", initialFolder, LevelName, "txt");
            if (!string.IsNullOrEmpty(path))
            {
                _cachedFolderPath = Path.GetDirectoryName(path);
                PlayerPrefs.SetString(FolderPathKey, _cachedFolderPath);
                File.WriteAllText(path, _levelData.ToJson().Compress());
                FilePath = path;
                _originJsonData = _levelData.ToJson();
                UpdateChangesStatus();
                LevelName = Path.GetFileNameWithoutExtension(path);
                AddRecent(path, true);
            }
        }

        public void SaveFileImmediately(out List<string> failed)
        {
            failed = GetFailedSaveConditionIndices();
            if (failed.Count > 0) return;

            if (string.IsNullOrEmpty(FilePath))
            {
                SaveFileAs(out failed);
                return;
            }

            File.WriteAllText(FilePath, _levelData.ToJson().Compress());
            _originJsonData = _levelData.ToJson();
            UpdateChangesStatus();
            AddRecent(FilePath, true);
        }

        private void AddRecent(string path, bool withThumb)
        {
            Texture2D thumb = null;
            try
            {
                if (withThumb) thumb = ThumbnailCapture?.Invoke();
                RecentLevels.Touch(path, thumb);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Recent: {e.Message}");
            }
            finally
            {
                if (thumb) Destroy(thumb);
            }
        }

        public void SaveTempToPlay()
        {
            DataTempExtensions<string>.Set("level_data", _levelData.ToJson());
            DataTempExtensions<string>.Set("level_data_origin", _originJsonData);
            DataTempExtensions<string>.Set("level_name", LevelName);
            DataTempExtensions<string>.Set("level_path", FilePath ?? string.Empty);
            DataTempExtensions<bool>.Set("from_editor", true);

            var match = Regex.Match(LevelName ?? "", @"\d+");
            if (match.Success && int.TryParse(match.Value, out var level))
                Messenger<OnSetTempLevel>.Emit(new OnSetTempLevel { level = level });
            else
                Messenger<OnSetTempLevel>.Emit(new OnSetTempLevel { level = 999 });
        }

        private void LoadLevel(LevelData levelData)
        {
            _levelData = levelData.Clone();

            Messenger<OnLoadLevel>.Emit(new OnLoadLevel { levelData = _levelData });
            _originJsonData = _levelData.ToJson();
            UpdateChangesStatus();
            _commandInvoker.ClearHistory();
        }

        private void OnSavePropertyData(OnSavePropertyData data)
        {
            _levelData.SetProperty(data.propertyData);
            UpdateChangesStatus();
        }

        private void OnRemovePropertyData(OnRemovePropertyData data)
        {
            if (_levelData == null || string.IsNullOrEmpty(data.propertyType)) return;

            _levelData.properties.Remove(data.propertyType);
            UpdateChangesStatus();
        }

        public void UpdateErrorStatus()
        {
            HasError = GetFailedSaveConditionIndices().Count > 0;
        }

        private void OnEnable()
        {
            Messenger<OnSavePropertyData>.Register(OnSavePropertyData);
            Messenger<OnRemovePropertyData>.Register(OnRemovePropertyData);
        }

        private void OnDisable()
        {
            Messenger<OnSavePropertyData>.Unregister(OnSavePropertyData);
            Messenger<OnRemovePropertyData>.Unregister(OnRemovePropertyData);
        }
    }

    public struct OnLoadLevel { public LevelData levelData; }
    public struct OnSavePropertyData { public PropertyData propertyData; }
    public struct OnRemovePropertyData { public string propertyType; }
    public struct OnSetTempLevel { public int level; }

    // Compress/Decompress đã chuyển sang Falcon.Shared.BaseInGame.LevelCompressor (asmdef này editor-only).
    public static class LevelCompressorMenu
    {
#if UNITY_EDITOR
        private const string DefaultLevelsFolder = "Assets/_Game/Shared/Levels/Resources";

        [UnityEditor.MenuItem("Falcon/Levels/Compress Txt In Folder")]
        private static void CompressTxtInFolder()
        {
            string folder = GetSelectedFolderPath() ?? DefaultLevelsFolder;
            if (!AssetDatabase.IsValidFolder(folder))
            {
                Debug.LogWarning($"LevelCompressor: Folder not found: {folder}");
                return;
            }

            string fullPath = ToFullPath(folder);
            string[] files = Directory.GetFiles(fullPath, "*.txt", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Debug.LogWarning($"LevelCompressor: No .txt files in {folder}");
                return;
            }

            AssetDatabase.StartAssetEditing();
            int compressedCount = 0;
            int skippedCount = 0;
            try
            {
                foreach (string file in files)
                {
                    string input = File.ReadAllText(file);
                    if (LevelCompressor.IsAlreadyCompressed(input))
                    {
                        skippedCount++;
                        continue;
                    }
                    string output = input.Compress();
                    if (string.IsNullOrEmpty(output)) continue;
                    File.WriteAllText(file, output);
                    compressedCount++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            Debug.Log($"LevelCompressor: Compressed {compressedCount} .txt files in {folder} (skipped {skippedCount})");
        }

        private static string GetSelectedFolderPath()
        {
            var obj = Selection.activeObject;
            if (!obj) return null;
            var path = AssetDatabase.GetAssetPath(obj);
            return AssetDatabase.IsValidFolder(path) ? path : null;
        }

        private static string ToFullPath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            if (projectRoot != null)
                return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
            return null;
        }
#endif
    }
}
