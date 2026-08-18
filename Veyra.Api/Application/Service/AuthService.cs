using Microsoft.AspNetCore.Identity;
using Veyra.Api.Application.Mapper;
using Veyra.Api.Application.Security;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Domain.Enums;
using Veyra.Api.Domain.Repository;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Presentation.Dto.Response;

namespace Veyra.Api.Application.Service;

public class AuthService
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly TokenService _tokens;
    private readonly UserMapper _mapper;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ILogger<AuthService> _logger;

    private readonly User _decoyUser;
    private readonly string _decoyHash;

    public AuthService(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        TokenService tokens,
        UserMapper mapper,
        IPasswordHasher<User> passwordHasher,
        ILogger<AuthService> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
        _logger = logger;

        _decoyUser = new User("decoy", "decoy@veyra.local", string.Empty);
        _decoyHash = _passwordHasher.HashPassword(_decoyUser, Guid.NewGuid().ToString());
    }

    public AuthResult<UserResponse> Register(CreateUserRequest request)
    {
        if (_users.ExistsByEmail(request.Email))
        {
            return AuthResult<UserResponse>.Fail(AuthError.EmailAlreadyUsed);
        }

        var user = CreateUser(request, UserEnum.Client);

        return AuthResult<UserResponse>.Ok(_mapper.ToResponse(user));
    }

    public User CreateUser(CreateUserRequest request, UserEnum role)
    {
        var user = _mapper.ToEntity(request, passwordHash: string.Empty, role);
        user.ChangePasswordHash(_passwordHasher.HashPassword(user, request.Password));

        return _users.Add(user);
    }

    public AuthResult<AuthResponse> Login(LoginRequest request)
    {
        var user = _users.FindByEmail(request.Email);

        if (user is null)
        {
            _passwordHasher.VerifyHashedPassword(_decoyUser, _decoyHash, request.Password);
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidCredentials);
        }

        var verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verification == PasswordVerificationResult.Failed)
        {
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidCredentials);
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(_passwordHasher.HashPassword(user, request.Password));
        }

        return AuthResult<AuthResponse>.Ok(IssueTokens(user));
    }

    public AuthResult<AuthResponse> Refresh(string refreshToken)
    {
        var stored = _refreshTokens.Find(refreshToken);

        if (stored is null)
        {
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidRefreshToken);
        }

        if (stored.IsExpired)
        {
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidRefreshToken);
        }

        var user = _users.FindById(stored.UserId);

        if (user is null)
        {
            _refreshTokens.RevokeAllForUser(stored.UserId);
            return AuthResult<AuthResponse>.Fail(AuthError.InvalidRefreshToken);
        }

        var rotated = _tokens.CreateRefreshToken(user.Id);

        if (!stored.TryRevoke(replacedByToken: rotated.Token))
        {
            var revoked = _refreshTokens.RevokeAllForUser(stored.UserId);
            _logger.LogWarning(
                "Reuso de refresh token detectado para o usuario {UserId}. {Revoked} token(s) ativo(s) revogado(s).",
                stored.UserId, revoked);

            return AuthResult<AuthResponse>.Fail(AuthError.InvalidRefreshToken);
        }

        _refreshTokens.Add(rotated);

        return AuthResult<AuthResponse>.Ok(BuildResponse(user, rotated));
    }

    public void Logout(int userId, string refreshToken)
    {
        var stored = _refreshTokens.Find(refreshToken);

        if (stored is null || stored.UserId != userId)
        {
            return;
        }

        stored.TryRevoke();
    }

    public AuthResponse IssueTokens(User user)
    {
        var refreshToken = _tokens.CreateRefreshToken(user.Id);
        _refreshTokens.Add(refreshToken);

        return BuildResponse(user, refreshToken);
    }

    private AuthResponse BuildResponse(User user, RefreshToken refreshToken) => new(
        AccessToken: _tokens.CreateAccessToken(user),
        RefreshToken: refreshToken.Token,
        ExpiresIn: _tokens.AccessTokenSeconds,
        User: _mapper.ToResponse(user));
}
