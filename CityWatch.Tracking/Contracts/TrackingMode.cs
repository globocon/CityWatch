namespace CityWatch.Tracking.Contracts
{
    /// <summary>
    /// The four tracking modes (§5.1). Precedence: Duress > Live > Transit > Normal.
    /// Values are stable — they are stored in TrackPoint.ModeAtCapture and travel in the
    /// ingest contract, so they must never be renumbered.
    /// </summary>
    public enum TrackingMode : byte
    {
        /// <summary>NFC anchors only. No continuous GPS. The default, and the cheap mode.</summary>
        Normal = 1,

        /// <summary>Adaptive sampling while travelling between sites.</summary>
        Transit = 2,

        /// <summary>Operator-requested high-frequency tracking. Always TTL-bounded.</summary>
        Live = 3,

        /// <summary>Continuous updates until explicitly cancelled. No timeout, ever.</summary>
        Duress = 4
    }

    /// <summary>
    /// Where a point came from. Stored in TrackPoint.SourceType; a future model must be able
    /// to weight an NFC-anchored fix differently from a free-floating GPS fix, and quality
    /// that is not recorded at write time cannot be recovered later (§14).
    /// </summary>
    public enum TrackPointSource : byte
    {
        /// <summary>GPS stamped on an NFC checkpoint scan — the highest-trust point.</summary>
        NfcAnchor = 1,

        Transit = 2,

        Live = 3,

        Duress = 4
    }

    /// <summary>Quality flags, combinable. Zero means a clean point.</summary>
    [System.Flags]
    public enum TrackPointFlags : byte
    {
        None = 0,

        /// <summary>Android reported a mock location provider.</summary>
        MockProvider = 1,

        /// <summary>Accuracy worse than the configured threshold; kept but not trusted.</summary>
        LowAccuracy = 2,

        /// <summary>Failed the implied-speed plausibility check. Flagged, never dropped —
        /// a flagged teleport is evidence; a dropped point is an unexplainable gap.</summary>
        Implausible = 4,

        /// <summary>Replayed from the device's offline cache; must not animate as live movement.</summary>
        Backfilled = 8
    }
}
