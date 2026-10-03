// Real clock backed by DateTime.UtcNow - registered in the DI container.

using System;

namespace GalacticEmpire.Core
{
    /// <summary>Production IClock implementation - wraps DateTime.UtcNow.</summary>
    public sealed class SystemClock : IClock
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
