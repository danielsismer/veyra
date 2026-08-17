using Veyra.Api.Domain.Entities;
using Veyra.Api.Domain.Enums;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Presentation.Dto.Response;

namespace Veyra.Api.Application.Mapper;

public class UserMapper
{
    /// <summary>
    /// O hash chega pronto: o mapper nao conhece o algoritmo, e a senha em claro
    /// nunca passa por aqui.
    /// </summary>
    public User ToEntity(CreateUserRequest request, string passwordHash, UserEnum role = UserEnum.Client)
    {
        return new User(
            request.Name,
            request.Email,
            passwordHash,
            role
        );
    }

    public UserResponse ToResponse(User user)
    {
        return new UserResponse(
            user.Id,
            user.Name,
            user.Email,
            user.RoleName
        );
    }
}
