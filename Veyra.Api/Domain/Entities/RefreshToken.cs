namespace Veyra.Api.Domain.Entities;

public class RefreshToken
{
    private readonly object _gate = new();

    public RefreshToken(string token, int userId, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Token = token;
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    public string Token { get; }
    public int UserId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? RevokedAt { private set; get; }
    public string? ReplacedByToken { private set; get; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    public bool TryRevoke(string? replacedByToken = null)
    {
        lock (_gate)
        {
            if (RevokedAt is not null)
            {
                return false;
            }

            RevokedAt = DateTimeOffset.UtcNow;
            ReplacedByToken = replacedByToken;
            return true;
        }
    }
}
