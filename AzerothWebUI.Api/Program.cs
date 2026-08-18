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

var worldConnectionString = builder.Configuration.GetValue<string>("AzerothCore:WorldConnectionString")
    ?? throw new InvalidOperationException("AzerothCore:WorldConnectionString is not configured.");
builder.Services.AddSingleton(new WorldRepository(worldConnectionString));
builder.Services.AddSingleton(new AhBotRepository(worldConnectionString));

var adminConnectionString = builder.Configuration.GetValue<string>("AzerothWebUI:AdminConnectionString")
    ?? throw new InvalidOperationException("AzerothWebUI:AdminConnectionString is not configured.");
builder.Services.AddSingleton(new AdminUserRepository(adminConnectionString));
builder.Services.AddSingleton<AdminAuthService>();
builder.Services.AddSingleton<PlayerAuthService>();

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

const string AdminScheme = "Admin";
const string PlayerScheme = "Player";

static void ConfigureAuthCookie(CookieAuthenticationOptions options, string cookieName)
{
    options.Cookie.Name = cookieName;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
}

builder.Services.AddAuthentication(AdminScheme)
    .AddCookie(AdminScheme, options => ConfigureAuthCookie(options, "AzerothWebUI.Admin"))
    .AddCookie(PlayerScheme, options => ConfigureAuthCookie(options, "AzerothWebUI.Player"));

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AdminScheme, policy => policy.AddAuthenticationSchemes(AdminScheme).RequireAuthenticatedUser());
    options.AddPolicy(PlayerScheme, policy => policy.AddAuthenticationSchemes(PlayerScheme).RequireAuthenticatedUser());
});

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
    var identity = new ClaimsIdentity(claims, AdminScheme);
    await http.SignInAsync(AdminScheme, new ClaimsPrincipal(identity));

    return Results.Ok(new { username = user.Username });
})
.WithName("AdminLogin");

app.MapPost("/api/admin/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(AdminScheme);
    return Results.Ok();
})
.WithName("AdminLogout")
.RequireAuthorization(AdminScheme);

app.MapGet("/api/admin/me", (ClaimsPrincipal user) =>
    Results.Ok(new { username = user.Identity!.Name }))
.WithName("AdminMe")
.RequireAuthorization(AdminScheme);

app.MapPost("/api/player/login", async (PlayerLoginRequest request, PlayerAuthService auth, HttpContext http) =>
{
    var accountId = await auth.ValidateCredentialsAsync(request.Username, request.Password);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var claims = new List<Claim>
    {
        new(ClaimTypes.Name, request.Username.ToUpperInvariant()),
        new(ClaimTypes.NameIdentifier, accountId.Value.ToString()),
    };
    var identity = new ClaimsIdentity(claims, PlayerScheme);
    await http.SignInAsync(PlayerScheme, new ClaimsPrincipal(identity));

    return Results.Ok(new { username = request.Username.ToUpperInvariant() });
})
.WithName("PlayerLogin");

app.MapPost("/api/player/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(PlayerScheme);
    return Results.Ok();
})
.WithName("PlayerLogout")
.RequireAuthorization(PlayerScheme);

app.MapGet("/api/player/me", async (HttpContext http) =>
{
    var result = await http.AuthenticateAsync(PlayerScheme);
    return Results.Ok(new { username = result.Principal!.Identity!.Name });
})
.WithName("PlayerMe")
.RequireAuthorization(PlayerScheme);

app.MapGet("/api/armory/characters", async (string? q, CharacterRepository characters) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(Array.Empty<CharacterSummary>());
    }

    return Results.Ok(await characters.SearchCharactersAsync(q));
})
.WithName("ArmorySearchCharacters");

app.MapGet("/api/armory/characters/{name}", async (string name, CharacterRepository characters, WorldRepository world) =>
{
    var profile = await characters.FindCharacterProfileAsync(name);
    if (profile is null)
    {
        return Results.NotFound($"Character '{name}' not found.");
    }

    var itemEntries = profile.Value.Equipped.Select(e => e.ItemEntry).Distinct().ToArray();
    var items = await world.FindItemsAsync(itemEntries);

    var equippedItems = profile.Value.Equipped
        .Select(e => items.TryGetValue(e.ItemEntry, out var item)
            ? new EquippedItem(e.Slot, e.ItemEntry, item.Name, item.Quality, item.DisplayId, item.ItemLevel)
            : new EquippedItem(e.Slot, e.ItemEntry, $"Unknown item #{e.ItemEntry}", 0, 0, 0))
        .ToList();

    var detail = new CharacterDetail(
        profile.Value.Guid,
        profile.Value.Name,
        profile.Value.Race,
        profile.Value.Class,
        profile.Value.Gender,
        profile.Value.Level,
        profile.Value.GuildName,
        profile.Value.Online,
        equippedItems);

    return Results.Ok(detail);
})
.WithName("ArmoryGetCharacter");

