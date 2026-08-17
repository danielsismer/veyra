namespace Veyra.Api.Domain.Entities;

public class RefreshToken
{
    public RefreshToken(string token, int userId, DateTimeOffset createdAt, DateTimeOffset expiresAt)
    {
        Token = token;
        UserId = userId;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    private readonly object _gate = new();

    public string Token { get; }
    public int UserId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset ExpiresAt { get; }
    public DateTimeOffset? RevokedAt { private set; get; }
    public string? ReplacedByToken { private set; get; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsRevoked && !IsExpired;

    /// <summary>
    /// Consome o token de forma atomica. Retorna <c>false</c> se ele ja tinha sido consumido —
    /// e esse <c>false</c> que denuncia o reuso.
    ///
    /// Precisa ser compare-and-set, e nao "checar depois revogar": dois refreshes simultaneos
    /// com o mesmo token passariam os dois pela checagem e ambos emitiriam sessao nova.
    /// Equivale ao UPDATE ... WHERE RevokedAt IS NULL conferindo o numero de linhas afetadas.
    /// </summary>
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
