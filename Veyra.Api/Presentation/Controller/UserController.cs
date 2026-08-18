using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Application.Service;

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

    [HttpPost]
    [AllowAnonymous]
    public IActionResult Create(CreateUserRequest request)
    {
        var user = _service.Create(request);
        return Ok(user);
    }

    [HttpGet]
    [Authorize]
    public IActionResult FindAll()
    {
        var users = _service.FindAll();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize]
    public IActionResult FindById(int id)
    {
        var user = _service.FindById(id);
        return Ok(user);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "ADMIN")]
    public IActionResult DeleteById(int id)
    {
        _service.DeleteById(id);
        return NoContent();
    }
}