using Veyra.Api.Domain.Entities;
using Veyra.Api.Infrastructure.Repository;
using Xunit;

namespace Veyra.Api.Tests;

public class InMemoryRepositoryTests
{
    [Fact]
    public void Add_atribui_ids_sequenciais_a_partir_de_um()
    {
        var repo = new InMemoryUserRepository();

        var primeiro = repo.Add(new User("A", "a@veyra.local", "h"));
        var segundo = repo.Add(new User("B", "b@veyra.local", "h"));

        Assert.Equal(1, primeiro.Id);
        Assert.Equal(2, segundo.Id);
    }

    [Fact]
    public void Add_concorrente_nunca_repete_id()
    {
        var repo = new InMemoryUserRepository();

        Parallel.For(0, 200, i => repo.Add(new User($"U{i}", $"u{i}@veyra.local", "h")));

        var ids = repo.FindAll().Select(u => u.Id).ToList();
        Assert.Equal(200, ids.Count);
        Assert.Equal(200, ids.Distinct().Count());
        Assert.DoesNotContain(0, ids);
    }

    [Fact]
    public void FindByEmail_ignora_maiusculas()
    {
        var repo = new InMemoryUserRepository();
        repo.Add(new User("A", "Ana@Veyra.Local", "h"));

        Assert.NotNull(repo.FindByEmail("ana@veyra.local"));
        Assert.True(repo.ExistsByEmail("ANA@VEYRA.LOCAL"));
    }

    [Fact]
    public void Remove_apaga_de_fato_o_usuario()
    {
        var repo = new InMemoryUserRepository();
        var user = repo.Add(new User("A", "a@veyra.local", "h"));

        Assert.True(repo.Remove(user.Id));
        Assert.Null(repo.FindById(user.Id));
        Assert.Empty(repo.FindAll());
        Assert.False(repo.Remove(user.Id));
    }

    [Fact]
    public void TryRevoke_so_deixa_um_chamador_consumir_o_token()
    {
        var token = new RefreshToken(
            "t", userId: 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        var vitorias = 0;
        Parallel.For(0, 64, _ =>
        {
            if (token.TryRevoke())
            {
                Interlocked.Increment(ref vitorias);
            }
        });

        Assert.Equal(1, vitorias);
        Assert.True(token.IsRevoked);
    }

    [Fact]
    public void RevokeAllForUser_atinge_so_os_tokens_ativos_do_usuario()
    {
        var repo = new InMemoryRefreshTokenRepository();
        var doUsuario1 = new RefreshToken("a", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        var jaRevogado = new RefreshToken("b", 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));
        var doUsuario2 = new RefreshToken("c", 2, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        jaRevogado.TryRevoke();
        repo.Add(doUsuario1);
        repo.Add(jaRevogado);
        repo.Add(doUsuario2);

        var revogados = repo.RevokeAllForUser(1);

        Assert.Equal(1, revogados);
        Assert.True(doUsuario1.IsRevoked);
        Assert.True(doUsuario2.IsActive);
    }
}
