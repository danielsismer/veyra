using System.Collections.Concurrent;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Domain.Repository;

namespace Veyra.Api.Infrastructure.Repository;

/// <summary>
/// Store em memoria registrado como singleton. Ao migrar para EF Core, guardar o SHA-256
/// do token em vez do valor em claro.
/// </summary>
public class InMemoryRefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new(StringComparer.Ordinal);

    public void Add(RefreshToken token) => _tokens[token.Token] = token;

    public RefreshToken? Find(string token) => _tokens.TryGetValue(token, out var found) ? found : null;

    public IReadOnlyList<RefreshToken> FindByUser(int userId) =>
        _tokens.Values.Where(t => t.UserId == userId).ToList();

    public int RevokeAllForUser(int userId)
    {
        var revoked = 0;

        foreach (var token in _tokens.Values.Where(t => t.UserId == userId))
        {
            if (token.TryRevoke())
            {
                revoked++;
            }
        }

        return revoked;
    }
}
