using System;

namespace CityWatch.Events.Events
{
    /// <summary>
    /// A patrol began. Published where PcarVisitHistory records the "Started" action.
    /// </summary>
    public sealed class PatrolStarted : DomainEvent
    {
        public PatrolStarted(int smartWandId, int? guardId, int? pcarRouteId, int clientSiteId, DateTime occurredUtc)
            : base(occurredUtc)
        {
            SmartWandId = smartWandId;
            GuardId = guardId;
            PcarRouteId = pcarRouteId;
            ClientSiteId = clientSiteId;
        }

        public int SmartWandId { get; }

        public int? GuardId { get; }

        /// <summary>Planned route, when the patrol is running one. Null for ad-hoc patrols.</summary>
        public int? PcarRouteId { get; }

        public int ClientSiteId { get; }
    }

    /// <summary>
    /// A patrol finished or was cancelled.
    /// </summary>
    public sealed class PatrolEnded : DomainEvent
    {
        public PatrolEnded(int smartWandId, int? guardId, string reason, DateTime occurredUtc)
            : base(occurredUtc)
        {
            SmartWandId = smartWandId;
            GuardId = guardId;
            Reason = reason;
        }

        public int SmartWandId { get; }

        public int? GuardId { get; }

        /// <summary>Matches the existing PcarVisitHistory action vocabulary: Completed, Cancelled, Pushed.</summary>
        public string Reason { get; }
    }

    /// <summary>
    /// A patrol vehicle left a site geofence. Raised inside the tracking pack by the geofence
    /// evaluator, not by existing platform code — it is on the bus so that Analytics and the
    /// client portal can consume it later without going through the tracking module.
    /// </summary>
    public sealed class PatrolVehicleExited : DomainEvent
    {
        public PatrolVehicleExited(int smartWandId, int clientSiteId, DateTime occurredUtc)
            : base(occurredUtc)
        {
            SmartWandId = smartWandId;
            ClientSiteId = clientSiteId;
        }

        public int SmartWandId { get; }

        public int ClientSiteId { get; }
    }
}