app.MapGet("/api/armory/items/search", async (string? q, WorldRepository world) =>
{
    if (string.IsNullOrWhiteSpace(q))
    {
        return Results.Ok(Array.Empty<ItemSearchResult>());
    }

    return Results.Ok(await world.SearchItemsAsync(q));
})
.WithName("ArmorySearchItems");

app.MapGet("/api/armory/items/{id:int}", async (int id, WorldRepository world) =>
{
    var item = await world.FindItemAsync(id);
    if (item is null)
    {
        return Results.NotFound($"Item #{id} not found.");
    }

    var dropSources = await world.FindDropSourcesAsync(id);
    return Results.Ok(new { item, dropSources });
})
.WithName("ArmoryGetItem");

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
.RequireAuthorization(AdminScheme);

app.MapGet("/api/admin/accounts", async (AccountRepository accounts) =>
    Results.Ok(await accounts.ListAccountsAsync()))
.WithName("AdminListAccounts")
.RequireAuthorization(AdminScheme);

app.MapGet("/api/admin/config/files", (ConfigFileService configFiles) =>
    Results.Ok(configFiles.ListFiles()))
.WithName("AdminListConfigFiles")
.RequireAuthorization(AdminScheme);

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
.RequireAuthorization(AdminScheme);

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
.RequireAuthorization(AdminScheme);

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
.RequireAuthorization(AdminScheme);

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
.RequireAuthorization(AdminScheme);

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
.RequireAuthorization(AdminScheme);

app.MapGet("/api/admin/ahbot/houses", async (AhBotRepository ahBot) =>
    Results.Ok(await ahBot.ListHousesAsync()))
.WithName("AdminListAhBotHouses")
.RequireAuthorization(AdminScheme);

app.MapPut("/api/admin/ahbot/houses/{auctionHouse:int}", async (int auctionHouse, AhBotHouse settings, AhBotRepository ahBot) =>
{
    var updated = await ahBot.UpdateHouseAsync(auctionHouse, settings);
    return updated ? Results.Ok() : Results.NotFound($"Auction house {auctionHouse} not found.");
})
.WithName("AdminUpdateAhBotHouse")
.RequireAuthorization(AdminScheme);

app.MapGet("/api/admin/ahbot/disabled-items", async (AhBotRepository ahBot) =>
    Results.Ok(await ahBot.ListDisabledItemsAsync()))
.WithName("AdminListAhBotDisabledItems")
.RequireAuthorization(AdminScheme);

app.MapPost("/api/admin/ahbot/disabled-items/{itemId:int}", async (int itemId, AhBotRepository ahBot) =>
{
    await ahBot.AddDisabledItemAsync(itemId);
    return Results.Created();
})
.WithName("AdminAddAhBotDisabledItem")
.RequireAuthorization(AdminScheme);

app.MapDelete("/api/admin/ahbot/disabled-items/{itemId:int}", async (int itemId, AhBotRepository ahBot) =>
{
    await ahBot.RemoveDisabledItemAsync(itemId);
    return Results.Ok();
})
.WithName("AdminRemoveAhBotDisabledItem")
.RequireAuthorization(AdminScheme);

app.MapPost("/api/admin/items/award", async (AwardItemRequest request, SoapClient soap) =>
{
    if (string.IsNullOrWhiteSpace(request.CharacterName))
    {
        return Results.BadRequest("Character name is required.");
    }

    if (request.ItemId <= 0 || request.Count <= 0)
    {
        return Results.BadRequest("Item id and count must be positive.");
    }

    // .send items (not .additem) - .additem requires the target to be online and selected,
    // which isn't possible over SOAP; .send items is DB-persisted mail delivery that works
    // regardless of whether the character is currently logged in.
    var subject = string.IsNullOrWhiteSpace(request.Subject) ? "Item Delivery" : request.Subject;
    var message = string.IsNullOrWhiteSpace(request.Message) ? "An item has been sent to you by an administrator." : request.Message;

    try
    {
        var output = await soap.ExecuteCommandAsync(
            $".send items {request.CharacterName} \"{subject}\" \"{message}\" {request.ItemId}:{request.Count}");
        return Results.Ok(new { result = output });
    }
    catch (SoapCommandException ex)
    {
        return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
    }
})
.WithName("AdminAwardItem")
.RequireAuthorization(AdminScheme);

app.MapFallbackToFile("index.html");

app.Run();
