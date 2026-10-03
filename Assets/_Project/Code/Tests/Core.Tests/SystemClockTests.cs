// Layer: Core.Tests - Unit tests for SystemClock.

using System;
using NUnit.Framework;
using GalacticEmpire.Core;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class SystemClockTests
    {
        [Test]
        public void UtcNow_ReturnsCurrentUtcTime()
        {
            var clock = new SystemClock();

            var before = DateTime.UtcNow;
            var reading = clock.UtcNow;
            var after = DateTime.UtcNow;

            Assert.That(reading, Is.InRange(before, after));
        }

        [Test]
        public void UtcNow_HasUtcKind()
        {
            var clock = new SystemClock();

            Assert.That(clock.UtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }
}
