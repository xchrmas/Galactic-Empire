// Layer: Editor | Forces C# latest version in all generated .csproj files.

using UnityEditor;

namespace GalacticEmpire.Editor
{
    // Note: Using old-style namespace here intentionally —
    // this file runs BEFORE the C# version fix is applied.

    /// <summary>Patches generated .csproj to use latest C# version.</summary>
    public sealed class CSharpProjectPostprocessor : AssetPostprocessor
    {
        private static string OnGeneratedCSProject(string path, string content)
        {
            return content
                .Replace("<LangVersion>9.0</LangVersion>", "<LangVersion>latest</LangVersion>")
                .Replace("<LangVersion>8.0</LangVersion>", "<LangVersion>latest</LangVersion>")
                .Replace("<Nullable>disable</Nullable>",   "<Nullable>enable</Nullable>");
        }
    }
}
