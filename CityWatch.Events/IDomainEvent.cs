using System;

namespace CityWatch.Events
{
    /// <summary>
    /// Marker for anything that can travel on the domain event bus.
    /// </summary>
    /// <remarks>
    /// Two clocks are carried on every event, following the same discipline as
    /// PcarVisitHistory: <see cref="OccurredUtc"/> is when the thing happened (which may be a
    /// device clock, minutes or hours before it reached the server), and <see cref="PublishedUtc"/>
    /// is when the server saw it. Storing both is what lets clock skew be measured rather than
    /// silently absorbed as noise.
    /// </remarks>
    public interface IDomainEvent
    {
        /// <summary>Stable identity for the event, so handlers can be idempotent.</summary>
        Guid EventId { get; }

        /// <summary>When the event actually happened, in UTC.</summary>
        DateTime OccurredUtc { get; }

        /// <summary>When the server published it, in UTC. Set by the publisher, not the caller.</summary>
        DateTime PublishedUtc { get; set; }
    }

    /// <summary>
    /// Base implementation supplying identity and the two clocks. Events are plain data:
    /// no behaviour, no references to entities, nothing that would tie a subscriber to
    /// the publisher's data layer.
    /// </summary>
    public abstract class DomainEvent : IDomainEvent
    {
        protected DomainEvent(DateTime occurredUtc)
        {
            EventId = Guid.NewGuid();
            OccurredUtc = occurredUtc;
        }

        public Guid EventId { get; }

        public DateTime OccurredUtc { get; }

        public DateTime PublishedUtc { get; set; }

        /// <summary>Short name used in logs and metrics.</summary>
        public string EventName => GetType().Name;
    }
}
