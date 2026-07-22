using System.Text.Json;

namespace Orders.Infrastructure.Messaging;

/// <summary>
/// Wire format shared with consumers (see the Notifications service):
/// camelCase JSON of { eventType, occurredAtUtc, data }.
/// </summary>
public record EventEnvelope(string EventType, DateTime OccurredAtUtc, object Data)
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    public static string Serialize(string eventType, object data) =>
        JsonSerializer.Serialize(
            new EventEnvelope(eventType, DateTime.UtcNow, data), SerializerOptions);
}
