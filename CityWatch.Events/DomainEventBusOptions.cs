namespace CityWatch.Events
{
    /// <summary>
    /// Tuning for the in-process bus. Defaults are sized for the current platform
    /// (827 sites, 1,202 guards) with a wide margin.
    /// </summary>
    public sealed class DomainEventBusOptions
    {
        /// <summary>
        /// Maximum events held between publisher and dispatcher.
        /// </summary>
        /// <remarks>
        /// Bounded on purpose. An unbounded queue converts a slow or stuck subscriber into an
        /// out-of-memory crash of the whole application, which would mean a new feature pack could
        /// take down NFC scanning — exactly the failure the bus exists to prevent. When the buffer
        /// is full the oldest event is dropped and <see cref="DomainEventBusMetrics.Dropped"/>
        /// increments, so the loss is visible rather than silent.
        /// </remarks>
        public int Capacity { get; set; } = 10_000;

        /// <summary>
        /// How long a single handler may run before the dispatcher abandons it and moves on.
        /// Prevents one wedged handler from stalling the pump for every other subscriber.
        /// </summary>
        public int HandlerTimeoutSeconds { get; set; } = 30;
    }
}
