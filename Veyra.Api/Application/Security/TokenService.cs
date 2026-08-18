using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Veyra.Api.Domain.Entities;

namespace Veyra.Api.Application.Security;

public class TokenService
{
    private const int RefreshTokenBytes = 32;

    private readonly JwtSettings _settings;
    private readonly JsonWebTokenHandler _handler = new();

    public TokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public int AccessTokenSeconds => _settings.AccessTokenMinutes * 60;

    public string CreateAccessToken(User user)
    {
        var now = DateTimeOffset.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _settings.Issuer,
            Audience = _settings.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = now.AddMinutes(_settings.AccessTokenMinutes).UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id.ToString(),
                [JwtRegisteredClaimNames.Email] = user.Email,
                [JwtRegisteredClaimNames.Name] = user.Name,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString(),
                [ClaimNames.Role] = user.RoleName
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(_settings.KeyBytes),
                SecurityAlgorithms.HmacSha256)
        };

        return _handler.CreateToken(descriptor);
    }

    public RefreshToken CreateRefreshToken(int userId)
    {
        var now = DateTimeOffset.UtcNow;

        return new RefreshToken(
            token: Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes)),
            userId: userId,
            createdAt: now,
            expiresAt: now.AddDays(_settings.RefreshTokenDays));
    }
}
