using Veyra.Api.Domain.Entities;

namespace Veyra.Api.Domain.Repository;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);

    RefreshToken? Find(string token);

    IReadOnlyList<RefreshToken> FindByUser(int userId);

    /// <summary>Revoga todos os tokens ainda ativos do usuario. Retorna quantos foram revogados.</summary>
    int RevokeAllForUser(int userId);
}
