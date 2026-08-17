using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class CharacterRepository(string charactersConnectionString)
{
    /// <summary>
    /// Returns the name of the character currently online for the given account,
    /// or null if none is online. An account has at most one online character at a time.
    /// </summary>
    public async Task<string?> FindOnlineCharacterNameAsync(int accountId)
    {
        await using var connection = new MySqlConnection(charactersConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM characters WHERE account = @accountId AND online = 1 LIMIT 1";
        command.Parameters.AddWithValue("@accountId", accountId);

        var result = await command.ExecuteScalarAsync();
        return result as string;
    }
}
