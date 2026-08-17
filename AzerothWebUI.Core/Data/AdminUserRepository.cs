using AzerothWebUI.Core.Auth;
using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class AdminUserRepository(string adminConnectionString)
{
    public async Task<AdminUser?> FindByUsernameAsync(string username)
    {
        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Username, PasswordHash FROM AdminUsers WHERE Username = @username LIMIT 1";
        command.Parameters.AddWithValue("@username", username);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new AdminUser(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
    }

    public async Task CreateAsync(string username, string passwordHash)
    {
        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT INTO AdminUsers (Username, PasswordHash) VALUES (@username, @passwordHash)";
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@passwordHash", passwordHash);

        await command.ExecuteNonQueryAsync();
    }
}
