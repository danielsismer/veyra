namespace Veyra.Api.Presentation.Dto.Response;

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserResponse User,
    string TokenType = "Bearer");
