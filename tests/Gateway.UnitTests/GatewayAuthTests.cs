using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Gateway.UnitTests;

/// <summary>
/// Auth behavior tests against the real gateway pipeline. 401/403 mean the
/// gateway blocked the request; anything else means authorization passed and
/// the request reached the proxy (502 when the backend is down, 2xx/4xx from
/// the service when it happens to be running locally).
/// </summary>
public class GatewayAuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public GatewayAuthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    private static void AssertPassedAuth(HttpResponseMessage response)
    {
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<string> GetTokenAsync(string username, string password)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/auth/token", new { username, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<TokenBody>();
        return body!.AccessToken;
    }

    private record TokenBody(string AccessToken, string TokenType, int ExpiresInSeconds);

    [Fact]
    public async Task TokenEndpoint_WithValidAdminCredentials_IssuesTokenWithAdminRole()
    {
        var token = await GetTokenAsync("admin", "admin123");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("nexuscommerce-gateway", jwt.Issuer);
        Assert.Contains(jwt.Claims, c => c.Type is "role" or System.Security.Claims.ClaimTypes.Role
            && c.Value == "admin");
    }

    [Fact]
    public async Task TokenEndpoint_WithWrongPassword_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/auth/token",
            new { username = "admin", password = "wrong" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithoutToken_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/orders/api/v1/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Orders_WithCustomerToken_PassesAuthAndReachesProxy()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetTokenAsync("demo", "demo123"));

        var response = await client.GetAsync("/orders/api/v1/orders");

        AssertPassedAuth(response);
    }

    [Fact]
    public async Task CatalogRead_Anonymous_PassesAuthAndReachesProxy()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/catalog/api/v1/products");

        AssertPassedAuth(response);
    }

    [Fact]
    public async Task CatalogWrite_WithCustomerToken_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetTokenAsync("demo", "demo123"));

        var response = await client.PostAsJsonAsync("/catalog/api/v1/products", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CatalogWrite_WithAdminToken_PassesAuthAndReachesProxy()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetTokenAsync("admin", "admin123"));

        var response = await client.PostAsJsonAsync("/catalog/api/v1/products", new { });

        AssertPassedAuth(response);
    }

    [Fact]
    public async Task Notifications_WithCustomerToken_Returns403()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await GetTokenAsync("demo", "demo123"));

        var response = await client.GetAsync("/notifications/api/v1/notifications");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Ai_Anonymous_PassesAuthAndReachesProxy()
    {
        using var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/ai/api/v1/chat", new { message = "hi" });

        AssertPassedAuth(response);
    }

    [Fact]
    public async Task Orders_WithGarbageToken_Returns401()
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/orders/api/v1/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
