using System.Collections.Concurrent;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Domain.Repository;

namespace Veyra.Api.Infrastructure.Repository;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<int, User> _users = new();
    private int _lastId;

    public User Add(User user)
    {
        user.AssignId(Interlocked.Increment(ref _lastId));
        _users[user.Id] = user;
        return user;
    }

    public IReadOnlyList<User> FindAll() => _users.Values.OrderBy(u => u.Id).ToList();

    public User? FindById(int id) => _users.TryGetValue(id, out var user) ? user : null;

    public User? FindByEmail(string email) =>
        _users.Values.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));

    public bool ExistsByEmail(string email) => FindByEmail(email) is not null;

    public bool Remove(int id) => _users.TryRemove(id, out _);
}
