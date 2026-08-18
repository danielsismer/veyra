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

var jwtSettings = builder.Configuration
    .GetSection(JwtSettings.SectionName)
    .Get<JwtSettings>() ?? new JwtSettings();

if (!jwtSettings.Validate(out var jwtError))
{
    throw new InvalidOperationException($"Configuracao de JWT invalida. {jwtError}");
}

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

builder.Services.AddSingleton<IUserRepository, InMemoryUserRepository>();
builder.Services.AddSingleton<IRefreshTokenRepository, InMemoryRefreshTokenRepository>();

builder.Services.AddSingleton<UserMapper>();
builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<TokenService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
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
            ClockSkew = TimeSpan.Zero,
            NameClaimType = ClaimNames.Subject,
            RoleClaimType = ClaimNames.Role
        };
    });

builder.Services.AddAuthorization();

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
