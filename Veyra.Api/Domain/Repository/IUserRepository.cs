using Veyra.Api.Domain.Entities;

namespace Veyra.Api.Domain.Repository;

public interface IUserRepository
{
    User Add(User user);

    IReadOnlyList<User> FindAll();

    User? FindById(int id);

    User? FindByEmail(string email);

    bool ExistsByEmail(string email);

    bool Remove(int id);
}
