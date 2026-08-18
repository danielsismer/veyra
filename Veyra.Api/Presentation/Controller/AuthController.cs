using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veyra.Api.Application.Security;
using Veyra.Api.Application.Service;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Presentation.Dto.Response;

namespace Veyra.Api.Presentation.Controller;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private const string InvalidCredentialsMessage = "E-mail ou senha invalidos.";
    private const string InvalidRefreshTokenMessage = "Refresh token invalido ou expirado.";

    private readonly AuthService _auth;
    private readonly UserService _users;

    public AuthController(AuthService auth, UserService users)
    {
        _auth = auth;
        _users = users;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public IActionResult Register(CreateUserRequest request)
    {
        var result = _auth.Register(request);

        if (!result.Succeeded)
        {
            return Problem(
                title: "E-mail ja cadastrado.",
                detail: "Ja existe um usuario com esse e-mail.",
                statusCode: StatusCodes.Status409Conflict);
        }

        return CreatedAtAction(
            actionName: nameof(UserController.FindById),
            controllerName: "User",
            routeValues: new { id = result.Value!.Id },
            value: result.Value);
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Login(LoginRequest request)
    {
        var result = _auth.Login(request);

        return result.Succeeded
            ? Ok(result.Value)
            : Problem(title: InvalidCredentialsMessage, statusCode: StatusCodes.Status401Unauthorized);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Refresh(RefreshRequest request)
    {
        var result = _auth.Refresh(request.RefreshToken);

        return result.Succeeded
            ? Ok(result.Value)
            : Problem(title: InvalidRefreshTokenMessage, statusCode: StatusCodes.Status401Unauthorized);
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout(RefreshRequest request)
    {
        _auth.Logout(CurrentUserId, request.RefreshToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Me()
    {
        var user = _users.FindById(CurrentUserId);

        return user is null ? NotFound() : Ok(user);
    }

    private int CurrentUserId =>
        int.Parse(User.FindFirstValue(ClaimNames.Subject) ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
