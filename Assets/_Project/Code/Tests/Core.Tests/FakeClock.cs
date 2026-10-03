// Test double for IClock - lets tests control "now" and advance it manually.

using System;
using GalacticEmpire.Core;

namespace GalacticEmpire.Core.Tests
{
    public sealed class FakeClock : IClock
    {
        public DateTime UtcNow { get; private set; }

        public FakeClock(DateTime startUtc)
        {
            UtcNow = startUtc;
        }

        public void Advance(TimeSpan amount)
        {
            UtcNow += amount;
        }
    }
}
