using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CityWatch.Events
{
    /// <summary>
    /// Background pump that drains the event channel and invokes subscribers.
    /// </summary>
    /// <remarks>
    /// Every handler invocation is individually isolated: its own DI scope, its own timeout, its own
    /// try/catch. One subscriber throwing, hanging or leaking cannot affect another subscriber, and
    /// cannot reach the publisher at all — by the time an event is here, the workflow that raised it
    /// has already returned.
    /// </remarks>
    public sealed class DomainEventDispatcher : BackgroundService
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> HandleMethodCache = new();

        private readonly ChannelReader<IDomainEvent> _reader;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DomainEventBusOptions _options;
        private readonly DomainEventBusMetrics _metrics;
        private readonly ILogger<DomainEventDispatcher> _logger;

        public DomainEventDispatcher(
            ChannelReader<IDomainEvent> reader,
            IServiceScopeFactory scopeFactory,
            DomainEventBusOptions options,
            DomainEventBusMetrics metrics,
            ILogger<DomainEventDispatcher> logger)
        {
            _reader = reader;
            _scopeFactory = scopeFactory;
            _options = options;
            _metrics = metrics;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Domain event dispatcher started (capacity {Capacity}).", _options.Capacity);

            try
            {
                await foreach (var domainEvent in _reader.ReadAllAsync(stoppingToken))
                {
                    await DispatchAsync(domainEvent, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                /* The pump itself must not die silently: if it does, every subscriber goes deaf
                   while the application carries on looking healthy. */
                _logger.LogCritical(ex, "Domain event dispatcher terminated unexpectedly. Subscribers are no longer receiving events.");
            }

            _logger.LogInformation(
                "Domain event dispatcher stopped. Published={Published} Handled={Handled} Dropped={Dropped} Failures={Failures} Timeouts={Timeouts}",
                _metrics.Published, _metrics.Handled, _metrics.Dropped, _metrics.HandlerFailures, _metrics.HandlerTimeouts);
        }

        internal async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken stoppingToken)
        {
            var eventType = domainEvent.GetType();

            // A scope per event: handlers may take scoped dependencies such as a DbContext.
            using var scope = _scopeFactory.CreateScope();

            IReadOnlyList<object> handlers;
            try
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
                handlers = scope.ServiceProvider.GetServices(handlerType).Where(h => h != null).ToList()!;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not resolve handlers for {EventType}.", eventType.Name);
                return;
            }

            if (handlers.Count == 0)
                return;

            var handleMethod = HandleMethodCache.GetOrAdd(eventType, static t =>
                typeof(IDomainEventHandler<>).MakeGenericType(t).GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!);

            foreach (var handler in handlers)
            {
                await InvokeHandlerAsync(handler, handleMethod, domainEvent, eventType, stoppingToken);
            }
        }

        private async Task InvokeHandlerAsync(
            object handler, MethodInfo handleMethod, IDomainEvent domainEvent, Type eventType, CancellationToken stoppingToken)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeoutSource.CancelAfter(TimeSpan.FromSeconds(_options.HandlerTimeoutSeconds));

            try
            {
                Task task;
                try
                {
                    task = (Task)handleMethod.Invoke(handler, new object[] { domainEvent, timeoutSource.Token })!;
                }
                catch (TargetInvocationException tie) when (tie.InnerException != null)
                {
                    /* A handler that is not declared async can throw before returning a Task, and
                       reflection wraps that. Unwrap so the catches below see the real exception
                       rather than always falling through to the generic one. */
                    ExceptionDispatchInfo.Capture(tie.InnerException).Throw();
                    throw; // unreachable; keeps the compiler happy about definite assignment
                }

                await task;
                _metrics.RecordHandled();
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                _metrics.RecordHandlerTimeout();
                _logger.LogWarning("Handler {Handler} timed out after {Seconds}s handling {EventType} ({EventId}).",
                    handler.GetType().Name, _options.HandlerTimeoutSeconds, eventType.Name, domainEvent.EventId);
            }
            catch (OperationCanceledException)
            {
                // Shutting down; not a handler fault.
            }
            catch (Exception ex)
            {
                /* Swallow and continue. A feature pack failing is a feature pack problem — the
                   platform workflow that raised this event completed successfully long ago. */
                _metrics.RecordHandlerFailure();
                _logger.LogError(ex, "Handler {Handler} failed handling {EventType} ({EventId}).",
                    handler.GetType().Name, eventType.Name, domainEvent.EventId);
            }
        }
    }
}
