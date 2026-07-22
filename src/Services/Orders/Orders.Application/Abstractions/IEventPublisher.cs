namespace Orders.Application.Abstractions;

/// <summary>
/// Publishes integration events for other services to consume.
/// Implementations are fire-and-forget from the caller's perspective: a
/// transport failure is logged, never allowed to fail the business operation
/// (at-most-once delivery; an outbox would upgrade this — see ADR 0003).
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync(string eventType, object data, CancellationToken cancellationToken = default);
}
