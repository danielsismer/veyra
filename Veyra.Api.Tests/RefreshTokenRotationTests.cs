using Veyra.Api.Application.Security;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Presentation.Dto.Request;
using Veyra.Api.Presentation.Dto.Response;
using Xunit;

namespace Veyra.Api.Tests;

public class RefreshTokenRotationTests
{
    [Fact]
    public void Refresh_devolve_um_par_novo_e_marca_o_antigo_como_substituido()
    {
        var ctx = new AuthTestContext();
        var login = Login(ctx);

        var refreshed = ctx.Auth.Refresh(login.RefreshToken);

        Assert.True(refreshed.Succeeded);
        Assert.NotEqual(login.RefreshToken, refreshed.Value!.RefreshToken);

        var antigo = ctx.RefreshTokens.Find(login.RefreshToken)!;
        Assert.True(antigo.IsRevoked);
        Assert.Equal(refreshed.Value.RefreshToken, antigo.ReplacedByToken);

        var novo = ctx.RefreshTokens.Find(refreshed.Value.RefreshToken)!;
        Assert.True(novo.IsActive);
    }

    [Fact]
    public void Reusar_um_refresh_token_ja_rotacionado_derruba_a_familia_inteira()
    {
        var ctx = new AuthTestContext();
        var login = Login(ctx);

        var rotacionado = ctx.Auth.Refresh(login.RefreshToken).Value!;

        var reuso = ctx.Auth.Refresh(login.RefreshToken);

        Assert.False(reuso.Succeeded);
        Assert.Equal(AuthError.InvalidRefreshToken, reuso.Error);

        Assert.True(ctx.RefreshTokens.Find(rotacionado.RefreshToken)!.IsRevoked);
        Assert.False(ctx.Auth.Refresh(rotacionado.RefreshToken).Succeeded);
    }

    [Fact]
    public void Refresh_token_desconhecido_e_recusado()
    {
        var ctx = new AuthTestContext();

        var resultado = ctx.Auth.Refresh("token-que-nunca-existiu");

        Assert.False(resultado.Succeeded);
        Assert.Equal(AuthError.InvalidRefreshToken, resultado.Error);
    }

    [Fact]
    public void Refresh_token_expirado_e_recusado_sem_derrubar_a_familia()
    {
        var ctx = new AuthTestContext();
        var login = Login(ctx);
        var usuario = ctx.Users.FindByEmail("user@veyra.local")!;

        var vencido = new RefreshToken(
            token: "token-vencido",
            userId: usuario.Id,
            createdAt: DateTimeOffset.UtcNow.AddDays(-10),
            expiresAt: DateTimeOffset.UtcNow.AddDays(-1));
        ctx.RefreshTokens.Add(vencido);

        var resultado = ctx.Auth.Refresh("token-vencido");

        Assert.False(resultado.Succeeded);
        Assert.True(ctx.RefreshTokens.Find(login.RefreshToken)!.IsActive);
    }

    [Fact]
    public void Logout_revoga_o_token_e_impede_refresh_posterior()
    {
        var ctx = new AuthTestContext();
        var login = Login(ctx);
        var usuario = ctx.Users.FindByEmail("user@veyra.local")!;

        ctx.Auth.Logout(usuario.Id, login.RefreshToken);

        Assert.False(ctx.Auth.Refresh(login.RefreshToken).Succeeded);
    }

    [Fact]
    public void Logout_de_outro_usuario_nao_revoga_o_token()
    {
        var ctx = new AuthTestContext();
        var login = Login(ctx);
        var intruso = ctx.Auth.Register(AuthTestContext.Request("intruso@veyra.local"));

        ctx.Auth.Logout(intruso.Value!.Id, login.RefreshToken);

        Assert.True(ctx.RefreshTokens.Find(login.RefreshToken)!.IsActive);
    }

    [Fact]
    public void Refresh_concorrente_com_o_mesmo_token_deixa_apenas_um_vencedor()
    {
        var ctx = new AuthTestContext();
        var login = Login(ctx);

        var resultados = new bool[16];
        Parallel.For(0, resultados.Length, i => resultados[i] = ctx.Auth.Refresh(login.RefreshToken).Succeeded);

        Assert.Equal(1, resultados.Count(sucesso => sucesso));
    }

    private static AuthResponse Login(AuthTestContext ctx)
    {
        ctx.Auth.Register(AuthTestContext.Request());

        var login = ctx.Auth.Login(new LoginRequest
        {
            Email = "user@veyra.local",
            Password = AuthTestContext.DefaultPassword
        });

        Assert.True(login.Succeeded);
        return login.Value!;
    }
}
