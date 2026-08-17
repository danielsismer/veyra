namespace Veyra.Api.Application.Security;

public enum AuthError
{
    None = 0,
    EmailAlreadyUsed,
    InvalidCredentials,
    InvalidRefreshToken
}

public sealed class AuthResult<T>
{
    private AuthResult(T? value, AuthError error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }
    public AuthError Error { get; }
    public bool Succeeded => Error == AuthError.None;

    public static AuthResult<T> Ok(T value) => new(value, AuthError.None);

    public static AuthResult<T> Fail(AuthError error) => new(default, error);
}
