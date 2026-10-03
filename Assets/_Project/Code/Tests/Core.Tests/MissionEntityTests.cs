// Layer: Core.Tests - Unit tests for MissionEntity domain logic.

using System;
using NUnit.Framework;
using GalacticEmpire.Feature.Exploration.Domain;

namespace GalacticEmpire.Core.Tests
{
    [TestFixture]
    public sealed class MissionEntityTests
    {
        private static readonly DateTime StartUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static readonly TimeSpan TravelDuration = TimeSpan.FromMinutes(10);

        private FakeClock _clock;

        [SetUp]
        public void SetUp()
        {
            _clock = new FakeClock(StartUtc);
        }

        // Create

        [Test]
        public void Create_WithValidData_ReturnsOutboundMission()
        {
            var fleetId = Guid.NewGuid();
            var originId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            var mission = MissionEntity.Create(fleetId, originId, targetId, TravelDuration, _clock);

            Assert.That(mission.FleetId, Is.EqualTo(fleetId));
            Assert.That(mission.OriginSectorId, Is.EqualTo(originId));
            Assert.That(mission.TargetSectorId, Is.EqualTo(targetId));
            Assert.That(mission.Status, Is.EqualTo(MissionStatus.OutboundTravel));
            Assert.That(mission.IsOutbound, Is.True);
        }

        [Test]
        public void Create_SetsStartedAtUtcToClockNow()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            Assert.That(mission.StartedAtUtc, Is.EqualTo(StartUtc));
        }

        [Test]
        public void Create_SetsArrivesAtUtcToStartPlusTravelDuration()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            Assert.That(mission.ArrivesAtUtc, Is.EqualTo(StartUtc + TravelDuration));
        }

        [Test]
        public void Create_LeavesReturnArrivesAtUtcNull()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            Assert.That(mission.ReturnArrivesAtUtc, Is.Null);
        }

        [Test]
        public void Create_WithEmptyFleetId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                MissionEntity.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock));
        }

        [Test]
        public void Create_WithEmptyOriginSectorId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                MissionEntity.Create(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), TravelDuration, _clock));
        }

        [Test]
        public void Create_WithEmptyTargetSectorId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
                MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty, TravelDuration, _clock));
        }

        [Test]
        public void Create_WithSameOriginAndTarget_ThrowsArgumentException()
        {
            var sectorId = Guid.NewGuid();

            Assert.Throws<ArgumentException>(() =>
                MissionEntity.Create(Guid.NewGuid(), sectorId, sectorId, TravelDuration, _clock));
        }

        [Test]
        public void Create_WithZeroTravelDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TimeSpan.Zero, _clock));
        }

        [Test]
        public void Create_WithNegativeTravelDuration_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TimeSpan.FromMinutes(-1), _clock));
        }

        [Test]
        public void Create_WithNullClock_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() =>
                MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, null));
        }

        // MarkArrived

        [Test]
        public void MarkArrived_ChangesStatusToArrived()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            var arrived = mission.MarkArrived();

            Assert.That(arrived.Status, Is.EqualTo(MissionStatus.Arrived));
        }

        [Test]
        public void MarkArrived_DoesNotModifyOriginalMission()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            mission.MarkArrived();

            Assert.That(mission.Status, Is.EqualTo(MissionStatus.OutboundTravel));
        }

        [Test]
        public void MarkArrived_WhenAlreadyArrived_ThrowsInvalidOperationException()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived();

            Assert.Throws<InvalidOperationException>(() => mission.MarkArrived());
        }

        // StartReturn

        [Test]
        public void StartReturn_ChangesStatusToReturnTravel()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived();

            var returning = mission.StartReturn(TravelDuration, _clock);

            Assert.That(returning.Status, Is.EqualTo(MissionStatus.ReturnTravel));
        }

        [Test]
        public void StartReturn_SetsReturnArrivesAtUtc()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived();
            _clock.Advance(TimeSpan.FromMinutes(2));

            var returning = mission.StartReturn(TravelDuration, _clock);

            Assert.That(returning.ReturnArrivesAtUtc, Is.EqualTo(_clock.UtcNow + TravelDuration));
        }

        [Test]
        public void StartReturn_WhenNotArrived_ThrowsInvalidOperationException()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            Assert.Throws<InvalidOperationException>(() => mission.StartReturn(TravelDuration, _clock));
        }

        [Test]
        public void StartReturn_WithZeroTravelDuration_ThrowsArgumentOutOfRangeException()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived();

            Assert.Throws<ArgumentOutOfRangeException>(() => mission.StartReturn(TimeSpan.Zero, _clock));
        }

        // Complete

        [Test]
        public void Complete_ChangesStatusToCompleted()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived()
                .StartReturn(TravelDuration, _clock);

            var completed = mission.Complete();

            Assert.That(completed.Status, Is.EqualTo(MissionStatus.Completed));
            Assert.That(completed.IsCompleted, Is.True);
        }

        [Test]
        public void Complete_WhenNotReturning_ThrowsInvalidOperationException()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived();

            Assert.Throws<InvalidOperationException>(() => mission.Complete());
        }

        // HasReachedTarget

        [Test]
        public void HasReachedTarget_BeforeArrivalTime_ReturnsFalse()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);
            _clock.Advance(TimeSpan.FromMinutes(5));

            Assert.That(mission.HasReachedTarget(_clock), Is.False);
        }

        [Test]
        public void HasReachedTarget_AtOrAfterArrivalTime_ReturnsTrue()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);
            _clock.Advance(TravelDuration);

            Assert.That(mission.HasReachedTarget(_clock), Is.True);
        }

        [Test]
        public void HasReachedTarget_WhenAlreadyArrived_ReturnsFalse()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived();
            _clock.Advance(TravelDuration);

            Assert.That(mission.HasReachedTarget(_clock), Is.False);
        }

        [Test]
        public void HasReachedTarget_WithNullClock_ThrowsArgumentNullException()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);

            Assert.Throws<ArgumentNullException>(() => mission.HasReachedTarget(null));
        }

        // HasReturnedHome

        [Test]
        public void HasReturnedHome_BeforeReturnArrival_ReturnsFalse()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived()
                .StartReturn(TravelDuration, _clock);
            _clock.Advance(TimeSpan.FromMinutes(5));

            Assert.That(mission.HasReturnedHome(_clock), Is.False);
        }

        [Test]
        public void HasReturnedHome_AtOrAfterReturnArrival_ReturnsTrue()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived()
                .StartReturn(TravelDuration, _clock);
            _clock.Advance(TravelDuration);

            Assert.That(mission.HasReturnedHome(_clock), Is.True);
        }

        [Test]
        public void HasReturnedHome_WhileStillOutbound_ReturnsFalse()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock);
            _clock.Advance(TravelDuration * 5);

            Assert.That(mission.HasReturnedHome(_clock), Is.False);
        }

        [Test]
        public void HasReturnedHome_WithNullClock_ThrowsArgumentNullException()
        {
            var mission = MissionEntity.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), TravelDuration, _clock)
                .MarkArrived()
                .StartReturn(TravelDuration, _clock);

            Assert.Throws<ArgumentNullException>(() => mission.HasReturnedHome(null));
        }
    }
}
