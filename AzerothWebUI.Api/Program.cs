using AzerothWebUI.Core.Auth;
using AzerothWebUI.Core.Config;
using AzerothWebUI.Core.Data;
using AzerothWebUI.Core.Domain;
using AzerothWebUI.Core.Soap;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var authConnectionString = builder.Configuration.GetValue<string>("AzerothCore:AuthConnectionString")
    ?? throw new InvalidOperationException("AzerothCore:AuthConnectionString is not configured.");
builder.Services.AddSingleton(new AccountRepository(authConnectionString));

var charactersConnectionString = builder.Configuration.GetValue<string>("AzerothCore:CharactersConnectionString")
    ?? throw new InvalidOperationException("AzerothCore:CharactersConnectionString is not configured.");
builder.Services.AddSingleton(new CharacterRepository(charactersConnectionString));

var adminConnectionString = builder.Configuration.GetValue<string>("AzerothWebUI:AdminConnectionString")
    ?? throw new InvalidOperationException("AzerothWebUI:AdminConnectionString is not configured.");
builder.Services.AddSingleton(new AdminUserRepository(adminConnectionString));
builder.Services.AddSingleton<AdminAuthService>();

var soapUrl = builder.Configuration.GetValue<string>("AzerothCore:SoapUrl")
    ?? throw new InvalidOperationException("AzerothCore:SoapUrl is not configured.");
var soapUsername = builder.Configuration.GetValue<string>("AzerothCore:SoapUsername")
    ?? throw new InvalidOperationException("AzerothCore:SoapUsername is not configured.");
var soapPassword = builder.Configuration.GetValue<string>("AzerothCore:SoapPassword")
    ?? throw new InvalidOperationException("AzerothCore:SoapPassword is not configured.");
builder.Services.AddHttpClient<SoapClient>();
builder.Services.AddSingleton(sp => new SoapClient(
    sp.GetRequiredService<System.Net.Http.HttpClient>(), soapUrl, soapUsername, soapPassword));

var worldserverConfigPath = builder.Configuration.GetValue<string>("AzerothCore:WorldserverConfigPath")
    ?? throw new InvalidOperationException("AzerothCore:WorldserverConfigPath is not configured.");
var moduleConfigDirectory = builder.Configuration.GetValue<string>("AzerothCore:ModuleConfigDirectory")
    ?? throw new InvalidOperationException("AzerothCore:ModuleConfigDirectory is not configured.");
builder.Services.AddSingleton(new ConfigFileService(worldserverConfigPath, moduleConfigDirectory));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "AzerothWebUI.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

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

if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/dev/seed-admin", async (AdminLoginRequest request, AdminUserRepository adminUsers, AdminAuthService auth) =>
    {
        await adminUsers.CreateAsync(request.Username, auth.HashPassword(request.Password));
        return Results.Created();
    })
    .WithName("DevSeedAdmin");
}

app.MapPost("/api/admin/login", async (AdminLoginRequest request, AdminAuthService auth, HttpContext http) =>
{
    var user = await auth.ValidateCredentialsAsync(request.Username, request.Password);
    if (user is null)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim> { new(ClaimTypes.Name, user.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

    return Results.Ok(new { username = user.Username });
})
.WithName("AdminLogin");

app.MapPost("/api/admin/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok();
})
.WithName("AdminLogout")
.RequireAuthorization();

app.MapGet("/api/admin/me", (ClaimsPrincipal user) =>
    Results.Ok(new { username = user.Identity!.Name }))
.WithName("AdminMe")
.RequireAuthorization();

app.MapGet("/api/admin/status", async (SoapClient soap) =>
{
    try
    {
        var output = await soap.ExecuteCommandAsync("server info");
        return Results.Ok(new ServerStatus(output));
    }
    catch (SoapCommandException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("AdminServerStatus")
.RequireAuthorization();

app.MapGet("/api/admin/accounts", async (AccountRepository accounts) =>
    Results.Ok(await accounts.ListAccountsAsync()))
.WithName("AdminListAccounts")
.RequireAuthorization();

app.MapGet("/api/admin/config/files", (ConfigFileService configFiles) =>
    Results.Ok(configFiles.ListFiles()))
.WithName("AdminListConfigFiles")
.RequireAuthorization();

app.MapGet("/api/admin/config/{file}", async (string file, ConfigFileService configFiles) =>
{
    var entry = configFiles.FindEntry(file);
    if (entry is null)
    {
        return Results.NotFound($"Unknown config file '{file}'.");
    }

    return Results.Ok(await configFiles.ReadEntriesAsync(entry));
})
.WithName("AdminGetConfig")
.RequireAuthorization();

app.MapPatch("/api/admin/config/{file}/{key}", async (string file, string key, UpdateConfigValueRequest request, ConfigFileService configFiles, SoapClient soap) =>
{
    var fileEntry = configFiles.FindEntry(file);
    if (fileEntry is null)
    {
        return Results.NotFound($"Unknown config file '{file}'.");
    }

    var requiresRestart = fileEntry.Descriptor.AlwaysRestartRequired || RestartRequiredKeys.Keys.Contains(key);

    var store = configFiles.GetStore(fileEntry);
    var content = await store.ReadAllTextAsync();
    var updatedContent = WorldserverConfigWriter.SetValue(content, key, request.Value);
    if (updatedContent is null)
    {
        return Results.NotFound($"Config key '{key}' was not found in '{file}'.");
    }

    await store.WriteAllTextAsync(updatedContent);

    var updatedEntry = fileEntry.Parser(updatedContent).FirstOrDefault(e => e.Key == key) is { } parsed
        ? parsed with { SourceFile = fileEntry.Descriptor.DisplayName, RequiresRestart = requiresRestart }
        : null;

    if (requiresRestart)
    {
        return Results.Ok(new { entry = updatedEntry, requiresRestart = true, reloadResult = (string?)null });
    }

    try
    {
        var reloadOutput = await soap.ExecuteCommandAsync("reload config");
        return Results.Ok(new { entry = updatedEntry, requiresRestart = false, reloadResult = reloadOutput });
    }
    catch (SoapCommandException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("AdminUpdateConfig")
.RequireAuthorization();

app.MapPost("/api/admin/accounts/{username}/ban", async (string username, SoapClient soap) =>
{
    try
    {
        var output = await soap.ExecuteCommandAsync($"ban account {username} -1 webui-admin-action");
        return Results.Ok(new { result = output });
    }
    catch (SoapCommandException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("AdminBanAccount")
.RequireAuthorization();

app.MapPost("/api/admin/accounts/{username}/unban", async (string username, SoapClient soap) =>
{
    try
    {
        var output = await soap.ExecuteCommandAsync($"unban account {username}");
        return Results.Ok(new { result = output });
    }
    catch (SoapCommandException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("AdminUnbanAccount")
.RequireAuthorization();

app.MapPost("/api/admin/accounts/{username}/kick", async (string username, AccountRepository accounts, CharacterRepository characters, SoapClient soap) =>
{
    var accountId = await accounts.FindIdByUsernameAsync(username);
    if (accountId is null)
    {
        return Results.NotFound($"Account '{username}' not found.");
    }

    var characterName = await characters.FindOnlineCharacterNameAsync(accountId.Value);
    if (characterName is null)
    {
        return Results.BadRequest($"Account '{username}' has no character currently online.");
    }

    try
    {
        var output = await soap.ExecuteCommandAsync($"kick {characterName}");
        return Results.Ok(new { result = output });
    }
    catch (SoapCommandException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("AdminKickAccount")
.RequireAuthorization();

app.MapFallbackToFile("index.html");

app.Run();
