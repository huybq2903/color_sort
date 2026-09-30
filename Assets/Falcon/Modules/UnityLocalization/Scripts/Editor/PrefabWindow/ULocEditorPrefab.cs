/*
 * Author: Bui Quang Huy
 * Email: huybq@falcongames.com
 * Company: Falcon Games
 * Date: 2025-09-19
 */

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using TMPro;
using UnityEngine.Localization.Components;
using Falcon.Modules.UnityLocalization.Runtime;
using UnityEditor.Localization;

namespace Falcon.Modules.UnityLocalization.Editor
{
    [Serializable]
    public class Row
    {
        public TMP_Text text;
        public string path; // name only
        public bool hasStringComp;
        public bool hasFontComp;

        // Cached components (non-serialized to avoid Unity serialization issues)
        [NonSerialized] public LocalizeStringEvent cachedStringComp;
        [NonSerialized] public LocalizeTMP_FontComponent cachedFontComp;
        
        // Cached SerializedObjects (non-serialized)
        [NonSerialized] public SerializedObject cachedStringSO;
        [NonSerialized] public SerializedObject cachedFontSO;
    }
    
    public class ULocEditorPrefab : EditorWindow
    {
        // private Vector2 _scroll;
        private List<Row> _rows = new();
        private GameObject _currentRoot;
        private Vector2 _scrollPos;
        
        private ULocHeaderComponent _header;
        private ULocToolbarComponent _toolbar;
        private ULocEntryComponent _entryComponent;
        
        private AssetTableCollection[] _assetTables;
        
        private int currentTab = 0;
        private readonly string[] tabs = { "Table", "Prefab" };
        
        // Caching for filtered results
        private List<Row> _cachedFilteredRows;
        private string _lastSearchText = "";
        
        // Caching for cell component instances
        private readonly Dictionary<Row, ULocStringCellComponent> _stringCellCache = new();
        private readonly Dictionary<Row, ULocFontCellComponent> _fontCellCache = new();
        
        // Debouncing for selection changes
        private GameObject _pendingSelection;
        private bool _selectionChangePending;
        
        public static LocalizedTMP_Font defaultFont = new();
        
        [MenuItem("Falcon/Modules/Unity Localization/Localize Editor Prefab")]
        private static void Open()
        {
            var win = GetWindow<ULocEditorPrefab>("Localize Editor Prefab");
            win.minSize = new Vector2(700, 420);
            win.RefreshFromSelection();
        }

        private void OnEnable()
        {
            Selection.selectionChanged += OnSelectionChanged;
            RefreshFromSelection();
            
            _header = new ULocHeaderComponent(_currentRoot, RebuildRows);
            _toolbar = new ULocToolbarComponent();
            _entryComponent = new ULocEntryComponent();
        }

        private void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChanged;
            // Cleanup pending selection change
            EditorApplication.delayCall -= ProcessSelectionChange;
            _selectionChangePending = false;
            _pendingSelection = null;
            
            // Cleanup SerializedObjects in rows
            foreach (var row in _rows)
            {
                if (row.cachedStringSO != null)
                {
                    row.cachedStringSO.Dispose();
                    row.cachedStringSO = null;
                }
                if (row.cachedFontSO != null)
                {
                    row.cachedFontSO.Dispose();
                    row.cachedFontSO = null;
                }
            }
        }

        private void OnSelectionChanged()
        {
            var selected = Selection.activeGameObject;
            
            // Debounce: only process if selection actually changed
            if (_pendingSelection == selected && _selectionChangePending)
                return;
            
            _pendingSelection = selected;
            
            // Only add delayCall if not already pending
            if (!_selectionChangePending)
            {
                _selectionChangePending = true;
                // Use delayCall to debounce rapid selection changes
                EditorApplication.delayCall += ProcessSelectionChange;
            }
            
            Repaint();
        }
        
        private void ProcessSelectionChange()
        {
            _selectionChangePending = false;
            // Remove this call from delayCall to prevent multiple calls
            EditorApplication.delayCall -= ProcessSelectionChange;
            RefreshFromSelection();
        }

        private void RefreshFromSelection()
        {
            var go = Selection.activeGameObject;
            if (go && EditorUtility.IsPersistent(go))
            {
                _currentRoot = null;
                _rows.Clear();
                return;
            }

            _currentRoot = go;
            RebuildRows();
        }

