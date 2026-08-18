using Veyra.Api.Domain.Enums;

namespace Veyra.Api.Domain.Entities;

public class User
{
    public User(string name, string email, string passwordHash, UserEnum role = UserEnum.Client)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    public int Id { private set; get; }
    public string Name { private set; get; }
    public string Email { private set; get; }
    public string PasswordHash { private set; get; }
    public UserEnum Role { private set; get; }

    public string RoleName => Role.ToString().ToUpperInvariant();

    public void AssignId(int id) => Id = id;

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;
}
