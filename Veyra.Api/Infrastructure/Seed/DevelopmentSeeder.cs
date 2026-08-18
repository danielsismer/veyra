using Veyra.Api.Application.Service;
using Veyra.Api.Domain.Enums;
using Veyra.Api.Domain.Repository;
using Veyra.Api.Presentation.Dto.Request;

namespace Veyra.Api.Infrastructure.Seed;

public static class DevelopmentSeeder
{
    public static void SeedAdmin(IServiceProvider services, IConfiguration configuration, ILogger logger)
    {
        var email = configuration["Seed:Admin:Email"];
        var password = configuration["Seed:Admin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("Seed do admin ignorado: Seed:Admin:Email/Password nao configurados.");
            return;
        }

        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        if (users.ExistsByEmail(email))
        {
            return;
        }

        var auth = scope.ServiceProvider.GetRequiredService<AuthService>();

        auth.CreateUser(
            new CreateUserRequest
            {
                Name = configuration["Seed:Admin:Name"] ?? "Admin",
                Email = email,
                Password = password
            },
            UserEnum.Admin);

        logger.LogInformation("Admin de desenvolvimento criado: {Email}", email);
    }
}
