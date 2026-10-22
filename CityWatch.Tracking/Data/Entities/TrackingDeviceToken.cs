using System;

namespace CityWatch.Tracking.Data.Entities
{
    /// <summary>
    /// An FCM registration token for a tracking unit's phone (DbScript 369). A token is a
    /// NUDGE address, never a data channel: the server uses it to ask a device for a fresh
    /// position; the position itself always arrives on the audited ingest path. A device may
    /// only register a token for the unit whose ACTIVE session it holds — the same trust
    /// model as ingest (§13.1.1). Several rows per unit are expected over time (reinstalls,
    /// replacement phones); dead tokens are deactivated, never blindly trusted unique.
    /// </summary>
    public class TrackingDeviceToken
    {
        public int Id { get; set; }

        /// <summary>TrackingUnitKey — the car/guard identity, never the SmartWand device.</summary>
        public int UnitId { get; set; }

        public string FcmToken { get; set; } = string.Empty;

        /// <summary>"android" today; APNs would add "ios" without a schema change.</summary>
        public string Platform { get; set; } = "android";

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }

        /// <summary>Last time the device re-confirmed this token (login or token refresh).</summary>
        public DateTime? LastSeenUtc { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Set when FCM reports the token dead (Unregistered) or the device logs out.</summary>
        public DateTime? InvalidatedUtc { get; set; }
    }
}
