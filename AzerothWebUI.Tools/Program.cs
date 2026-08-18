using AzerothWebUI.Core.Auth;

if (args.Length == 0)
{
    PrintUsage();
    return 1;
}

switch (args[0])
{
    case "hash-admin-password" when args.Length == 2:
        HashAdminPassword(args[1]);
        return 0;

    case "create-gm-account" when args.Length == 3:
        CreateGmAccount(args[1], args[2]);
        return 0;

    default:
        PrintUsage();
        return 1;
}

static void HashAdminPassword(string password)
{
    var hash = new AdminAuthService(null!).HashPassword(password);
    Console.WriteLine($"INSERT INTO azerothwebui.AdminUsers (Username, PasswordHash) VALUES ('admin', '{hash}');");
}

static void CreateGmAccount(string username, string password)
{
    var salt = Srp6.GenerateSalt();
    var verifier = Srp6.ComputeVerifier(username, password, salt);
    var upperUsername = username.ToUpperInvariant();
    var saltHex = Convert.ToHexString(salt);
    var verifierHex = Convert.ToHexString(verifier);

    Console.WriteLine($"INSERT INTO account (username, salt, verifier, email, reg_mail, joindate) VALUES ('{upperUsername}', UNHEX('{saltHex}'), UNHEX('{verifierHex}'), '', '', NOW());");
    Console.WriteLine("INSERT INTO account_access (id, gmlevel, RealmID) VALUES (LAST_INSERT_ID(), 3, -1);");
}

static void PrintUsage()
{
    Console.Error.WriteLine("""
        Usage:
          dotnet run --project AzerothWebUI.Tools -- hash-admin-password <password>
              Prints an INSERT statement for AzerothWebUI's own admin login (azerothwebui.AdminUsers).

          dotnet run --project AzerothWebUI.Tools -- create-gm-account <username> <password>
              Prints INSERT statements for a GM-level AzerothCore account (acore_auth.account +
              account_access, gmlevel 3), suitable for the SOAP service account.
        """);
}
