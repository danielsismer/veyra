using Veyra.Api.Domain.Enums;

namespace Veyra.Api.Domain.Entities;

public class User {
    public User(string name, string email, string password)
    {
        Name = name;
        Email = email;
        Password = password;
    }

    public int Id { private set; get; }
    public string? Name { private set; get; }
    public string? Email { private set; get; }
    public string? Password { private set; get; }
    public UserEnum Role { private set; get; }

}
