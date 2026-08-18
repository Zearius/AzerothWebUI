using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class MotdRepository(string adminConnectionString)
{
    public async Task<string> GetAsync()
    {
        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT Content FROM Motd WHERE Id = 1";

        var result = await command.ExecuteScalarAsync();
        return result as string ?? string.Empty;
    }

    public async Task SetAsync(string content)
    {
        await using var connection = new MySqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Motd SET Content = @content WHERE Id = 1";
        command.Parameters.AddWithValue("@content", content);

        await command.ExecuteNonQueryAsync();
    }
}
