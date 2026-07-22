using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orders.Application.Abstractions;
using Orders.Infrastructure.Messaging;
using Orders.Infrastructure.Persistence;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the Orders persistence layer. The database provider is chosen via
    /// configuration ("Database:Provider": "PostgreSql" | "Sqlite") so the service runs
    /// against PostgreSQL in real environments and SQLite for lightweight local dev.
    /// </summary>
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Database:Provider"] ?? "PostgreSql";

        services.AddDbContext<OrdersDbContext>(options =>
        {
            switch (provider.ToLowerInvariant())
            {
                case "sqlite":
                    options.UseSqlite(
                        configuration.GetConnectionString("OrdersDb") ?? "Data Source=orders.db");
                    break;
                case "postgresql":
                    options.UseNpgsql(
                        configuration.GetConnectionString("OrdersDb")
                        ?? throw new InvalidOperationException(
                            "Connection string 'OrdersDb' is required when using the PostgreSql provider."));
                    break;
                default:
                    throw new InvalidOperationException($"Unknown database provider '{provider}'.");
            }
        });

        services.AddScoped<IOrdersDbContext>(sp => sp.GetRequiredService<OrdersDbContext>());

        AddEventPublisher(services, configuration);

        return services;
    }

    /// <summary>
    /// Registers the integration-event transport ("EventBus:Transport"):
    /// "RabbitMq" for broker-backed environments, "Http" to POST events directly
    /// to a consumer during local dev, "None" (default) to log and drop them.
    /// </summary>
    private static void AddEventPublisher(IServiceCollection services, IConfiguration configuration)
    {
        var transport = configuration["EventBus:Transport"] ?? "None";

        switch (transport.ToLowerInvariant())
        {
            case "http":
                var endpoint = configuration["EventBus:HttpEndpoint"]
                    ?? throw new InvalidOperationException(
                        "'EventBus:HttpEndpoint' is required when using the Http transport.");
                services.AddHttpClient<HttpEventPublisher>(client =>
                {
                    client.BaseAddress = new Uri(endpoint);
                    client.Timeout = TimeSpan.FromSeconds(5);
                });
                services.AddScoped<IEventPublisher>(sp => sp.GetRequiredService<HttpEventPublisher>());
                break;

            case "rabbitmq":
                services.AddSingleton<IEventPublisher>(sp => new RabbitMqEventPublisher(
                    configuration["EventBus:AmqpUri"] ?? "amqp://guest:guest@localhost:5672/",
                    configuration["EventBus:Exchange"] ?? "nexus.events",
                    sp.GetRequiredService<ILogger<RabbitMqEventPublisher>>()));
                break;

            case "none":
                services.AddSingleton<IEventPublisher, NullEventPublisher>();
                break;

            default:
                throw new InvalidOperationException($"Unknown event transport '{transport}'.");
        }
    }
}
