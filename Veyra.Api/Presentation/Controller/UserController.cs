using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veyra.Api.Application.Service;
using Veyra.Api.Presentation.Dto.Response;

namespace Veyra.Api.Presentation.Controller;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly UserService _service;

    public UserController(UserService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType<List<UserResponse>>(StatusCodes.Status200OK)]
    public IActionResult FindAll()
    {
        var users = _service.FindAll();
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult FindById(int id)
    {
        var user = _service.FindById(id);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "ADMIN")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteById(int id)
    {
        return _service.DeleteById(id) ? NoContent() : NotFound();
    }
}
