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

    /// <summary>Atribuido pelo repositorio no momento da persistencia.</summary>
    public void AssignId(int id) => Id = id;

    public void ChangePasswordHash(string passwordHash) => PasswordHash = passwordHash;

    /// <summary>
    /// Nome da role como aparece na claim e nos atributos [Authorize(Roles = "...")].
    /// </summary>
    public string RoleName => Role.ToString().ToUpperInvariant();
}
