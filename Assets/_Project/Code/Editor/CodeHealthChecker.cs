// Layer: Editor | Static code health analyzer for Galactic Empire.
// Scans all C# files and reports architecture violations, performance issues, and ECS anti-patterns.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GalacticEmpire.Editor
{
    public enum IssueSeverity { Info, Warning, Critical }

    public class CodeIssue
    {
        public string FilePath;
        public string RuleId;
        public string Category;
        public IssueSeverity Severity;
        public string Message;
        public string[] Suggestions;
        public string[] Examples;
    }

    public interface ICodeRule
    {
        string Id { get; }
        string Category { get; }
        IssueSeverity DefaultSeverity { get; }
        bool Analyze(string path, string content, out CodeIssue issue);
    }

    public static class CodeHealthChecker
    {
        public static readonly List<CodeIssue> FoundIssues = new();

        private static readonly Dictionary<string, (string hash, CodeIssue[] issues)> _cache = new();
        private static readonly HashSet<string> _ignoredIssues = new();

        private static readonly Regex StripRegex = new Regex(
            @"@""(?:[^""]|"""")*""|""(?:[^""\\]|\\.)*""|//.*$|/\*[\s\S]*?\*/",
            RegexOptions.Multiline | RegexOptions.Compiled
        );

        private static readonly ICodeRule[] Rules =
        {
            // Naming conventions
            new InterfaceNamingRule(),
            new PrivateFieldNamingRule(),

            // Clean Architecture violations
            new CoreUsesUnityRule(),
            new CoreUsesInfrastructureRule(),
            new PresentationUsesDirectInstantiationRule(),

            // Performance
            new HeavyUpdateRule(),
            new GCAllocationInUpdateRule(),

            // ECS / DOTS patterns
            new MonoBehaviourInsteadOfECSRule(),
            new MutableECSComponentRule(),

            // Security & best practices
            new DirectPlayerPrefsRule(),
            new FindObjectUsageRule(),
            new SingletonAntiPatternRule(),
        };

        [MenuItem("GalacticEmpire/🔍 Run Code Health Scan")]
        public static void RunScan()
        {
            FoundIssues.Clear();
            LoadIgnoredIssues();
            Debug.Log("=== 🚀 Galactic Empire Code Health Scan Started ===");

            var guids = AssetDatabase.FindAssets("t:Script", new[] { "Assets/_Project" });
            if (guids.Length == 0)
            {
                Debug.LogWarning("No scripts found in Assets/_Project.");
                return;
            }

            int total = guids.Length;
            int processed = 0;

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!path.EndsWith(".cs") ||
                    path.Contains("/Plugins/") ||
                    path.Contains("/Generated/"))
                    continue;

                EditorUtility.DisplayProgressBar(
                    "🔍 Scanning...", Path.GetFileName(path), processed++ / (float)total);

                string rawContent = File.ReadAllText(path);
                string hash = $"{rawContent.Length}{File.GetLastWriteTimeUtc(path):O}";

                if (_cache.TryGetValue(path, out var cached) && cached.hash == hash)
                {
                    FoundIssues.AddRange(cached.issues
                        .Where(i => !_ignoredIssues.Contains($"{i.FilePath}|{i.RuleId}")));
                    continue;
                }

                string cleanContent = StripCommentsAndStrings(rawContent);
                var issues = new List<CodeIssue>();

                foreach (var rule in Rules)
                {
                    if (rule.Analyze(path, cleanContent, out var issue) &&
                        !_ignoredIssues.Contains($"{path}|{rule.Id}"))
                    {
                        issues.Add(issue);
                    }
                }

                _cache[path] = (hash, issues.ToArray());
                FoundIssues.AddRange(issues);
            }

            EditorUtility.ClearProgressBar();
            PrintResults();
            Debug.Log($"=== ✅ Scan Complete — {FoundIssues.Count} issue(s) found ===");
        }

        [MenuItem("GalacticEmpire/📤 Export Health Report")]
        public static void ExportMarkdown()
        {
            string md = "# Galactic Empire — Code Health Report\n\n";
            md += $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}\n\n";
            md += "| File | Category | Severity | Message |\n|---|---|---|---|\n";

            foreach (var i in FoundIssues)
                md += $"| {Path.GetFileName(i.FilePath)} | {i.Category} | {i.Severity} | {i.Message} |\n";

            string path = Path.Combine(Application.dataPath, "../CodeHealthReport.md");
            File.WriteAllText(path, md);
            EditorUtility.RevealInFinder(path);
            Debug.Log($"✅ Report exported to {path}");
        }

        [MenuItem("GalacticEmpire/🛠 Review Issues")]
        public static void OpenReviewer() => CodeHealthReviewerWindow.ShowWindow();

        private static string StripCommentsAndStrings(string code) =>
            StripRegex.Replace(code, m =>
                m.Value.StartsWith("//") || m.Value.StartsWith("/*") ? "" : "\"\"");

        private static void LoadIgnoredIssues()
        {
            _ignoredIssues.Clear();
            var saved = EditorPrefs.GetString("GE_CodeHealth_Ignored", "");
            if (!string.IsNullOrEmpty(saved))
                _ignoredIssues.UnionWith(saved.Split(';'));
        }

        private static void PrintResults()
        {
            int fileCount = FoundIssues.Select(i => i.FilePath).Distinct().Count();
            Debug.Log($"📊 Files scanned: {fileCount} | Issues: {FoundIssues.Count}");

            if (FoundIssues.Count == 0)
            {
                Debug.Log("✅ No issues found. Architecture is clean.");
                return;
            }

            foreach (var issue in FoundIssues)
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.FilePath);
                string log = $"[{issue.Severity}] [{issue.Category}] {Path.GetFileName(issue.FilePath)}\n⚠️ {issue.Message}\n";

                for (int i = 0; i < issue.Suggestions.Length; i++)
                    log += $"💡 {issue.Suggestions[i]}\n   ↳ {issue.Examples[i]}\n";

                Debug.LogWarning(log.TrimEnd(), obj);
            }
        }
    }

    // ── Naming Convention Rules ───────────────────────────────────────────

    internal class InterfaceNamingRule : ICodeRule
    {
        public string Id => "NAME001";
        public string Category => "Naming";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            var match = Regex.Match(content, @"public\s+interface\s+([A-Z][a-zA-Z0-9]+)\b");

            if (match.Success && !match.Groups[1].Value.StartsWith("I"))
            {
                issue = Build(path, $"Interface '{match.Groups[1].Value}' must start with 'I'.",
                    new[] { "Rename to follow IPascalCase convention.", "Configure .editorconfig to enforce this." },
                    new[] { "public interface IFleetRepository { }", "dotnet_naming_rule.interfaces.required_prefix = I" });
                return true;
            }

            issue = null;
            return false;
        }

        private CodeIssue Build(string path, string msg, string[] s, string[] e) =>
            new CodeIssue { FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity, Message = msg, Suggestions = s, Examples = e };
    }

    internal class PrivateFieldNamingRule : ICodeRule
    {
        public string Id => "NAME002";
        public string Category => "Naming";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // Detects private fields that don't start with _
            var match = Regex.Match(content, @"private\s+(?!static\s+readonly)[\w<>\[\]]+\s+([a-z][a-zA-Z0-9]*)\s*[;=]");

            if (match.Success && !match.Groups[1].Value.StartsWith("_"))
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = $"Private field '{match.Groups[1].Value}' should start with '_'.",
                    Suggestions = new[] { "Rename to _camelCase.", "Configure .editorconfig to enforce naming." },
                    Examples = new[] { "private float _speed;", "dotnet_naming_style.underscore_camel_style.required_prefix = _" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    // ── Clean Architecture Rules ──────────────────────────────────────────

    internal class CoreUsesUnityRule : ICodeRule
    {
        public string Id => "ARCH001";
        public string Category => "Clean Architecture";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // Core layer must never reference UnityEngine
            bool isCoreLayer = path.Contains("/Code/Core/");
            bool usesUnity = Regex.IsMatch(content, @"using UnityEngine|MonoBehaviour|ScriptableObject");

            if (isCoreLayer && usesUnity)
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Core layer must not reference UnityEngine — violates Clean Architecture.",
                    Suggestions = new[] { "Remove Unity dependency from Core.", "Move Unity-specific code to Infrastructure.", "Use interfaces in Core — implement in Infrastructure." },
                    Examples = new[] { "// Core: pure C# only\npublic sealed record ShipEntity { }", "// Infrastructure: Unity code here\npublic class ShipView : MonoBehaviour { }", "// Core defines contract\npublic interface IFleetRepository { }" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    internal class CoreUsesInfrastructureRule : ICodeRule
    {
        public string Id => "ARCH002";
        public string Category => "Clean Architecture";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // Core must never reference Infrastructure or Presentation namespaces
            bool isCoreLayer = path.Contains("/Code/Core/");
            bool usesOuterLayer = Regex.IsMatch(content,
                @"using GalacticEmpire\.Infrastructure|using GalacticEmpire\.Presentation");

            if (isCoreLayer && usesOuterLayer)
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Core layer references Infrastructure or Presentation — dependency inversion violated.",
                    Suggestions = new[] { "Dependencies must point inward only.", "Use interfaces in Core, implement in outer layers.", "Inject dependencies via constructor (VContainer)." },
                    Examples = new[] { "// Wrong: Core → Infrastructure\nusing GalacticEmpire.Infrastructure;", "// Correct: Core defines interface\npublic interface IFleetRepository { }", "// VContainer injects the implementation\nbuilder.Register<FleetRepositorySO>().As<IFleetRepository>();" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    internal class PresentationUsesDirectInstantiationRule : ICodeRule
    {
        public string Id => "ARCH003";
        public string Category => "Clean Architecture";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // Presentation should never use 'new' to create services — use DI
            bool isPresentationLayer = path.Contains("/Code/Presentation/");
            bool usesNew = Regex.IsMatch(content, @"new\s+(Fleet|Ship|Station|Resource|Battle)\w+\s*\(");

            if (isPresentationLayer && usesNew)
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Presentation layer manually instantiates a service — use DI instead.",
                    Suggestions = new[] { "Inject dependency via constructor.", "Register in GameBootstrapper.", "Never 'new' a service in Presentation." },
                    Examples = new[] { "public GameEntryPoint(IFleetRepository repo) { _repo = repo; }", "builder.Register<FleetService>().As<IFleetService>();", "// VContainer resolves the entire graph automatically" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    // ── Performance Rules ─────────────────────────────────────────────────

    internal class HeavyUpdateRule : ICodeRule
    {
        public string Id => "PERF001";
        public string Category => "Performance";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            var methods = Regex.Matches(content,
                @"\b(Update|LateUpdate|FixedUpdate)\s*\([^)]*\)\s*\{", RegexOptions.Multiline);

            foreach (Match m in methods)
            {
                string body = content.Substring(m.Index, Math.Min(content.Length - m.Index, 1500));

                if (Regex.IsMatch(body, @"GetComponent|Find\(|new\s+GameObject|Resources\.Load"))
                {
                    issue = new CodeIssue
                    {
                        FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                        Message = $"Heavy operation detected in {m.Groups[1].Value}() — causes GC and performance drop.",
                        Suggestions = new[] { "Cache references in Awake() or Start().", "Use event-driven architecture with UniRx/R3.", "Move simulation logic to ECS ISystem." },
                        Examples = new[] { "private Rigidbody _rb;\nvoid Awake() => _rb = GetComponent<Rigidbody>();", "Observable.EveryUpdate().Subscribe(_ => OnTick());", "[BurstCompile]\npublic partial struct FleetMovementSystem : ISystem { }" }
                    };
                    return true;
                }
            }

            issue = null;
            return false;
        }
    }

    internal class GCAllocationInUpdateRule : ICodeRule
    {
        public string Id => "PERF002";
        public string Category => "Performance";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            var methods = Regex.Matches(content,
                @"\b(Update|LateUpdate|FixedUpdate|Tick)\s*\([^)]*\)\s*\{", RegexOptions.Multiline);

            foreach (Match m in methods)
            {
                string body = content.Substring(m.Index, Math.Min(content.Length - m.Index, 1500));

                if (Regex.IsMatch(body, @"new\s+(List|Dictionary|HashSet|object)|\.ToList\(|\.ToArray\(|string\s*\+"))
                {
                    issue = new CodeIssue
                    {
                        FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                        Message = $"GC allocation detected in {m.Groups[1].Value}() — causes frame spikes.",
                        Suggestions = new[] { "Reuse collections — clear instead of new.", "Use ArrayPool<T> for temporary arrays.", "Move to Burst-compiled Job for zero-alloc processing." },
                        Examples = new[] { "private readonly List<ShipEntity> _cache = new();\nvoid Tick() { _cache.Clear(); Fill(_cache); }", "var arr = ArrayPool<ShipComponent>.Shared.Rent(size);", "[BurstCompile]\nvoid Execute(NativeArray<ShipComponent> ships) { }" }
                    };
                    return true;
                }
            }

            issue = null;
            return false;
        }
    }

    // ── ECS / DOTS Rules ──────────────────────────────────────────────────

    internal class MonoBehaviourInsteadOfECSRule : ICodeRule
    {
        public string Id => "ECS001";
        public string Category => "ECS / DOTS";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // Detects MonoBehaviour used for simulation data (ships, projectiles, fleet units)
            bool isSimulation = Regex.IsMatch(Path.GetFileName(path),
                @"Ship|Fleet|Projectile|Unit|Enemy|Combat", RegexOptions.IgnoreCase);

            bool usesMonoBehaviour = Regex.IsMatch(content, @":\s*MonoBehaviour");

            if (isSimulation && usesMonoBehaviour && !path.Contains("/Presentation/"))
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Simulation class uses MonoBehaviour — consider ECS IComponentData for performance.",
                    Suggestions = new[] { "Use IComponentData struct for simulation data.", "Keep MonoBehaviour only in Presentation layer for visuals.", "Use Entities.Graphics for rendering ECS entities." },
                    Examples = new[] { "public struct ShipComponent : IComponentData\n{\n    public float Hull;\n    public float Speed;\n}", "// Presentation only — syncs visual to ECS entity\npublic class ShipView : MonoBehaviour { }", "// ECS System processes all ShipComponents in Burst\n[BurstCompile]\npublic partial struct ShipMovementSystem : ISystem { }" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    internal class MutableECSComponentRule : ICodeRule
    {
        public string Id => "ECS002";
        public string Category => "ECS / DOTS";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // ECS components should be structs with public fields, not classes with properties
            bool hasIComponentData = content.Contains("IComponentData");
            bool usesClass = Regex.IsMatch(content, @"public\s+class\s+\w+\s*:\s*IComponentData");

            if (hasIComponentData && usesClass)
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "IComponentData should be a struct, not a class — class components cause GC pressure.",
                    Suggestions = new[] { "Change class to struct.", "Keep ECS components as plain data — no logic.", "Logic belongs in ISystem, not IComponentData." },
                    Examples = new[] { "public struct ShipComponent : IComponentData\n{\n    public float Hull;\n    public float Speed;\n}", "// No methods in components — data only", "// All logic in Burst-compiled systems\npublic partial struct DamageSystem : ISystem { }" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    // ── Best Practice Rules ───────────────────────────────────────────────

    internal class DirectPlayerPrefsRule : ICodeRule
    {
        public string Id => "BP001";
        public string Category => "Best Practices";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            if (Regex.IsMatch(content, @"PlayerPrefs\.(Get|Set)", RegexOptions.IgnoreCase))
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Direct PlayerPrefs usage detected — wrap in a storage service.",
                    Suggestions = new[] { "Create ISaveService abstraction.", "Use Unity Cloud Save for cross-platform data.", "Encrypt sensitive data before saving." },
                    Examples = new[] { "public interface ISaveService { void Save<T>(string key, T value); }", "await CloudSaveService.Instance.Data.Player.SaveAsync(data);", "var encrypted = AesEncrypt(JsonUtility.ToJson(data));" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    internal class FindObjectUsageRule : ICodeRule
    {
        public string Id => "BP002";
        public string Category => "Best Practices";
        public IssueSeverity DefaultSeverity => IssueSeverity.Critical;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            if (Regex.IsMatch(content, @"FindObjectOfType|FindObjectsOfType|GameObject\.Find\b"))
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Find/FindObjectOfType detected — extremely slow, use DI instead.",
                    Suggestions = new[] { "Inject dependency via VContainer constructor injection.", "Use R3 Observable events instead of polling.", "Register as singleton in GameBootstrapper." },
                    Examples = new[] { "public MyClass(IFleetRepository repo) { _repo = repo; }", "Observable.FromEvent(...).Subscribe(OnShipDestroyed);", "builder.RegisterInstance(_uiManager).As<IUIManager>();" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    internal class SingletonAntiPatternRule : ICodeRule
    {
        public string Id => "BP003";
        public string Category => "Best Practices";
        public IssueSeverity DefaultSeverity => IssueSeverity.Warning;

        public bool Analyze(string path, string content, out CodeIssue issue)
        {
            // Detects classic Singleton pattern
            bool hasSingleton = Regex.IsMatch(content, @"static\s+\w+\s+Instance") &&
                                 Regex.IsMatch(content, @"private\s+static");

            if (hasSingleton)
            {
                issue = new CodeIssue
                {
                    FilePath = path, RuleId = Id, Category = Category, Severity = DefaultSeverity,
                    Message = "Singleton pattern detected — replace with VContainer DI for testability.",
                    Suggestions = new[] { "Register as singleton scope in VContainer.", "Inject via constructor — never access via static Instance.", "Singletons make unit testing impossible." },
                    Examples = new[] { "builder.Register<FleetService>(Lifetime.Singleton).As<IFleetService>();", "public GameEntryPoint(IFleetService fleet) { _fleet = fleet; }", "// In tests: inject a mock instead\nvar mock = new MockFleetService();" }
                };
                return true;
            }

            issue = null;
            return false;
        }
    }

    // ── Editor Window ─────────────────────────────────────────────────────

    public class CodeHealthReviewerWindow : EditorWindow
    {
        private Vector2 _scroll;
        private int _expandedIndex = -1;
        private bool _showCritical = true;
        private bool _showWarning = true;
        private bool _showInfo = true;

        public static void ShowWindow()
        {
            var win = GetWindow<CodeHealthReviewerWindow>("🔍 Galactic Empire — Code Issues");
            win.minSize = new Vector2(520, 400);
            win.Show();
        }

        private void OnGUI()
        {
            if (AdvancedCodeHealthCheckerFoundIssuesCount() == 0)
            {
                EditorGUILayout.HelpBox("✅ No issues found. Run the scanner first via GalacticEmpire menu.", MessageType.Info);

                if (GUILayout.Button("🔍 Run Scan Now", GUILayout.Height(32)))
                    CodeHealthChecker.RunScan();

                return;
            }

            DrawToolbar();
            DrawFilters();
            DrawIssueList();
        }

        private int AdvancedCodeHealthCheckerFoundIssuesCount() =>
            CodeHealthChecker.FoundIssues.Count;

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUILayout.LabelField($"📋 Issues: {CodeHealthChecker.FoundIssues.Count}", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("🔄 Rescan", EditorStyles.toolbarButton))
            {
                _expandedIndex = -1;
                CodeHealthChecker.RunScan();
            }

            if (GUILayout.Button("📤 Export MD", EditorStyles.toolbarButton))
                CodeHealthChecker.ExportMarkdown();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFilters()
        {
            EditorGUILayout.BeginHorizontal();
            _showCritical = GUILayout.Toggle(_showCritical, "🔴 Critical", EditorStyles.miniButton);
            _showWarning  = GUILayout.Toggle(_showWarning,  "🟡 Warning",  EditorStyles.miniButton);
            _showInfo     = GUILayout.Toggle(_showInfo,     "🔵 Info",     EditorStyles.miniButton);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawIssueList()
        {
            var visible = CodeHealthChecker.FoundIssues
                .Where(i => (i.Severity == IssueSeverity.Critical && _showCritical) ||
                            (i.Severity == IssueSeverity.Warning  && _showWarning)  ||
                            (i.Severity == IssueSeverity.Info     && _showInfo))
                .ToList();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < visible.Count; i++)
            {
                var issue = visible[i];
                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.LabelField(
                    $"[{issue.Severity}] [{issue.Category}] — {Path.GetFileName(issue.FilePath)}",
                    EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    $"⚠️ {issue.Message}",
                    new GUIStyle(EditorStyles.wordWrappedMiniLabel) { fontStyle = FontStyle.Bold });

                bool expanded = _expandedIndex == i;

                if (GUILayout.Button(expanded ? "🔼 Collapse" : "🔽 Show Suggestions", EditorStyles.miniButton))
                    _expandedIndex = expanded ? -1 : i;

                if (expanded)
                {
                    EditorGUILayout.BeginVertical("helpBox");

                    for (int j = 0; j < issue.Suggestions.Length; j++)
                    {
                        EditorGUILayout.LabelField($"{j + 1}. {issue.Suggestions[j]}", EditorStyles.wordWrappedLabel);
                        EditorGUILayout.SelectableLabel($"   ↳ {issue.Examples[j]}", EditorStyles.wordWrappedMiniLabel, GUILayout.Height(36));
                        EditorGUILayout.Space(2);
                    }

                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("📂 Open", GUILayout.Height(20)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.FilePath);
                    if (obj != null) AssetDatabase.OpenAsset(obj);
                }

                if (GUILayout.Button("🎯 Ping", GUILayout.Height(20)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(issue.FilePath);
                    if (obj != null) EditorGUIUtility.PingObject(obj);
                }

                if (GUILayout.Button("🔕 Ignore", GUILayout.Height(20)))
                {
                    string key = $"{issue.FilePath}|{issue.RuleId}";
                    var ignored = EditorPrefs.GetString("GE_CodeHealth_Ignored", "").Split(';').ToList();

                    if (!ignored.Contains(key))
                    {
                        ignored.Add(key);
                        EditorPrefs.SetString("GE_CodeHealth_Ignored", string.Join(";", ignored));
                    }

                    _expandedIndex = -1;
                }

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(2);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
