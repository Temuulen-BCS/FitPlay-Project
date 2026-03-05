using FitPlay.Api.Auth;
using FitPlay.Api.Services;
using FitPlay.Domain.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace FitPlay.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _config;
    private readonly FitPlayContext _fitDb;
    private readonly MembershipService _membershipService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        FitPlayContext fitDb,
        MembershipService membershipService)
    {
        _userManager = userManager;
        _config = config;
        _fitDb = fitDb;
        _membershipService = membershipService;
    }

    public record RegisterDto(string Email, string Password);
    public record LoginDto(string Email, string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        var user = new ApplicationUser { UserName = dto.Email, Email = dto.Email };
        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded) return BadRequest(result.Errors);

        return Ok(new { message = "Registered successfully" });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _userManager.FindByEmailAsync(dto.Email);
        if (user is null) return Unauthorized();

        var ok = await _userManager.CheckPasswordAsync(user, dto.Password);
        if (!ok) return Unauthorized();

        var token = await GenerateToken(user);
        return Ok(new { token });
    }

    private async Task<string> GenerateToken(ApplicationUser user)
    {
        var key = _config["Jwt:Key"]!;
        var issuer = _config["Jwt:Issuer"]!;
        var audience = _config["Jwt:Audience"]!;

        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? ""),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
        };

        var domainUser = await _fitDb.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdentityUserId == user.Id);
        if (domainUser is not null)
        {
            var active = await _membershipService.GetActiveSubscriptionAsync(domainUser.Id);
            if (active is not null)
            {
                claims.Add(new Claim("membership", "active"));
            }
        }

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var creds = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var jwt = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
