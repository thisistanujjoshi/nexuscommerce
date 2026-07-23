using System.Text;
using Gateway.Api.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false; // TLS terminates ahead of the gateway in real deployments
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Auth:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["Auth:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Auth:SigningKey"]
                    ?? throw new InvalidOperationException("'Auth:SigningKey' is required."))),
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("authenticated", policy => policy.RequireAuthenticatedUser());
    options.AddPolicy("admin", policy => policy.RequireRole("admin"));
});

builder.Services.AddSingleton<TokenService>();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

builder.Services.AddHealthChecks();

// Dev-time CORS for the storefront/admin dev servers (any localhost port);
// in production the gateway is the single public origin.
builder.Services.AddCors(options => options.AddPolicy("Frontends", policy => policy
    .SetIsOriginAllowed(origin => new Uri(origin).IsLoopback)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

app.UseCors("Frontends");
app.UseHttpMetrics();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/auth/token", (TokenRequest request, TokenService tokens) =>
{
    var token = tokens.IssueToken(request.Username, request.Password);
    return token is null
        ? Results.Unauthorized()
        : Results.Ok(token);
});

app.MapHealthChecks("/health");
app.MapMetrics();
app.MapReverseProxy();

app.Run();

public record TokenRequest(string Username, string Password);

public partial class Program;
