using System.ComponentModel.DataAnnotations;

namespace Veyra.Api.Presentation.Dto.Request;

public record LoginRequest
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    [Required]
    public required string Password { get; init; }
}
