using AzerothWebUI.Core.Auth;
using AzerothWebUI.Core.Data;
using AzerothWebUI.Core.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var authConnectionString = builder.Configuration.GetValue<string>("AzerothCore:AuthConnectionString")
    ?? throw new InvalidOperationException("AzerothCore:AuthConnectionString is not configured.");
builder.Services.AddSingleton(new AccountRepository(authConnectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/register", async (RegistrationRequest request, AccountRepository accounts) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 16)
    {
        return Results.BadRequest("Username is required and must be 16 characters or fewer.");
    }

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 16)
    {
        return Results.BadRequest("Password is required and must be 16 characters or fewer.");
    }

    if (string.IsNullOrWhiteSpace(request.Email))
    {
        return Results.BadRequest("Email is required.");
    }

    if (await accounts.UsernameExistsAsync(request.Username))
    {
        return Results.Conflict("Username is already taken.");
    }

    var salt = Srp6.GenerateSalt();
    var verifier = Srp6.ComputeVerifier(request.Username, request.Password, salt);
    await accounts.CreateAccountAsync(request.Username, salt, verifier, request.Email);

    return Results.Created();
})
.WithName("Register");

app.Run();
