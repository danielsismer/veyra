using Veyra.Api.Domain.Entities;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Presentation.Dto.Response;
using Veyra.Api.Application.Mapper;

namespace Veyra.Api.Application.Service;

public class UserService
{
    private  List<User> _users = new();
    private readonly UserMapper _mapper = new();

    public UserService(UserMapper mapper, List<User> users)
    {
        _mapper = mapper;
        _users = users;
    }

    public UserResponse Create(CreateUserRequest request)
    {

        var user = _mapper.ToEntity(request);
        _users.Add(user);

        return _mapper.ToResponse(user);
    }

    public List<UserResponse>? FindAll()
    {
        return _users.Select(_mapper.ToResponse).ToList();
    }

    public UserResponse? FindById(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return user is null ? null : _mapper.ToResponse(user);
    }

    public void DeleteById(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);

    }


}