using Veyra.Api.Domain.Entities;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Presentation.Dto.Response;

namespace Veyra.Api.Application.Mapper;

public class UserMapper
{
    
    public User ToEntity(CreateUserRequest request)
    {
        return new User(
            request.Name,
            request.Email,
            request.Password
        );
    }

    public UserResponse ToResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.Name,
            user.Email
        );
    }

}