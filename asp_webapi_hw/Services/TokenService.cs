using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using asp_webapi_hw.DTO;
using asp_webapi_hw.Models;
using Microsoft.IdentityModel.Tokens;

namespace asp_webapi_hw.Services;

public class TokenService : ITokenService
{
    // ── Конфігурація ─────────────────────────────────────────────────────────
    private readonly string _jwtSecret;
    private readonly string _jwtIssuer;
    private readonly string _jwtAudience;
    private readonly int    _accessTokenLifetimeMinutes;
    private readonly int    _refreshTokenLifetimeDays;

    private static readonly List<RefreshToken> _refreshTokens = new();
    private static readonly Lock _lock = new();

    public TokenService(IConfiguration config)
    {
        _jwtSecret                  = config["Jwt:Secret"]
                                      ?? throw new InvalidOperationException("Jwt:Secret не налаштовано.");
        _jwtIssuer                  = config["Jwt:Issuer"]   ?? "homework_project";
        _jwtAudience                = config["Jwt:Audience"] ?? "homework_project";
        _accessTokenLifetimeMinutes = int.Parse(config["Jwt:AccessTokenLifetimeMinutes"]  ?? "15");
        _refreshTokenLifetimeDays   = int.Parse(config["Jwt:RefreshTokenLifetimeDays"]    ?? "7");
    }

    // ── GenerateTokenPair ─────────────────────────────────────────────────────
    public TokenResponseDto GenerateTokenPair(User user)
    {
        var accessToken  = CreateAccessToken(user);
        var refreshToken = CreateRefreshToken(user.Id);

        return new TokenResponseDto
        {
            AccessToken  = accessToken,
            RefreshToken = refreshToken.Token,
            ExpiresIn    = _accessTokenLifetimeMinutes * 60,
        };
    }

    // ── RefreshTokenPair ──────────────────────────────────────────────────────
    public TokenResponseDto? RefreshTokenPair(string refreshToken)
    {
        lock (_lock)
        {
            var existing = _refreshTokens
                .FirstOrDefault(t => t.Token == refreshToken);

            if (existing is null || !existing.IsActive)
                return null;

            existing.IsRevoked = true;

            var user = UserRepository.GetById(existing.UserId);
            if (user is null)
                return null;

            var newAccessToken  = CreateAccessToken(user);
            var newRefreshToken = CreateRefreshToken(user.Id);

            return new TokenResponseDto
            {
                AccessToken  = newAccessToken,
                RefreshToken = newRefreshToken.Token,
                ExpiresIn    = _accessTokenLifetimeMinutes * 60,
            };
        }
    }

    // ── RevokeToken ───────────────────────────────────────────────────────────
    public bool RevokeToken(string refreshToken)
    {
        lock (_lock)
        {
            var token = _refreshTokens
                .FirstOrDefault(t => t.Token == refreshToken && t.IsActive);

            if (token is null)
                return false;

            token.IsRevoked = true;
            return true;
        }
    }

    // ── RevokeAllUserTokens ───────────────────────────────────────────────────
    public void RevokeAllUserTokens(int userId)
    {
        lock (_lock)
        {
            _refreshTokens
                .Where(t => t.UserId == userId && t.IsActive)
                .ToList()
                .ForEach(t => t.IsRevoked = true);
        }
    }

    // ── ValidateAccessToken ───────────────────────────────────────────────────
    public int? ValidateAccessToken(string accessToken)
    {
        try
        {
            var key       = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var handler   = new JwtSecurityTokenHandler();

            handler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = key,
                ValidateIssuer           = true,
                ValidIssuer              = _jwtIssuer,
                ValidateAudience         = true,
                ValidAudience            = _jwtAudience,
                ValidateLifetime         = true,
                ClockSkew                = TimeSpan.Zero
            }, out var validatedToken);

            var jwt    = (JwtSecurityToken)validatedToken;
            var userId = int.Parse(
                jwt.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

            return userId;
        }
        catch
        {
            return null;
        }
    }

    private string CreateAccessToken(User user)
    {
        var key         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,           user.Email),
            new Claim(ClaimTypes.Name,            user.UserName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer:             _jwtIssuer,
            audience:           _jwtAudience,
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(_accessTokenLifetimeMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private RefreshToken CreateRefreshToken(int userId)
    {
        var token = new RefreshToken
        {
            Token     = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId    = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenLifetimeDays),
        };

        lock (_lock)
            _refreshTokens.Add(token);

        return token;
    }
}
