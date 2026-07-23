using System.Text.Json.Serialization;
using Orders.Api.Middleware;
using Orders.Application.Orders.Commands;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOrdersInfrastructure(builder.Configuration);
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(PlaceOrderCommand).Assembly));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "NexusCommerce Orders API",
        Version = "v1",
        Description = "Order lifecycle service (CQRS) — part of the NexusCommerce distributed order-management platform."
    });
});

builder.Services.AddHealthChecks();

// Dev-time CORS for the storefront/admin dev servers (any localhost port);
// in production the API gateway fronts all services on a single origin.
builder.Services.AddCors(options => options.AddPolicy("Frontends", policy => policy
    .SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors("Frontends");

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.UseHttpMetrics();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program;
