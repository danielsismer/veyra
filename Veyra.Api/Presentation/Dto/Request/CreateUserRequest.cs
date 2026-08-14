namespace Veyra.Api.Presentation.Dto.Request;

public record CreateUserRequest
{
    public string Name { get; init; }
    public string Email { get; init; }
    public string Password { get; init; }
}