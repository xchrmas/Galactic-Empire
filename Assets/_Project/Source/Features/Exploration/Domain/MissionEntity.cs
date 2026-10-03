// A fleet's trip to a sector and back. All mutations return a new record -
// nothing here is mutable. Time always comes from an IClock, never read
// directly, so travel duration can be simulated and tested deterministically.

using System;
using GalacticEmpire.Core;

namespace GalacticEmpire.Feature.Exploration.Domain
{
    /// <summary>Aggregate root representing one fleet's exploration trip.</summary>
    public sealed record MissionEntity
    {
        public Guid Id { get; init; }
        public Guid FleetId { get; init; }
        public Guid OriginSectorId { get; init; }
        public Guid TargetSectorId { get; init; }
        public MissionStatus Status { get; init; }

        public DateTime StartedAtUtc { get; init; }
        public DateTime ArrivesAtUtc { get; init; }

        // Set only once the fleet turns back (StartReturn) - null during OutboundTravel/Arrived
        public DateTime? ReturnArrivesAtUtc { get; init; }

        public bool IsOutbound => Status == MissionStatus.OutboundTravel;
        public bool IsCompleted => Status == MissionStatus.Completed;

        /// <summary>Starts a new mission - fleet departs now, heading for the target sector.</summary>
        public static MissionEntity Create(
            Guid fleetId,
            Guid originSectorId,
            Guid targetSectorId,
            TimeSpan travelDuration,
            IClock clock)
        {
            if (fleetId == Guid.Empty)
                throw new ArgumentException("Mission needs a fleet.", nameof(fleetId));

            if (originSectorId == Guid.Empty)
                throw new ArgumentException("Mission needs an origin sector.", nameof(originSectorId));

            if (targetSectorId == Guid.Empty)
                throw new ArgumentException("Mission needs a target sector.", nameof(targetSectorId));

            if (originSectorId == targetSectorId)
                throw new ArgumentException("Target sector must differ from the origin.", nameof(targetSectorId));

            if (travelDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(travelDuration), "Travel duration must be positive.");

            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            var now = clock.UtcNow;

            return new MissionEntity
            {
                Id       = Guid.NewGuid(),
                FleetId  = fleetId,
                OriginSectorId = originSectorId,
                TargetSectorId = targetSectorId,
                Status   = MissionStatus.OutboundTravel,

                StartedAtUtc = now,
                ArrivesAtUtc = now + travelDuration,
                ReturnArrivesAtUtc = null
            };
        }

        /// <summary>Fleet reached the target sector - ready for discovery/battle/reward resolution.</summary>
        public MissionEntity MarkArrived()
        {
            if (Status != MissionStatus.OutboundTravel)
                throw new InvalidOperationException($"Mission {Id} is not outbound, cannot arrive.");

            return this with { Status = MissionStatus.Arrived };
        }

        /// <summary>Fleet turns back toward the origin sector.</summary>
        public MissionEntity StartReturn(TimeSpan travelDuration, IClock clock)
        {
            if (Status != MissionStatus.Arrived)
                throw new InvalidOperationException($"Mission {Id} has not arrived yet, cannot return.");

            if (travelDuration <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(travelDuration), "Travel duration must be positive.");

            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            return this with
            {
                Status = MissionStatus.ReturnTravel,
                ReturnArrivesAtUtc = clock.UtcNow + travelDuration
            };
        }

        /// <summary>Fleet is back at the origin sector - mission is done.</summary>
        public MissionEntity Complete()
        {
            if (Status != MissionStatus.ReturnTravel)
                throw new InvalidOperationException($"Mission {Id} is not returning, cannot complete.");

            return this with { Status = MissionStatus.Completed };
        }

        /// <summary>True once the current clock time has reached ArrivesAtUtc (still OutboundTravel).</summary>
        public bool HasReachedTarget(IClock clock)
        {
            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            return Status == MissionStatus.OutboundTravel && clock.UtcNow >= ArrivesAtUtc;
        }

        /// <summary>True once the current clock time has reached ReturnArrivesAtUtc (still ReturnTravel).</summary>
        public bool HasReturnedHome(IClock clock)
        {
            if (clock == null)
                throw new ArgumentNullException(nameof(clock));

            return Status == MissionStatus.ReturnTravel
                && ReturnArrivesAtUtc.HasValue
                && clock.UtcNow >= ReturnArrivesAtUtc.Value;
        }
    }
}
