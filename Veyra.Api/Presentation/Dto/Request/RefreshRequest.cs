using System.ComponentModel.DataAnnotations;

namespace Veyra.Api.Presentation.Dto.Request;

public record RefreshRequest
{
    [Required]
    public required string RefreshToken { get; init; }
}
