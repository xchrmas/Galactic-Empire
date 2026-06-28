

using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GalacticEmpire.Editor
{
    /// <summary>Draws an interactive dependency graph of all Assembly Definitions.</summary>
    public class AssemblyVisualizer : EditorWindow
    {
        private struct AssemblyNode
        {
            public string Name;
            public string ShortName;
            public Rect Rect;
            public Color Color;
            public List<string> Dependencies;
        }

        private readonly List<AssemblyNode> _nodes = new();
        private Vector2 _scroll;
        private bool _initialized;

        private static readonly Dictionary<string, Color> LayerColors = new()
        {
            { "Core",           new Color(0.50f, 0.46f, 0.87f) },
            { "Application",    new Color(0.11f, 0.62f, 0.46f) },
            { "Infrastructure", new Color(0.85f, 0.35f, 0.19f) },
            { "Presentation",   new Color(0.22f, 0.54f, 0.87f) },
            { "Editor",         new Color(0.53f, 0.53f, 0.50f) },
            { "Tests",          new Color(0.39f, 0.60f, 0.13f) },
        };

        [MenuItem("GalacticEmpire/📊 Assembly Dependency Graph")]
        public static void ShowWindow()
        {
            var win = GetWindow<AssemblyVisualizer>("Assembly Dependencies");
            win.minSize = new Vector2(700, 500);
            win.Show();
        }

        private void OnEnable()
        {
            _initialized = false;
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!_initialized)
                BuildGraph();

            DrawGraph();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField("Galactic Empire — Assembly Dependency Graph", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Refresh", EditorStyles.toolbarButton))
                _initialized = false;

            if (GUILayout.Button("📤 Export DOT", EditorStyles.toolbarButton))
                ExportDotFile();

            EditorGUILayout.EndHorizontal();

            DrawLegend();
        }

        private void DrawLegend()
        {
            EditorGUILayout.BeginHorizontal();

            foreach (var kv in LayerColors)
            {
                var prev = GUI.color;
                GUI.color = kv.Value;
                GUILayout.Label("■", GUILayout.Width(16));
                GUI.color = prev;
                GUILayout.Label(kv.Key, EditorStyles.miniLabel, GUILayout.Width(80));
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private void BuildGraph()
        {
            _nodes.Clear();

            var asmdefGuids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", new[] { "Assets/_Project" });
            var layoutPositions = GenerateLayout(asmdefGuids.Length);
            int i = 0;

            foreach (var guid in asmdefGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string json = File.ReadAllText(path);
                string name = ExtractField(json, "name");

                var node = new AssemblyNode
                {
                    Name = name,
                    ShortName = GetShortName(name),
                    Rect  = new Rect(layoutPositions[i].x, layoutPositions[i].y, 160, 44),
                    Color = GetLayerColor(name),
                    Dependencies = ExtractDependencies(json)
                };

                _nodes.Add(node);
                i++;
            }

            _initialized = true;
        }

        private void DrawGraph()
        {
            _scroll = GUI.BeginScrollView(
                new Rect(0, 72, position.width, position.height - 72),
                _scroll,
                new Rect(0, 0, 900, 600));

            DrawConnections();
            DrawNodes();

            GUI.EndScrollView();
        }

        private void DrawConnections()
        {
            foreach (var node in _nodes)
            {
                foreach (var dep in node.Dependencies)
                {
                    var target = _nodes.FirstOrDefault(n => n.Name == dep);
                    if (target.Name == null)
                        continue;

                    Vector2 from = new Vector2(node.Rect.center.x, node.Rect.center.y);
                    Vector2 to   = new Vector2(target.Rect.center.x, target.Rect.center.y);

                    // Check for architecture violation — dependency pointing wrong direction
                    bool isViolation = IsArchitectureViolation(node.Name, target.Name);
                    Color lineColor  = isViolation ? Color.red : new Color(0.6f, 0.6f, 0.6f, 0.8f);

                    Handles.color = lineColor;
                    Handles.DrawAAPolyLine(2f, from, to);
                    DrawArrow(from, to, lineColor);
                }
            }
        }

        private void DrawNodes()
        {
            foreach (var node in _nodes)
            {
                var prev = GUI.color;
                GUI.color = new Color(node.Color.r, node.Color.g, node.Color.b, 0.15f);
                GUI.DrawTexture(node.Rect, Texture2D.whiteTexture);
                GUI.color = prev;

                // Border
                GUI.color = node.Color;
                GUI.DrawTexture(new Rect(node.Rect.x, node.Rect.y, node.Rect.width, 2), Texture2D.whiteTexture);
                GUI.color = prev;

                // Label
                var style = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize  = 11,
                    wordWrap  = true,
                    normal    = { textColor = node.Color }
                };

                GUI.Label(node.Rect, node.ShortName, style);
            }
        }

        private static void DrawArrow(Vector2 from, Vector2 to, Color color)
        {
            Vector2 dir = (to - from).normalized;
            Vector2 right = new Vector2(-dir.y, dir.x);
            Vector2 tip   = to - dir * 22f;
            Vector2 left  = tip - dir * 8f + right * 5f;
            Vector2 rightP = tip - dir * 8f - right * 5f;

            Handles.color = color;
            Handles.DrawAAPolyLine (2f, left, tip, rightP);
        }

        private static bool IsArchitectureViolation(string from, string to)
        {
            // Core must not depend on anything
            if (from.Contains("Core") && !from.Contains("Tests"))
            {
                if (to.Contains("Application") || to.Contains("Infrastructure") || to.Contains("Presentation"))
                    return true;
            }

            // Application must not depend on Infrastructure or Presentation
            if (from.Contains("Application") && !from.Contains("Tests"))
            {
                if (to.Contains("Infrastructure") || to.Contains("Presentation"))
                    return true;
            }

            return false;
        }

        private static Color GetLayerColor(string name)
        {
            foreach (var kv in LayerColors)
            {
                if (name.Contains(kv.Key))
                    return kv.Value;
            }
            return Color.white;
        }

        private static string GetShortName(string fullName)
        {
            return fullName.Replace("GalacticEmpire.", "");
        }

        private static List<Vector2> GenerateLayout(int count)
        {
            var positions = new List<Vector2>();
            int cols  = 3;
            float xStep  = 200f;
            float yStep  = 120f;

            for (int i = 0; i < count; i++)
            {
                float x = 40f + (i % cols) * xStep;
                float y = 40f + (i / cols) * yStep;
                positions.Add(new Vector2(x, y));
            }

            return positions;
        }

        private static string ExtractField(string json, string field)
        {
            string key = $"\"{field}\"";
            int index  = json.IndexOf(key);
            if (index < 0) return "";

            int start = json.IndexOf('"', index + key.Length + 1);
            int end   = json.IndexOf('"', start + 1);
            return start < 0 || end < 0 ? "" : json.Substring(start + 1, end - start - 1);
        }

        private static List<string> ExtractDependencies(string json)
        {
            var deps  = new List<string>();
            string key = "\"references\"";
            int index  = json.IndexOf(key);
            if (index < 0) return deps;

            int start = json.IndexOf('[', index);
            int end   = json.IndexOf(']', start);
            if (start < 0 || end < 0) return deps;

            string block = json.Substring(start + 1, end - start - 1);
            var parts    = block.Split(',');

            foreach (var part in parts)
            {
                string cleaned = part.Trim().Trim('"');
                if (!string.IsNullOrWhiteSpace(cleaned))
                    deps.Add(cleaned);
            }

            return deps;
        }

        private void ExportDotFile()
        {
            string dot = "digraph GalacticEmpire {\n  rankdir=BT;\n";

            foreach (var node in _nodes)
            {
                dot += $"  \"{node.ShortName}\" [style=filled, fillcolor=\"#{ColorToHex(node.Color)}\"];\n";

                foreach (var dep in node.Dependencies)
                {
                    var target = _nodes.FirstOrDefault(n => n.Name == dep);
                    if (target.Name != null)
                        dot += $"  \"{node.ShortName}\" -> \"{target.ShortName}\";\n";
                }
            }

            dot += "}";

            string path = Path.Combine(Application.dataPath, "../AssemblyDependencies.dot");
            File.WriteAllText(path, dot);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"✅ DOT file exported to {path}");
        }

        private static string ColorToHex(Color c)
        {
            return ColorUtility.ToHtmlStringRGB(c);
        }
    }
}
