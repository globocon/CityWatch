using System.Threading;

namespace CityWatch.Events
{
    /// <summary>
    /// Counters for the bus. Deliberately trivial — no metrics package dependency.
    /// </summary>
    /// <remarks>
    /// <see cref="Dropped"/> is the number that matters operationally. It should be zero. A
    /// non-zero and rising value means a subscriber cannot keep up and events are being lost,
    /// which is the designed failure mode but must never be an invisible one.
    /// </remarks>
    public sealed class DomainEventBusMetrics
    {
        private long _published;
        private long _dropped;
        private long _handled;
        private long _handlerFailures;
        private long _handlerTimeouts;

        public long Published => Interlocked.Read(ref _published);
        public long Dropped => Interlocked.Read(ref _dropped);
        public long Handled => Interlocked.Read(ref _handled);
        public long HandlerFailures => Interlocked.Read(ref _handlerFailures);
        public long HandlerTimeouts => Interlocked.Read(ref _handlerTimeouts);

        internal void RecordPublished() => Interlocked.Increment(ref _published);
        internal void RecordDropped() => Interlocked.Increment(ref _dropped);
        internal void RecordHandled() => Interlocked.Increment(ref _handled);
        internal void RecordHandlerFailure() => Interlocked.Increment(ref _handlerFailures);
        internal void RecordHandlerTimeout() => Interlocked.Increment(ref _handlerTimeouts);
    }
}
