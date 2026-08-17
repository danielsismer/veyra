using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Veyra.Api.Application.Security;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Domain.Enums;
using Xunit;

namespace Veyra.Api.Tests;

public class TokenServiceTests
{
    [Fact]
    public async Task Access_token_valida_contra_os_mesmos_parametros_usados_no_Program()
    {
        var ctx = new AuthTestContext();
        var user = ctx.Users.Add(new User("Ana", "ana@veyra.local", "hash", UserEnum.Admin));

        var token = ctx.Tokens.CreateAccessToken(user);
        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters(ctx.Settings));

        Assert.True(resultado.IsValid);
    }

    [Fact]
    public async Task Access_token_carrega_sub_email_name_jti_e_role()
    {
        var ctx = new AuthTestContext();
        var user = ctx.Users.Add(new User("Ana", "ana@veyra.local", "hash", UserEnum.Admin));

        var resultado = await new JsonWebTokenHandler()
            .ValidateTokenAsync(ctx.Tokens.CreateAccessToken(user), Parameters(ctx.Settings));

        var claims = resultado.ClaimsIdentity.Claims.ToDictionary(c => c.Type, c => c.Value);

        Assert.Equal(user.Id.ToString(), claims[JwtRegisteredClaimNames.Sub]);
        Assert.Equal("ana@veyra.local", claims[JwtRegisteredClaimNames.Email]);
        Assert.Equal("Ana", claims[JwtRegisteredClaimNames.Name]);
        Assert.True(claims.ContainsKey(JwtRegisteredClaimNames.Jti));
    }

    /// <summary>
    /// [Authorize(Roles = "ADMIN")] compara o VALOR da claim com Ordinal, ou seja,
    /// diferencia maiusculas. Se este teste quebrar, admins levam 403 em silencio.
    /// </summary>
    [Fact]
    public void Role_vai_para_a_claim_em_maiusculo_batendo_com_o_atributo_Authorize()
    {
        var ctx = new AuthTestContext();

        Assert.Equal("ADMIN", new User("a", "a@a.com", "h", UserEnum.Admin).RoleName);
        Assert.Equal("CLIENT", new User("c", "c@c.com", "h", UserEnum.Client).RoleName);
        Assert.Equal("SALESPERSON", new User("s", "s@s.com", "h", UserEnum.Salesperson).RoleName);

        var user = ctx.Users.Add(new User("Ana", "ana@veyra.local", "hash", UserEnum.Admin));
        var jwt = new JsonWebTokenHandler().ReadJsonWebToken(ctx.Tokens.CreateAccessToken(user));

        Assert.Equal("ADMIN", jwt.GetClaim(ClaimNames.Role).Value);
    }

    [Fact]
    public async Task Token_assinado_com_outra_chave_e_rejeitado()
    {
        var ctx = new AuthTestContext();
        var user = ctx.Users.Add(new User("Ana", "ana@veyra.local", "hash"));
        var token = ctx.Tokens.CreateAccessToken(user);

        var outraChave = new JwtSettings
        {
            Issuer = ctx.Settings.Issuer,
            Audience = ctx.Settings.Audience,
            Key = "outra-chave-completamente-diferente-com-32-bytes+"
        };

        var resultado = await new JsonWebTokenHandler().ValidateTokenAsync(token, Parameters(outraChave));

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void Refresh_tokens_sao_unicos_e_com_validade_configurada()
    {
        var ctx = new AuthTestContext(refreshTokenDays: 7);

        var gerados = Enumerable.Range(0, 500).Select(_ => ctx.Tokens.CreateRefreshToken(1)).ToList();

        Assert.Equal(gerados.Count, gerados.Select(t => t.Token).Distinct().Count());
        Assert.All(gerados, t => Assert.True(t.IsActive));
        Assert.All(gerados, t => Assert.InRange(
            (t.ExpiresAt - t.CreatedAt).TotalDays, 6.99, 7.01));
    }

    private static TokenValidationParameters Parameters(JwtSettings settings) => new()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = settings.Issuer,
        ValidAudience = settings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(settings.KeyBytes),
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimNames.Subject,
        RoleClaimType = ClaimNames.Role
    };
}
