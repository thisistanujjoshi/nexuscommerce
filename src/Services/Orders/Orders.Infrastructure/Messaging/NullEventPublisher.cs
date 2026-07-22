using Microsoft.Extensions.Logging;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Messaging;

/// <summary>Used when no event transport is configured; events are logged and dropped.</summary>
public class NullEventPublisher(ILogger<NullEventPublisher> logger) : IEventPublisher
{
    public Task PublishAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("Event {EventType} not published (transport: None)", eventType);
        return Task.CompletedTask;
    }
}
