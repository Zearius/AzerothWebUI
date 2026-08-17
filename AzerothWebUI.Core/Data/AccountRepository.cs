using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class AccountRepository(string authConnectionString)
{
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
