using Veyra.Api.Domain.Entities;

namespace Veyra.Api.Domain.Repository;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);

    RefreshToken? Find(string token);

    IReadOnlyList<RefreshToken> FindByUser(int userId);

    int RevokeAllForUser(int userId);
}
