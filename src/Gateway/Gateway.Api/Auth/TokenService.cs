using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Gateway.Api.Auth;

public record DemoUser(string Username, string Password, string Role);

public record TokenResponse(string AccessToken, string TokenType, int ExpiresInSeconds);

/// <summary>
/// Issues demo JWTs for the portfolio deployment. A real system would delegate
/// to an identity provider (Entra ID, Auth0, Keycloak); the gateway's validation
/// pipeline would stay exactly the same.
/// </summary>
public class TokenService(IConfiguration configuration)
{
    public TokenResponse? IssueToken(string username, string password)
    {
        var user = configuration.GetSection("Auth:Users").Get<List<DemoUser>>()
            ?.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase)
                && u.Password == password);

        if (user is null)
            return null;

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Auth:SigningKey"]
                ?? throw new InvalidOperationException("'Auth:SigningKey' is required.")));

        var lifetime = TimeSpan.FromHours(1);
        var token = new JwtSecurityToken(
            issuer: configuration["Auth:Issuer"],
            audience: configuration["Auth:Audience"],
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, user.Username),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role),
            ],
            expires: DateTime.UtcNow.Add(lifetime),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new TokenResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            "Bearer",
            (int)lifetime.TotalSeconds);
    }
}
