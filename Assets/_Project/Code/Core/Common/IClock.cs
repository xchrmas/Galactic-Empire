// Abstraction over UTC time - Domain entities take time in, they never read it.

using System;

namespace GalacticEmpire.Core
{
    /// <summary>Provides the current UTC time - lets tests inject a fake clock.</summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
    }
}
