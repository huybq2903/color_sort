/*
 * Author: quanph
 * Email: quanph@falcongames.com
 * Company: Falcon Games
 */
// 2025-06-22


using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace Falcon.Modules.Core.ModulesVerify.Editor
{
    public class ModuleDependencyVisualizer : EditorWindow
    {
        private static readonly Color[] _colors = new Color[]
        {
            new Color(0.20f, 0.29f, 0.37f), // midnight blue
            new Color(0.26f, 0.45f, 0.77f), // steel blue
            new Color(0.18f, 0.67f, 0.72f), // teal
            new Color(0.30f, 0.60f, 0.36f), // green
            new Color(0.85f, 0.32f, 0.31f), // red
            new Color(0.91f, 0.49f, 0.13f), // orange
            new Color(0.94f, 0.76f, 0.05f), // gold
            new Color(0.69f, 0.55f, 0.26f), // khaki
            new Color(0.63f, 0.40f, 0.74f), // purple
            new Color(0.72f, 0.33f, 0.62f), // magenta
            new Color(0.94f, 0.60f, 0.60f), // light red
            new Color(0.84f, 0.74f, 0.75f), // light pink
            new Color(0.76f, 0.85f, 0.87f), // light cyan
            new Color(0.74f, 0.83f, 0.69f), // light green
            new Color(0.88f, 0.88f, 0.88f) // very light gray
        };

        private class Node
        {
            public string name;
            public Vector2 position;
            public float width = 100;
            public float height = 40;
            public int layer = 0; // Layer for sorting nodes visually
            public string author;
            public string asmdefPath;
            public int colorIndex = 0; // Màu sắc của node, dùng để vẽ
            public Rect Rect => new Rect(position, new Vector2(width, height));
        }

        public class ScannedModule
        {
            public string name;
            public string author;
            public List<string> references = new();
            public string path;
            public int colorIndex = 0; // Màu sắc của module, dùng để vẽ
        }

        private Dictionary<string, Node> nodes = new();
        private Dictionary<string, List<string>> dependencies = new();
        private Dictionary<string, ScannedModule> scannedModules = new();
        private Vector2 center => new Vector2(position.width / 2f, position.height / 2f);
        private string selectedModule = null;

        [MenuItem("Falcon/Modules Verify/Modules Dependency Visualizer")]
        public static void ShowWindow()
        {
            GetWindow<ModuleDependencyVisualizer>("Module Dependencies");
        }

        private Vector2 scrollPos = Vector2.zero;
        private Rect contentRect;

        private void CalculateContentRect()
        {
            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;

            foreach (var node in nodes.Values)
            {
                Rect rect = node.Rect;
                minX = Mathf.Min(minX, rect.xMin);
                minY = Mathf.Min(minY, rect.yMin);
                maxX = Mathf.Max(maxX, rect.xMax);
                maxY = Mathf.Max(maxY, rect.yMax);
            }

            float padding = 100f;
            float width = Mathf.Max(position.width, maxX - minX + padding * 2);
            float height = Mathf.Max(position.height, maxY - minY + padding * 2);

            contentRect = new Rect(minX - padding, minY - padding, width, height);
        }

        private void OnEnable()
        {
            scannedModules = ScanFalconModules();
            var filtered = FilterActiveModules(scannedModules);
            dependencies = filtered.ToDictionary(k => k.Key, v => v.Value.references);

            nodes.Clear();
            int n = dependencies.Count;
            float radius = 300f;
            Vector2 offset = new Vector2(position.width / 2f, position.height / 2f);

            int i = 0;
            foreach (var kvp in dependencies)
            {
                string moduleName = kvp.Key;
                float angle = i * Mathf.PI * 2f / n;
                float x = Center.x + Mathf.Cos(angle) * radius;
                float y = Center.y + Mathf.Sin(angle) * radius;

                string mName = moduleName.Replace("Falcon.Modules.", "").Replace(".Runtime", "");

                nodes[moduleName] = new Node
                {
                    name = mName.Split('.').Length >= 3 ? string.Join(".", mName.Split('.')[^2..]) : mName,
                    asmdefPath = scannedModules[moduleName].path,
                    author = scannedModules[moduleName].author,
                    colorIndex = scannedModules[moduleName].colorIndex
                };

                i++;
            }

            int layer = 0;
            Dictionary<string, List<string>> tmpDependencies = dependencies
                .ToDictionary(
                    entry => entry.Key,
                    entry => new List<string>(entry.Value)
                );

            while (tmpDependencies.Count > 0)
            {
                layer += 1;
                List<string> toRemove = new();

                foreach (var kvp in tmpDependencies)
                {
                    string moduleName = kvp.Key;
                    if (kvp.Value.Count == 0)
                    {
                        nodes[moduleName].layer = layer;
                        toRemove.Add(moduleName);
                    }
                }

                foreach (var moduleName in toRemove)
                {
                    foreach (var kvp in tmpDependencies)
                    {
                        kvp.Value.Remove(moduleName);
                    }
                }

                if (toRemove.Count == 0)
                {
                    foreach (var kvp in tmpDependencies)
                    {
                        string moduleName = kvp.Key;
                        nodes[moduleName].layer = layer;
                    }

                    break;
                }

                foreach (string name in toRemove)
                {
                    tmpDependencies.Remove(name);
                }
            }

            //Sắp xếp các nodes theo layer 
            nodes = nodes
                .OrderBy(kvp => kvp.Value.layer)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private Vector2 Center => new Vector2(position.width / 2f, position.height / 2f);

        private void OnGUI()
        {
            CalculateContentRect();

            scrollPos = GUI.BeginScrollView(
                new Rect(0, 0, position.width, position.height),
                scrollPos,
                contentRect
            );
            DrawNodes();
            DrawConnections();
            HandleEvents(Event.current);

            GUI.EndScrollView();
            Repaint();
        }

        private int firstDraw = 0;

        private void DrawNodes()
        {
            GUIStyle style = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 5, 5),
                normal = { background = null } // không để Unity vẽ nền đè lên
            };

            GUIStyle styleAuthor = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 11,
                fontStyle = FontStyle.Italic,
                padding = new RectOffset(1, 1, 1, 1),
                normal = { background = null } // không để Unity vẽ nền đè lên
            };

            int currentLayer = 0;
            float currentX = 0;
            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                var name = kvp.Key;
                int nodeY = node.layer * 90;
                if (currentLayer < node.layer)
                {
                    currentLayer = node.layer;
                    currentX = 0;
                }

                if (firstDraw == 0)
                {
                    node.position = new Vector2(currentX, nodeY);
                }

                Vector2 textSize = style.CalcSize(new GUIContent(node.name));
                Vector2 boxSize = new Vector2(textSize.x + 20, 40);
                Rect rect = new Rect(node.position, boxSize);
                node.width = boxSize.x;
                node.height = boxSize.y;
                Texture2D pinkTexture = EditorGUIUtility.whiteTexture;

                Color prevColor = GUI.color;
                // Color baseColor = _colors[node.layer % _colors.Length];
                Color baseColor = _colors[node.colorIndex];
                
                bool isParent = selectedModule != null && dependencies[selectedModule].Contains(name);
                bool isChild = selectedModule != null && dependencies[name].Contains(selectedModule);
                GUI.color = isParent ? Color.red : baseColor;
                GUI.color = isChild ? Color.green : GUI.color;
                GUI.color = (selectedModule == name) ? Color.yellow : GUI.color;

                GUI.DrawTexture(rect, pinkTexture);
                GUI.color = prevColor;
                GUI.Box(rect, new GUIContent(node.name, node.author), style);

                Vector2 textSizeAuthor = styleAuthor.CalcSize(new GUIContent(node.author));
                Vector2 boxSizeAuthor = new Vector2(textSizeAuthor.x + 5, 25);
                Rect rectAuthor = new Rect(new Vector2(rect.position.x, rect.position.y - 20), boxSizeAuthor);
                GUI.Box(rectAuthor, new GUIContent(node.author, node.author), styleAuthor);

                currentX += boxSize.x + 20;
            }

            firstDraw++;
        }

        private void DrawConnections()
        {
            Handles.BeginGUI();

            foreach (var kvp in dependencies)
            {
                if (!nodes.ContainsKey(kvp.Key)) continue;
                var fromNode = nodes[kvp.Key];
                Rect fromRect = fromNode.Rect;

                foreach (string dep in kvp.Value)
                {
                    if (!nodes.ContainsKey(dep)) continue;
                    var toNode = nodes[dep];
                    Rect toRect = toNode.Rect;

                    Vector2 start = GetEdgeIntersection(fromRect, toRect.center);
                    Vector2 end = GetEdgeIntersection(toRect, fromRect.center);

                    Color lineColor = _colors[toNode.layer % _colors.Length];
                    Handles.color = lineColor;

                    float lineWidth = 1f;
                    Handles.DrawAAPolyLine(lineWidth, start, end); // ✅ đúng cú pháp

                    Vector2 dir = (end - start).normalized;
                    Vector2 perp = new Vector2(-dir.y, dir.x);
                    Vector2 arrowTail = end - dir * 12f;

                    Handles.DrawAAConvexPolygon(end, arrowTail + perp * 4, arrowTail - perp * 4);
                }
            }

            Handles.EndGUI();
        }

        private void HandleEvents(Event e)
        {
            if (e.type == EventType.MouseDown)
            {
                selectedModule = null;
            }

            foreach (var kvp in nodes)
            {
                var node = kvp.Value;
                var name = kvp.Key;
                Rect rect = node.Rect;

                switch (e.type)
                {
                    case EventType.MouseDown:
                        if (rect.Contains(e.mousePosition))
                        {
                            if (!string.IsNullOrEmpty(node.asmdefPath))
                            {
                                selectedModule = name;
                                var asset = AssetDatabase.LoadAssetAtPath<Object>(node.asmdefPath);
                                if (asset) EditorGUIUtility.PingObject(asset);
                            }

                            e.Use();
                        }

                        break;

                    case EventType.MouseDrag:
                        if (rect.Contains(e.mousePosition))
                        {
                            node.position += e.delta;
                            e.Use();
                        }

                        break;
                }
            }
        }

        private Vector2 GetEdgeIntersection(Rect rect, Vector2 target)
        {
            Vector2 center = rect.center;
            Vector2 dir = (target - center).normalized;

            float dx = rect.width / 2f;
            float dy = rect.height / 2f;

            float scaleX = Mathf.Abs(dx / dir.x);
            float scaleY = Mathf.Abs(dy / dir.y);
            float scale = Mathf.Min(scaleX, scaleY);

            return center + dir * scale;
        }


        private static Dictionary<string, int> authorColors = new Dictionary<string, int>();
        private static int currentColorIndex = 0;
        private static Dictionary<string, ScannedModule> ScanFalconModules()
        {
            currentColorIndex = 1;
            authorColors.Clear();
            string root = "Assets/Falcon/";
            if (!Directory.Exists(root))
            {
                Debug.LogError("❌ Folder 'Assets/Falcon/' not found.");
                return new Dictionary<string, ScannedModule>();
            }

            string[] asmdefGUIDs = AssetDatabase.FindAssets("t:asmdef", new[] { root });
            Dictionary<string, ScannedModule> modules = new();

            foreach (var guid in asmdefGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string normalizedPath = path.Replace('\\', '/').ToLower();

                // ✅ Bỏ qua nếu nằm trong thư mục Lib hoặc Editor
                if (normalizedPath.Contains("/lib/")
                    || normalizedPath.Contains("/editor")
                    || normalizedPath.Contains(".editor")
                    || normalizedPath.Contains("/test")
                    || normalizedPath.Contains("/demo")
                   )
                    continue;

                string json = File.ReadAllText(path);
                AsmdefData data = JsonUtility.FromJson<AsmdefData>(json);

                if (string.IsNullOrEmpty(data.name))
                    continue;

                ScannedModule mod = new()
                {
                    name = data.name,
                    path = path,
                    references = new List<string>()
                };

                string modulePath = Path.GetDirectoryName(path);
                while(File.Exists(modulePath + "/package.json") == false && 
                      !string.IsNullOrEmpty(modulePath) && 
                      modulePath != "Assets")
                {
                    modulePath = Path.GetDirectoryName(modulePath);
                }
        
                string packagePath = modulePath + "/package.json";
                if (File.Exists(packagePath))
                {
                    string jsonConfig = File.ReadAllText(packagePath);
                    PackageJson package = JsonConvert.DeserializeObject<PackageJson>(jsonConfig);
                    if (package != null && package.author != null)
                    {
                        mod.author = package.author.name;
                        if (!authorColors.ContainsKey(mod.author))
                        {
                            authorColors[mod.author] = currentColorIndex % _colors.Length;
                            mod.colorIndex = authorColors[mod.author];
                            currentColorIndex++;
                        }
                        else
                        {
                            mod.colorIndex = authorColors[mod.author];
                        }
                    }else
                    {
                        mod.author = "Unknown";
                        mod.colorIndex = 0;
                    }
                        
                }
                else
                {
                    mod.author = "Unknown";
                    mod.colorIndex = 0;
                }
                
                if (data.references != null)
                {
                    foreach (var r in data.references)
                    {
                        string refName = ResolveReferenceToName(r);
                        if (!string.IsNullOrEmpty(refName) && refName.Contains("Falcon"))
                        {
                            mod.references.Add(refName);
                        }
                    }
                }

                modules[mod.name] = mod;
            }

            return modules;
        }

        private static string ResolveReferenceToName(string reference)
        {
            if (reference.StartsWith("GUID:"))
            {
                string guid = reference.Substring(5);
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var asm = JsonUtility.FromJson<AsmdefData>(json);
                    return asm.name;
                }
            }

            return reference;
        }

        private static void CollectTransitive(ScannedModule mod, Dictionary<string, ScannedModule> all,
            HashSet<string> visited)
        {
            foreach (var dep in mod.references)
            {
                if (visited.Add(dep) && all.TryGetValue(dep, out var next))
                {
                    CollectTransitive(next, all, visited);
                }
            }
        }

        private Dictionary<string, ScannedModule> FilterActiveModules(Dictionary<string, ScannedModule> raw)
        {
            // Giữ lại những module có liên kết hoặc được liên kết tới
            HashSet<string> referenced = new();
            foreach (var module in raw.Values)
            foreach (var dep in module.references)
                referenced.Add(dep);

            return raw
                .Where(kvp => kvp.Value.references.Count > 0 || referenced.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        [System.Serializable]
        private class AsmdefData
        {
            public string name;
            public string[] references;
        }

        [System.Serializable]
        class PackageJson
        {
            public string name;
            public string displayName;
            public string version;
            public string unity;
            public string description;
            public AuthorInfo author;
            public Dictionary<string, string> dependencies;
        }

        [System.Serializable]
        class AuthorInfo
        {
            public string name;
            public string email;
        }
    }
}