        private void RebuildRows()
        {
            _rows.Clear();
            _cachedFilteredRows = null; // Invalidate filter cache
            _lastSearchText = ""; // Reset search cache
            _stringCellCache.Clear(); // Clear cell component cache
            _fontCellCache.Clear();
            
            if (!_currentRoot) return;

            var tmps = _currentRoot.GetComponentsInChildren<TMP_Text>(true);
            foreach (var t in tmps)
            {
                if (!t) continue;

                if (t.gameObject.GameObjectHasMissingScripts())
                    continue;

                // Cache components once
                var stringComp = t.GetComponent<LocalizeStringEvent>();
                var fontComp = t.GetComponent<LocalizeTMP_FontComponent>();
                
                // Calculate full path once
                var fullPath = GetFullPath(t.transform, _currentRoot.transform);

                var row = new Row
                {
                    text = t,
                    path = t.name,
                    hasStringComp = stringComp != null,
                    hasFontComp = fontComp != null,
                    cachedStringComp = stringComp,
                    cachedFontComp = fontComp
                };
                _rows.Add(row);
            }

            _rows = _rows
                .OrderByDescending(r => !r.hasStringComp || !r.hasFontComp)
                .ThenBy(r => r.path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string GetFullPath(Transform target, Transform root)
        {
            if (target == null || root == null) return target != null ? target.name : "";
            
            var path = new System.Text.StringBuilder();
            var current = target;
            var pathParts = new List<string>();
            
            while (current != null && current != root.parent)
            {
                pathParts.Add(current.name);
                current = current.parent;
            }
            
            // Reverse to get root-to-target path
            for (int i = pathParts.Count - 1; i >= 0; i--)
            {
                if (path.Length > 0) path.Append("/");
                path.Append(pathParts[i]);
            }
            
            return path.Length > 0 ? path.ToString() : target.name;
        }

        private void OnGUI()
        {
            currentTab = GUILayout.Toolbar(currentTab, tabs);
            GUILayout.Space(10);

            switch (currentTab)
            {
                case 0:
                    OnTabTable();
                    break;
                case 1:
                    OnTabPrefab();
                    break;
            }
        }

        private void OnTabTable()
        {
            _entryComponent.OnGUI();
        }

        private void OnTabPrefab()
        {
            _header.SetRoot(_currentRoot);
            _header.OnGUI();
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (!_currentRoot)
            {
                EditorGUILayout.HelpBox("Hãy chọn object để quét TMP_Text.", MessageType.Info);
                // return;
            }
            _toolbar.OnGUI();
            
            EditorGUILayout.Space(4);

            // Get filtered rows with caching
            var filtered = GetFilteredRows();
            foreach (var row in filtered)
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    // Path clickable (ping-only)
                    var pathContent = new GUIContent(row.path, "Click để ping object này");
                    var pathRect = GUILayoutUtility.GetRect(pathContent, EditorStyles.label, GUILayout.Height(18));
                    EditorGUIUtility.AddCursorRect(pathRect, MouseCursor.Link);
                    if (GUI.Button(pathRect, pathContent, EditorStyles.label))
                    {
                        if (row.text != null)
                            EditorGUIUtility.PingObject(row.text.gameObject); // ping only
                    }

                    // Cells - reuse cached instances
                    if (!_stringCellCache.TryGetValue(row, out var stringCell))
                    {
                        stringCell = new ULocStringCellComponent(row);
                        _stringCellCache[row] = stringCell;
                    }
                    stringCell.OnGUI();
                    
                    if (!_fontCellCache.TryGetValue(row, out var fontCell))
                    {
                        fontCell = new ULocFontCellComponent(row);
                        _fontCellCache[row] = fontCell;
                    }
                    fontCell.OnGUI();
                }
            }
            EditorGUILayout.EndScrollView();
        }
        
        private List<Row> GetFilteredRows()
        {
            var currentSearch = _toolbar.Search?.Trim() ?? "";
            
            // Check if we need to recalculate
            if (_cachedFilteredRows == null || _lastSearchText != currentSearch)
            {
                _lastSearchText = currentSearch;
                
                if (string.IsNullOrEmpty(currentSearch))
                {
                    _cachedFilteredRows = _rows;
                }
                else
                {
                    _cachedFilteredRows = _rows.Where(r => 
                        r.path.IndexOf(currentSearch, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (r.text && r.text.name.IndexOf(currentSearch, StringComparison.OrdinalIgnoreCase) >= 0)
                    ).ToList();
                }
            }
            
            return _cachedFilteredRows;
        }
    }
}