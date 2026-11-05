using System;

namespace CityWatch.Events.Events
{
    /// <summary>
    /// A control-room operator requested live tracking of a specific vehicle.
    /// </summary>
    /// <remarks>
    /// This is a deliberate act of close surveillance of a named officer, so the operator's identity
    /// is part of the event rather than something a subscriber has to go and find. Audit consumes it
    /// directly.
    /// </remarks>
    public sealed class LiveTrackingRequested : DomainEvent
    {
        public LiveTrackingRequested(int smartWandId, int operatorUserId, int ttlSeconds, DateTime occurredUtc)
            : base(occurredUtc)
        {
            SmartWandId = smartWandId;
            OperatorUserId = operatorUserId;
            TtlSeconds = ttlSeconds;
        }

        public int SmartWandId { get; }

        public int OperatorUserId { get; }

        /// <summary>
        /// Live Mode is always time-bounded. A forgotten live session must expire on its own
        /// rather than drain a battery for the rest of a shift.
        /// </summary>
        public int TtlSeconds { get; }
    }

    /// <summary>
    /// Live tracking ended, either because the operator stopped it or because the TTL expired.
    /// </summary>
    public sealed class LiveTrackingEnded : DomainEvent
    {
        public LiveTrackingEnded(int smartWandId, int? operatorUserId, string reason, DateTime occurredUtc)
            : base(occurredUtc)
        {
            SmartWandId = smartWandId;
            OperatorUserId = operatorUserId;
            Reason = reason;
        }

        public int SmartWandId { get; }

        /// <summary>Null when the session expired rather than being cancelled by a person.</summary>
        public int? OperatorUserId { get; }

        /// <summary>Cancelled, Expired, DuressOverride, SessionEnded.</summary>
        public string Reason { get; }
    }
}
