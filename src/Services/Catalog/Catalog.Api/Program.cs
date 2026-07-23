using Catalog.Api.Middleware;
using Catalog.Application.Categories;
using Catalog.Application.Products;
using Catalog.Infrastructure;
using Catalog.Infrastructure.Persistence;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCatalogInfrastructure(builder.Configuration);
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<CategoryService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "NexusCommerce Catalog API",
        Version = "v1",
        Description = "Product catalog service — part of the NexusCommerce distributed order-management platform."
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
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    await CatalogDbSeeder.SeedAsync(db);
}

app.UseHttpMetrics();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();

public partial class Program;
