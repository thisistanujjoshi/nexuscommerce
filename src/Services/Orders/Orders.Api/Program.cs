using System.Text.Json.Serialization;
using Orders.Api.Middleware;
using Orders.Application.Orders.Commands;
using Orders.Infrastructure;
using Orders.Infrastructure.Persistence;

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

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program;
