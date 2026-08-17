using AzerothWebUI.Core.Data;
using Microsoft.AspNetCore.Identity;

namespace AzerothWebUI.Core.Auth;

public class AdminAuthService(AdminUserRepository adminUsers)
{
    private readonly PasswordHasher<AdminUser> _hasher = new();

    public async Task<AdminUser?> ValidateCredentialsAsync(string username, string password)
    {
        var user = await adminUsers.FindByUsernameAsync(username);
        if (user is null)
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Success ? user : null;
    }

    public string HashPassword(string password) =>
        _hasher.HashPassword(new AdminUser(0, string.Empty, string.Empty), password);
}
