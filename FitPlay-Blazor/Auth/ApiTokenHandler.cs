using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitPlay_Blazor.Auth;

public class ApiTokenHandler
{
    private readonly IConfiguration _configuration;

    public ApiTokenHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(ClaimsPrincipal principal)
    {
        var identityId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(identityId))
        {
            throw new InvalidOperationException("Missing identity user id.");
        }

        var key = (_configuration["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key not configured.")).Trim();
        var issuer = (_configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured.")).Trim();
        var audience = (_configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured.")).Trim();

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, identityId),
            new Claim(ClaimTypes.NameIdentifier, identityId),
        };

        var email = principal.FindFirstValue(ClaimTypes.Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, email));
        }

        var membership = principal.FindFirstValue("membership");
        if (!string.IsNullOrWhiteSpace(membership))
        {
            claims.Add(new Claim("membership", membership));
        }

        var roles = principal.Claims
            .Where(c => c.Type == ClaimTypes.Role || c.Type == "role")
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
