namespace CityWatch.Events
{
    /// <summary>
    /// The one method existing production code is allowed to call.
    /// </summary>
    /// <remarks>
    /// The contract is deliberately narrow and deliberately unhelpful about delivery:
    /// <list type="bullet">
    /// <item><description><b>It never throws.</b> A publish site sits at the end of a committed
    /// workflow (an NFC scan, a patrol start). Nothing that happens on the bus may fail that
    /// workflow, so every failure mode is swallowed inside the implementation.</description></item>
    /// <item><description><b>It never blocks.</b> Publish enqueues and returns. Handlers run on a
    /// background pump, so publisher latency is a channel write.</description></item>
    /// <item><description><b>It promises nothing about delivery.</b> Delivery is at-most-once and
    /// in-process. Anything a customer paid for keeps its existing direct path; the event is an
    /// additional observer, never the mechanism.</description></item>
    /// </list>
    /// When no subscriber is registered the resolved implementation is
    /// <see cref="NullDomainEventPublisher"/> and this call does nothing at all.
    /// </remarks>
    public interface IDomainEventPublisher
    {
        /// <summary>
        /// Hands an event to the bus. Returns immediately. Never throws.
        /// </summary>
        void Publish(IDomainEvent domainEvent);
    }
}
