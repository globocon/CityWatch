namespace CityWatch.Events
{
    /// <summary>
    /// The default publisher. Does nothing.
    /// </summary>
    /// <remarks>
    /// This type is why publish sites can be added to stable production code without changing its
    /// behaviour. When no feature pack has subscribed, this is what gets injected, and
    /// <see cref="Publish"/> is an empty method the JIT can elide entirely.
    ///
    /// It is registered by <c>AddDomainEvents</c> via TryAddSingleton, so the real publisher
    /// always wins if one has been registered first, and this is what remains otherwise.
    /// </remarks>
    public sealed class NullDomainEventPublisher : IDomainEventPublisher
    {
        public static readonly NullDomainEventPublisher Instance = new();

        public void Publish(IDomainEvent domainEvent)
        {
            // Intentionally empty. See remarks.
        }
    }
}
