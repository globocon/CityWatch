using System.Runtime.CompilerServices;

// Internal helpers (plausibility maths, source parsing) are tested directly rather than
// through the full ingest flow, so their edge cases stay cheap to pin down.
[assembly: InternalsVisibleTo("CityWatch.Tracking.Tests")]
