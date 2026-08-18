using AzerothWebUI.Core.Domain;
using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class CharacterRepository(string charactersConnectionString)
{
    // WotLK equipped-item slots: character_inventory.bag = 0 (main backpack container id) and
    // slot 0-18 are the paperdoll equipment slots; higher slots are bags/bank/inventory.
    private const int MaxEquipmentSlot = 18;

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

    public async Task<IReadOnlyList<CharacterSummary>> SearchCharactersAsync(string term, int limit = 25)
    {
        await using var connection = new MySqlConnection(charactersConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.guid, c.name, c.race, c.class, c.level, g.name, c.online
            FROM characters c
            LEFT JOIN guild_member gm ON gm.guid = c.guid
            LEFT JOIN guild g ON g.guildid = gm.guildid
            WHERE c.name LIKE @term
            ORDER BY c.name
            LIMIT @limit
            """;
        command.Parameters.AddWithValue("@term", $"%{term}%");
        command.Parameters.AddWithValue("@limit", limit);

        return await ReadCharacterSummariesAsync(command);
    }

    public async Task<IReadOnlyList<CharacterSummary>> ListCharactersByAccountAsync(int accountId)
    {
        await using var connection = new MySqlConnection(charactersConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT c.guid, c.name, c.race, c.class, c.level, g.name, c.online
            FROM characters c
            LEFT JOIN guild_member gm ON gm.guid = c.guid
            LEFT JOIN guild g ON g.guildid = gm.guildid
            WHERE c.account = @accountId
            ORDER BY c.name
            """;
        command.Parameters.AddWithValue("@accountId", accountId);

        return await ReadCharacterSummariesAsync(command);
    }

    private static async Task<IReadOnlyList<CharacterSummary>> ReadCharacterSummariesAsync(MySqlCommand command)
    {
        var results = new List<CharacterSummary>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new CharacterSummary(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetByte(2),
                reader.GetByte(3),
                reader.GetByte(4),
                reader.IsDBNull(5) ? null : reader.GetString(5),
                reader.GetUInt32(6) != 0));
        }

        return results;
    }

    /// <summary>
    /// Returns the character's profile plus its equipped item entries (not resolved to item
    /// names — the caller joins those against WorldRepository, since acore_world is a separate
    /// database/connection).
    /// </summary>
    public async Task<(int Guid, string Name, byte Race, byte Class, byte Gender, byte Level, string? GuildName, bool Online, IReadOnlyList<(byte Slot, int ItemEntry)> Equipped)?>
        FindCharacterProfileAsync(string name)
    {
        await using var connection = new MySqlConnection(charactersConnectionString);
        await connection.OpenAsync();

        int guid;
        string charName;
        byte race, charClass, gender, level;
        string? guildName;
        bool online;

        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT c.guid, c.name, c.race, c.class, c.gender, c.level, g.name, c.online
                FROM characters c
                LEFT JOIN guild_member gm ON gm.guid = c.guid
                LEFT JOIN guild g ON g.guildid = gm.guildid
                WHERE c.name = @name
                LIMIT 1
                """;
            command.Parameters.AddWithValue("@name", name);

            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return null;
            }

            guid = reader.GetInt32(0);
            charName = reader.GetString(1);
            race = reader.GetByte(2);
            charClass = reader.GetByte(3);
            gender = reader.GetByte(4);
            level = reader.GetByte(5);
            guildName = reader.IsDBNull(6) ? null : reader.GetString(6);
            online = reader.GetUInt32(7) != 0;
        }

        var equipped = new List<(byte Slot, int ItemEntry)>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT ci.slot, ii.itemEntry
                FROM character_inventory ci
                JOIN item_instance ii ON ii.guid = ci.item
                WHERE ci.guid = @guid AND ci.bag = 0 AND ci.slot <= @maxSlot
                ORDER BY ci.slot
                """;
            command.Parameters.AddWithValue("@guid", guid);
            command.Parameters.AddWithValue("@maxSlot", MaxEquipmentSlot);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                equipped.Add((reader.GetByte(0), reader.GetInt32(1)));
            }
        }

        return (guid, charName, race, charClass, gender, level, guildName, online, equipped);
    }
}
