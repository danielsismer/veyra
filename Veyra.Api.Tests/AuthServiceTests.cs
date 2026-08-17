using Microsoft.AspNetCore.Identity;
using Veyra.Api.Application.Security;
using Veyra.Api.Domain.Enums;
using Veyra.Api.Presentation.Dto.Request;
using Xunit;

namespace Veyra.Api.Tests;

public class AuthServiceTests
{
    [Fact]
    public void Register_grava_hash_e_nunca_a_senha_em_claro()
    {
        var ctx = new AuthTestContext();

        ctx.Auth.Register(AuthTestContext.Request());
        var user = ctx.Users.FindByEmail("user@veyra.local")!;

        Assert.NotEqual(AuthTestContext.DefaultPassword, user.PasswordHash);
        Assert.DoesNotContain(AuthTestContext.DefaultPassword, user.PasswordHash);
        Assert.Equal(
            PasswordVerificationResult.Success,
            ctx.PasswordHasher.VerifyHashedPassword(user, user.PasswordHash, AuthTestContext.DefaultPassword));
    }

    [Fact]
    public void Register_gera_hash_diferente_para_a_mesma_senha()
    {
        var ctx = new AuthTestContext();

        ctx.Auth.Register(AuthTestContext.Request("a@veyra.local"));
        ctx.Auth.Register(AuthTestContext.Request("b@veyra.local"));

        // Salt por senha: hashes iguais denunciariam ausencia de salt.
        Assert.NotEqual(
            ctx.Users.FindByEmail("a@veyra.local")!.PasswordHash,
            ctx.Users.FindByEmail("b@veyra.local")!.PasswordHash);
    }

    [Fact]
    public void Register_cria_sempre_com_role_Client()
    {
        var ctx = new AuthTestContext();

        var resultado = ctx.Auth.Register(AuthTestContext.Request());

        Assert.Equal("CLIENT", resultado.Value!.Role);
        Assert.Equal(UserEnum.Client, ctx.Users.FindByEmail("user@veyra.local")!.Role);
    }

    [Fact]
    public void Register_recusa_email_duplicado_ignorando_maiusculas()
    {
        var ctx = new AuthTestContext();
        ctx.Auth.Register(AuthTestContext.Request("user@veyra.local"));

        var duplicado = ctx.Auth.Register(AuthTestContext.Request("USER@VEYRA.LOCAL"));

        Assert.False(duplicado.Succeeded);
        Assert.Equal(AuthError.EmailAlreadyUsed, duplicado.Error);
    }

    [Fact]
    public void Login_com_credenciais_corretas_emite_os_dois_tokens()
    {
        var ctx = new AuthTestContext();
        ctx.Auth.Register(AuthTestContext.Request());

        var login = ctx.Auth.Login(new LoginRequest
        {
            Email = "user@veyra.local",
            Password = AuthTestContext.DefaultPassword
        });

        Assert.True(login.Succeeded);
        Assert.NotEmpty(login.Value!.AccessToken);
        Assert.NotEmpty(login.Value.RefreshToken);
        Assert.Equal("Bearer", login.Value.TokenType);
        Assert.Equal(15 * 60, login.Value.ExpiresIn);
        Assert.NotNull(ctx.RefreshTokens.Find(login.Value.RefreshToken));
    }

    [Theory]
    [InlineData("user@veyra.local", "senha-errada")]
    [InlineData("naocadastrado@veyra.local", AuthTestContext.DefaultPassword)]
    public void Login_invalido_devolve_sempre_o_mesmo_erro(string email, string senha)
    {
        var ctx = new AuthTestContext();
        ctx.Auth.Register(AuthTestContext.Request());

        var login = ctx.Auth.Login(new LoginRequest { Email = email, Password = senha });

        // Senha errada e e-mail inexistente sao indistinguiveis: nao da para enumerar contas.
        Assert.False(login.Succeeded);
        Assert.Equal(AuthError.InvalidCredentials, login.Error);
    }

    [Fact]
    public void Login_aceita_email_com_caixa_diferente()
    {
        var ctx = new AuthTestContext();
        ctx.Auth.Register(AuthTestContext.Request("user@veyra.local"));

        var login = ctx.Auth.Login(new LoginRequest
        {
            Email = "User@Veyra.Local",
            Password = AuthTestContext.DefaultPassword
        });

        Assert.True(login.Succeeded);
    }

    [Fact]
    public void CreateUser_permite_semear_um_admin()
    {
        var ctx = new AuthTestContext();

        var admin = ctx.Auth.CreateUser(AuthTestContext.Request("admin@veyra.local"), UserEnum.Admin);

        Assert.Equal("ADMIN", admin.RoleName);
        Assert.True(admin.Id > 0);
    }
}
