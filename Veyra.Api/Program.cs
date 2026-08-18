using Veyra.Api.Domain.Entities;
using Veyra.Api.Application.Service;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddScoped<UserService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Use(async(context, next) =>
{
    Console.WriteLine($"Método: {context.Request.Method}");
    Console.WriteLine($"Rota: {context.Request.Path}");

    await next();

    Console.WriteLine($"Status: {context.Response.StatusCode}");
});

app.MapGet("/teste", () =>
{
    return "Veyra OK!!!";
});

app.MapPost("/create", (User user) => {
    return Results.Ok(user);
});



app.MapGet("/status", () =>
{
    return Results.Ok(
        new
        {
            status="online",
            service="Veyra API"
        }
    );
});

app.MapGet("/erro", () =>
{
    return Results.BadRequest(
        new
        {
            status="Requisição Invalida"
        }
    );
});

app.Run();
