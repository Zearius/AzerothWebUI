using AzerothWebUI.Core.Data;

namespace AzerothWebUI.Core.Auth;

public class PlayerAuthService(AccountRepository accounts)
{
    public async Task<int?> ValidateCredentialsAsync(string username, string password)
    {
        var credentials = await accounts.FindCredentialsByUsernameAsync(username);
        if (credentials is null || credentials.Banned)
        {
            return null;
        }

        return Srp6.VerifyPassword(username, password, credentials.Salt, credentials.Verifier)
            ? credentials.Id
            : null;
    }
}
