/*
     * Author: ngocdx
     * Email: ngocdx@falcongames.com
     * Company: Falcon Games
     * Date: 2026-02-27
     */


using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace Falcon.Manager.Importer
{
	public static class UnityPackageCustomImporter
	{
        public static void ShowPreviewWindow(string packagePath)
        {
            var entries = ScanPackage(packagePath);
            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("Notice", "No new items found in package.", "Ok");
                return;
            }

            ImportPreviewWindow.Open(packagePath, entries);
        }

        /// <summary>
        /// Scans the package and returns a dict of targetPath -> tempRoot for new-only items.
        /// </summary>
        public static Dictionary<string, string> ScanPackage(string packagePath)
        {
            byte[] tarData = DecompressGZip(packagePath);
            var result = new Dictionary<string, string>();

            using (var stream = new MemoryStream(tarData))
            {
                while (stream.Position < stream.Length)
                {
                    var header = ReadTarHeader(stream);
                    if (header == null) break;

                    string entryName = header.Value.name;
                    long size = header.Value.size;

                    if (size == 0)
                    {
                        SkipPadding(stream, size);
                        continue;
                    }

                    byte[] data = new byte[size];
                    stream.Read(data, 0, (int)size);
                    SkipPadding(stream, size);

                    var parts = entryName.Split('/');
                    if (parts.Length != 2) continue;

                    string hash = parts[0];
                    string type = parts[1];

                    string tempRoot = Path.Combine("Temp/UnityPkgExtract", hash);
                    Directory.CreateDirectory(tempRoot);
                    File.WriteAllBytes(Path.Combine(tempRoot, type), data);

                    if (type == "pathname")
                    {
                        string rawStr = Encoding.UTF8.GetString(data);
                        
                        // Unity pathname can end with "\n00" (literal chars) — take first line only
                        string targetPath = rawStr.Split('\n')[0].Trim();
                        string assetFile = Path.Combine(tempRoot, "asset");
                        string metaFile = Path.Combine(tempRoot, "asset.meta");

                        if (!File.Exists(assetFile)) continue;
                        
                        // Check GUID: skip only if GUID exists AND file actually exists
                        if (File.Exists(metaFile))
                        {
                            string guid = ExtractGuidFromMeta(metaFile);
                            if (!string.IsNullOrEmpty(guid))
                            {
                                string existingPath = AssetDatabase.GUIDToAssetPath(guid);
                                
                                if (!string.IsNullOrEmpty(existingPath) && File.Exists(existingPath))
                                    continue;
                            }
                        }

                        result[targetPath] = tempRoot;
                    }
                }
            }

            return result;
        }

        public static void ImportEntries(Dictionary<string, string> entries)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var (targetPath, tempRoot) in entries)
                {
                    string assetFile = Path.Combine(tempRoot, "asset");
                    string metaFile = Path.Combine(tempRoot, "asset.meta");

                    if (!File.Exists(assetFile)) continue;

                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
                    File.Copy(assetFile, targetPath, false);

                    if (File.Exists(metaFile))
                        File.Copy(metaFile, targetPath + ".meta", false);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh();
            }

            EditorUtility.DisplayDialog("Import Succeeded", $"{entries.Count} new item(s) imported.", "Ok");
        }

        struct TarHeader
        {
            public string name;
            public long size;
        }

        static TarHeader? ReadTarHeader(Stream stream)
        {
            byte[] header = new byte[512];
            int read = stream.Read(header, 0, 512);
            if (read < 512)
                return null;

            string name = GetString(header, 0, 100);
            if (string.IsNullOrEmpty(name))
                return null;

            string sizeOctal = GetString(header, 124, 12).Trim();
            long size = Convert.ToInt64(sizeOctal, 8);

            return new TarHeader { name = name, size = size };
        }

        static void SkipPadding(Stream stream, long size)
        {
            long padding = 512 - (size % 512);
            if (padding == 512) return;
            stream.Position += padding;
        }

        static string GetString(byte[] bytes, int offset, int length)
        {
            return Encoding.ASCII.GetString(bytes, offset, length).Trim('\0');
        }

        static string ExtractGuidFromMeta(string metaPath)
        {
            foreach (var line in File.ReadLines(metaPath))
            {
                if (line.StartsWith("guid:"))
                    return line.Substring(5).Trim();
            }
            return null;
        }

        static byte[] DecompressGZip(string path)
        {
            using (FileStream fs = File.OpenRead(path))
            using (GZipStream gzip = new GZipStream(fs, CompressionMode.Decompress))
            using (MemoryStream ms = new MemoryStream())
            {
                gzip.CopyTo(ms);
                return ms.ToArray();
            }
        }
	}

    public class ImportPreviewWindow : EditorWindow
    {
        private Dictionary<string, string> _newEntries;
        private Dictionary<string, bool> _selectedEntries;
        private Vector2 _scrollPosition;
        private bool _selectAll = true;

        public static void Open(string packagePath, Dictionary<string, string> newEntries)
        {
            var window = GetWindow<ImportPreviewWindow>(true, "Import Preview - New Items");
            window.minSize = new Vector2(550, 400);
            window._newEntries = newEntries;
            window._selectedEntries = new Dictionary<string, bool>();
            foreach (var key in newEntries.Keys)
                window._selectedEntries[key] = true;
            window._selectAll = true;
            window.Show();
        }

        private void OnGUI()
        {
            if (_newEntries == null || _newEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("No new items to display.", MessageType.Info);
                return;
            }

            GUILayout.Space(5);
            GUILayout.Label($"<b>{_newEntries.Count} new item(s) found</b>", RichCenterStyle());
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                EditorGUI.BeginChangeCheck();
                _selectAll = EditorGUILayout.ToggleLeft("Select All", _selectAll, EditorStyles.boldLabel, GUILayout.Width(100));
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var key in _newEntries.Keys)
                        _selectedEntries[key] = _selectAll;
                }

                GUILayout.FlexibleSpace();
                int selectedCount = _selectedEntries.Count(kv => kv.Value);
                GUILayout.Label($"{selectedCount} / {_newEntries.Count} selected");
            }
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);
            DrawSeparator();

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

            // Group by top-level folder
            var grouped = _newEntries.Keys
                .OrderBy(p => p)
                .GroupBy(p => GetTopFolder(p));

            foreach (var group in grouped)
            {
                GUILayout.Space(3);
                GUILayout.Label($"<b>{group.Key}</b>", RichLabelStyle());

                EditorGUI.indentLevel++;
                foreach (var path in group)
                {
                    _selectedEntries[path] = EditorGUILayout.ToggleLeft(path, _selectedEntries[path]);
                }
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndScrollView();

            GUILayout.Space(5);
            DrawSeparator();
            GUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                int count = _selectedEntries.Count(kv => kv.Value);
                GUI.enabled = count > 0;
                if (GUILayout.Button($"Import Selected ({count})", GUILayout.Width(180), GUILayout.Height(30)))
                {
                    var toImport = _newEntries
                        .Where(kv => _selectedEntries.TryGetValue(kv.Key, out var sel) && sel)
                        .ToDictionary(kv => kv.Key, kv => kv.Value);

                    UnityPackageCustomImporter.ImportEntries(toImport);
                    Close();
                }
                GUI.enabled = true;

                GUILayout.Space(10);

                if (GUILayout.Button("Cancel", GUILayout.Width(80), GUILayout.Height(30)))
                    Close();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5);
        }

        private static string GetTopFolder(string path)
        {
            var parts = path.Replace('\\', '/').Split('/');
            return parts.Length >= 2 ? $"{parts[0]}/{parts[1]}" : parts[0];
        }

        private static void DrawSeparator()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }

        private static GUIStyle RichCenterStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                richText = true,
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13
            };
        }

        private static GUIStyle RichLabelStyle()
        {
            return new GUIStyle(EditorStyles.label)
            {
                richText = true,
                padding = new RectOffset(5, 0, 0, 0)
            };
        }
    }
}