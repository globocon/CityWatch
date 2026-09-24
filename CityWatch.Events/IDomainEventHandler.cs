using System.Threading;
using System.Threading.Tasks;

namespace CityWatch.Events
{
    /// <summary>
    /// Implemented by feature packs that want to observe platform events.
    /// Register with <c>services.AddDomainEventHandler&lt;TEvent, THandler&gt;()</c>.
    /// </summary>
    /// <remarks>
    /// A handler may throw: the dispatcher catches, logs and moves on, so a fault in one
    /// subscriber cannot reach the publisher or any other subscriber. Handlers should still be
    /// idempotent — <see cref="IDomainEvent.EventId"/> exists for that — because a future durable
    /// transport would change delivery from at-most-once to at-least-once without changing
    /// this interface.
    /// </remarks>
    public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
    {
        Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken);
    }
}
