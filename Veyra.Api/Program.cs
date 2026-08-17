using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Veyra.Api.Application.Mapper;
using Veyra.Api.Application.Security;
using Veyra.Api.Application.Service;
using Veyra.Api.Domain.Entities;
using Veyra.Api.Domain.Repository;
using Veyra.Api.Infrastructure.OpenApi;
using Veyra.Api.Infrastructure.Repository;
using Veyra.Api.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------- configuracao
// Valida antes de qualquer outro registro: subir com chave ausente ou curta demais
// so daria erro na primeira emissao de token, ja em producao.
var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

if (!jwtSettings.Validate(out var jwtError))
{
    throw new InvalidOperationException($"Configuracao de JWT invalida. {jwtError}");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

// ---------------------------------------------------------------- persistencia
// Singleton: o estado precisa sobreviver entre requests. Trocar por EF Core e so
// registrar outra implementacao destas duas interfaces.
builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();

// ---------------------------------------------------------------- servicos
builder.Services.AddSingleton<UserMapper>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();

// ---------------------------------------------------------------- autenticacao
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Sem isto o handler reescreve "sub"/"role" para as URIs longas do schema WS-*,
        // e [Authorize(Roles = "ADMIN")] passa a falhar em silencio.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(jwtSettings.KeyBytes),
            // O padrao de 5 minutos faria um token expirado continuar aceito.
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimNames.Subject,
            RoleClaimType = ClaimNames.Role
        };
    });

builder.Services.AddAuthorization();

// ---------------------------------------------------------------- http
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<AuthOperationTransformer>();
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    DevelopmentSeeder.SeedAdmin(app.Services, app.Configuration, app.Logger);
}

app.Use(async (context, next) =>
{
    Console.WriteLine($"Método: {context.Request.Method}");
    Console.WriteLine($"Rota: {context.Request.Path}");
    await next();
    Console.WriteLine($"Status: {context.Response.StatusCode}");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/status", () => Results.Ok(new { status = "online", service = "Veyra API" }))
   .AllowAnonymous();

app.MapControllers();

app.Run();
