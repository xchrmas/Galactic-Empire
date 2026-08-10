// Compatibility shim for C# init-only setters in older Unity runtimes.
// Defines the required type so code using 'init' compiles.

namespace System.Runtime.CompilerServices
{
    // Internal by design to avoid exposing API surface.
    internal static class IsExternalInit { }
}
