using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Veyra.Api.Application.Mapper;
using Veyra.Api.Application.Security;
using Veyra.Api.Application.Service;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Infrastructure.Repository;
using Veyra.Api.Presentation.Dto.Request;

namespace Veyra.Api.Tests;

/// <summary>
/// Monta AuthService sobre os repositorios em memoria, sem host HTTP.
/// </summary>
internal sealed class AuthTestContext
{
    public AuthTestContext(int accessTokenMinutes = 15, int refreshTokenDays = 7)
    {
        Settings = new JwtSettings
        {
            Issuer = "veyra-test",
            Audience = "veyra-test-client",
            Key = "chave-de-teste-com-mais-de-32-bytes-para-hmac-sha256",
            AccessTokenMinutes = accessTokenMinutes,
            RefreshTokenDays = refreshTokenDays
        };

        Users = new InMemoryUserRepository();
        RefreshTokens = new InMemoryRefreshTokenRepository();
        Tokens = new TokenService(Options.Create(Settings));
        PasswordHasher = new PasswordHasher<User>();

        Auth = new AuthService(
            Users,
            RefreshTokens,
            Tokens,
            new UserMapper(),
            PasswordHasher,
            NullLogger<AuthService>.Instance);
    }

    public JwtSettings Settings { get; }
    public InMemoryUserRepository Users { get; }
    public InMemoryRefreshTokenRepository RefreshTokens { get; }
    public TokenService Tokens { get; }
    public IPasswordHasher<User> PasswordHasher { get; }
    public AuthService Auth { get; }

    public const string DefaultPassword = "senha@12345";

    public static CreateUserRequest Request(string email = "user@veyra.local", string password = DefaultPassword) =>
        new() { Name = "Usuario Teste", Email = email, Password = password };
}
