using Veyra.Api.Application.Mapper;
using Veyra.Api.Domain.Repository;
using Veyra.Api.Presentation.Dto.Response;

namespace Veyra.Api.Application.Service;

public class UserService
{
    private readonly IUserRepository _users;
    private readonly UserMapper _mapper;

    public UserService(IUserRepository users, UserMapper mapper)
    {
        _users = users;
        _mapper = mapper;
    }

    public List<UserResponse> FindAll()
    {
        return _users.FindAll().Select(_mapper.ToResponse).ToList();
    }

    public UserResponse? FindById(int id)
    {
        var user = _users.FindById(id);
        return user is null ? null : _mapper.ToResponse(user);
    }

    public bool DeleteById(int id)
    {
        return _users.Remove(id);
    }
}
