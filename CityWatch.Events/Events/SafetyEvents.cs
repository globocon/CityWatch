using System;

namespace CityWatch.Events.Events
{
    /// <summary>
    /// A duress alarm was raised.
    /// </summary>
    /// <remarks>
    /// <b>This event is an observer, never the mechanism.</b> The existing duress path — alerting,
    /// email, SMS, the control-room banner — is untouched and continues to work whether or not
    /// anything subscribes here. The tracking pack uses this only to escalate the device into
    /// Duress Mode. If the bus is down, duress still fires; the officer just does not get the
    /// higher location sample rate.
    ///
    /// That separation is deliberate. Officer safety functions must not share a failure domain
    /// with a reporting feature.
    /// </remarks>
    public sealed class DuressActivated : DomainEvent
    {
        public DuressActivated(int? guardId, int? smartWandId, int clientSiteId, string? gpsCoordinates, DateTime occurredUtc)
            : base(occurredUtc)
        {
            GuardId = guardId;
            SmartWandId = smartWandId;
            ClientSiteId = clientSiteId;
            GpsCoordinates = gpsCoordinates;
        }

        public int? GuardId { get; }

        public int? SmartWandId { get; }

        public int ClientSiteId { get; }

        /// <summary>Position at the moment duress was raised, when the device had one.</summary>
        public string? GpsCoordinates { get; }
    }
}
