using AzerothWebUI.Core.Domain;
using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class AccountRepository(string authConnectionString)
{
    public async Task<IReadOnlyList<AdminAccountSummary>> ListAccountsAsync()
    {
        await using var connection = new MySqlConnection(authConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                a.id,
                a.username,
                a.email,
                COALESCE(aa.gmlevel, 0) AS gmlevel,
                EXISTS (
                    SELECT 1 FROM account_banned ab
                    WHERE ab.id = a.id AND ab.active = 1
                        AND (ab.unbandate = 0 OR ab.unbandate > UNIX_TIMESTAMP())
                ) AS banned,
                a.online
            FROM account a
            LEFT JOIN account_access aa ON aa.id = a.id
            ORDER BY a.username
            """;

        var results = new List<AdminAccountSummary>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new AdminAccountSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetByte(3),
                reader.GetBoolean(4),
                reader.GetUInt32(5) != 0));
        }

        return results;
    }

    public async Task<int?> FindIdByUsernameAsync(string username)
    {
        await using var connection = new MySqlConnection(authConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM account WHERE username = @username LIMIT 1";
        command.Parameters.AddWithValue("@username", username.ToUpperInvariant());

        var result = await command.ExecuteScalarAsync();
        return result is null ? null : Convert.ToInt32(result);
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        await using var connection = new MySqlConnection(authConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1 FROM account WHERE username = @username LIMIT 1";
        command.Parameters.AddWithValue("@username", username.ToUpperInvariant());

        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync();
    }

    public async Task CreateAccountAsync(string username, byte[] salt, byte[] verifier, string email)
    {
        await using var connection = new MySqlConnection(authConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO account (username, salt, verifier, email, reg_mail, joindate)
            VALUES (@username, @salt, @verifier, @email, @regMail, NOW())
            """;
        command.Parameters.AddWithValue("@username", username.ToUpperInvariant());
        command.Parameters.AddWithValue("@salt", salt);
        command.Parameters.AddWithValue("@verifier", verifier);
        command.Parameters.AddWithValue("@email", email);
        command.Parameters.AddWithValue("@regMail", email);

        await command.ExecuteNonQueryAsync();
    }
}
