using System;

namespace CityWatch.Events.Events
{
    /// <summary>
    /// An officer signed in on a device. Published from the existing guard login path.
    /// </summary>
    public sealed class OfficerLoggedIn : DomainEvent
    {
        public OfficerLoggedIn(int guardId, int? smartWandId, int clientSiteId, string? deviceId, DateTime occurredUtc)
            : base(occurredUtc)
        {
            GuardId = guardId;
            SmartWandId = smartWandId;
            ClientSiteId = clientSiteId;
            DeviceId = deviceId;
        }

        public int GuardId { get; }

        /// <summary>
        /// ClientSiteSmartWand.Id — the tracking unit key. Null when the officer signed in
        /// without a wand allocation, in which case there is nothing to track.
        /// </summary>
        public int? SmartWandId { get; }

        public int ClientSiteId { get; }

        public string? DeviceId { get; }
    }

    /// <summary>
    /// An officer signed out. Subscribers must treat this as a hard stop.
    /// </summary>
    /// <remarks>
    /// For the tracking pack this is the privacy guarantee, not a housekeeping notification:
    /// the session closes and location capture ceases immediately. There is no configuration
    /// under which tracking survives this event.
    /// </remarks>
    public sealed class OfficerLoggedOut : DomainEvent
    {
        public OfficerLoggedOut(int guardId, int? smartWandId, DateTime occurredUtc)
            : base(occurredUtc)
        {
            GuardId = guardId;
            SmartWandId = smartWandId;
        }

        public int GuardId { get; }

        public int? SmartWandId { get; }
    }
}
