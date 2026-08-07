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
    /// Two key spaces therefore share the UnitId column, kept apart by an offset so they can
    /// never collide (position ids are 10-24, wand ids run past 140):
    ///
    ///     UnitId  &lt; 2,000,000   a SmartWand device        (ClientSiteSmartWand.Id)
    ///     UnitId &gt;= 2,000,000   a patrol car Position     (IncidentReportPositions.Id + offset)
    ///
    /// A car-based id reads plainly: 2,000,010 is Position 10, "Mobile Patrols (Car) M1".
    /// </remarks>
    public static class TrackingUnitKey
    {
        /// <summary>Added to a Position id to form its unit key. Must match the mobile app.</summary>
        public const int PositionOffset = 2_000_000;

        public static int FromPosition(int positionId) => PositionOffset + positionId;

        public static bool IsPosition(int unitId) => unitId >= PositionOffset;

        /// <summary>Position id behind a car-based unit key, or null for a wand-based one.</summary>
        public static int? ToPositionId(int unitId)
            => IsPosition(unitId) ? unitId - PositionOffset : (int?)null;
    }
}
