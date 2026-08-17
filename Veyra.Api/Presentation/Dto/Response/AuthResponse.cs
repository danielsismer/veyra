namespace Veyra.Api.Presentation.Dto.Response;

/// <param name="ExpiresIn">Validade do access token, em segundos.</param>
public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    UserResponse User,
    string TokenType = "Bearer");
