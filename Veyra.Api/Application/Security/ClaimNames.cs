namespace Veyra.Api.Application.Security;

/// <summary>
/// Nomes curtos de claim usados no payload do JWT. Eles precisam bater com o
/// RoleClaimType/NameClaimType configurados no JwtBearer, senao [Authorize(Roles = "...")]
/// falha silenciosamente.
/// </summary>
public static class ClaimNames
{
    public const string Role = "role";
    public const string Subject = "sub";
}
