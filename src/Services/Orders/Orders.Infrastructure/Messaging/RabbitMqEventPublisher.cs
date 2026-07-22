using System.Text;
using Microsoft.Extensions.Logging;
using Orders.Application.Abstractions;
using RabbitMQ.Client;

namespace Orders.Infrastructure.Messaging;

/// <summary>
/// Publishes events to the durable topic exchange "nexus.events" with the
/// event type as routing key. Selected with "EventBus:Transport": "RabbitMq".
/// The connection is created lazily and reused for the process lifetime.
/// </summary>
public sealed class RabbitMqEventPublisher(
    string amqpUri,
    string exchange,
    ILogger<RabbitMqEventPublisher> logger) : IEventPublisher, IAsyncDisposable
{
    private readonly SemaphoreSlim _connectLock = new(1, 1);
    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        try
        {
            var channel = await GetChannelAsync(cancellationToken);
            var body = Encoding.UTF8.GetBytes(EventEnvelope.Serialize(eventType, data));

            await channel.BasicPublishAsync(
                exchange: exchange,
                routingKey: eventType,
                mandatory: false,
                basicProperties: new BasicProperties
                {
                    ContentType = "application/json",
                    DeliveryMode = DeliveryModes.Persistent
                },
                body: body,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Failed to publish event {EventType}; continuing without it", eventType);
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        await _connectLock.WaitAsync(cancellationToken);
        try
        {
            if (_channel is { IsOpen: true })
                return _channel;

            var factory = new ConnectionFactory { Uri = new Uri(amqpUri) };
            _connection = await factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

            await _channel.ExchangeDeclareAsync(
                exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            return _channel;
        }
        finally
        {
            _connectLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.DisposeAsync();
        if (_connection is not null)
            await _connection.DisposeAsync();
        _connectLock.Dispose();
    }
}
