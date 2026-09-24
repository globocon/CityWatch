using System.Runtime.CompilerServices;

// The dispatcher's per-event DispatchAsync is internal: production code only ever sees the
// channel pump. Tests drive it directly to assert isolation properties deterministically.
[assembly: InternalsVisibleTo("CityWatch.Events.Tests")]
