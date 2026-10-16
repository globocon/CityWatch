namespace CityWatch.Tracking.Contracts
{
    /// <summary>
    /// How a tracked unit is identified.
    /// </summary>
    /// <remarks>
    /// The tracked thing is the CAR, and the car is the Position chosen at login. But patrol
    /// officers frequently log in WITHOUT selecting a SmartWand — every observed PCAR login
    /// has SmartWandId NULL — so the wand cannot be the unit key.
    ///
    /// The DEVICE is never the unit. A "SmartWand" record is just a registered phone (it
    /// carries IMEI, DeviceId, PhoneNumber) — it identifies hardware, not the thing on patrol.
    /// What is actually tracked is a car or a person:
    ///
    ///     a patrol car  -> the Position picked at login ("Mobile Patrols (Car) M1")
    ///     a foot guard  -> the guard themselves
    ///
    /// Both share the UnitId column, kept apart by offsets so they can never collide
    /// (guard ids reach ~1,200; position ids are 10-24):
    ///
    ///     UnitId &gt;= 2,000,000   a patrol car   (IncidentReportPositions.Id + 2,000,000)
    ///     UnitId &gt;= 1,000,000   a foot guard   (Guards.Id + 1,000,000)
    ///
    /// Both read plainly: 2,000,010 is Position 10 "Mobile Patrols (Car) M1"; 1,000,004 is
    /// guard 4. Values below 1,000,000 are legacy device-keyed units and are no longer issued.
    /// </remarks>
    public static class TrackingUnitKey
    {
        /// <summary>Added to a Position id to form a car's unit key. Must match the mobile app.</summary>
        public const int PositionOffset = 2_000_000;

        /// <summary>Added to a Guard id to form a foot guard's unit key. Must match the mobile app.</summary>
        public const int GuardOffset = 1_000_000;

        public static int FromPosition(int positionId) => PositionOffset + positionId;

        public static int FromGuard(int guardId) => GuardOffset + guardId;

        public static bool IsPosition(int unitId) => unitId >= PositionOffset;

        public static bool IsGuard(int unitId) => unitId >= GuardOffset && unitId < PositionOffset;

        /// <summary>Position id behind a car unit key, or null if it is not a car.</summary>
        public static int? ToPositionId(int unitId)
            => IsPosition(unitId) ? unitId - PositionOffset : (int?)null;

        /// <summary>Guard id behind a foot-guard unit key, or null if it is not one.</summary>
        public static int? ToGuardId(int unitId)
            => IsGuard(unitId) ? unitId - GuardOffset : (int?)null;
    }
}
