using System.Text;
using Microsoft.Extensions.Logging;
using Orders.Application.Abstractions;

namespace Orders.Infrastructure.Messaging;

/// <summary>
/// Dev-mode transport: POSTs the event envelope straight to a consumer's HTTP
/// ingress (the Notifications service) so the event flow works without a broker.
/// Selected with "EventBus:Transport": "Http".
/// </summary>
public class HttpEventPublisher(
    HttpClient httpClient,
    ILogger<HttpEventPublisher> logger) : IEventPublisher
{
    public async Task PublishAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var json = EventEnvelope.Serialize(eventType, data);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync((Uri?)null, content, cancellationToken);

            if (!response.IsSuccessStatusCode)
                logger.LogWarning(
                    "Event {EventType} rejected by consumer with {Status}", eventType, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to publish event {EventType}; continuing without it", eventType);
        }
    }
}
