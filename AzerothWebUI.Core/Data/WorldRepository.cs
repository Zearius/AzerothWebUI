using AzerothWebUI.Core.Domain;
using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class WorldRepository(string worldConnectionString)
{
    // Every _loot_template table shares this schema. Direct drops have Item <> 0; a row with
    // Item = 0 and Reference <> 0 instead points at reference_loot_template.Entry, a shared
    // loot group pulled in by many creatures/objects — resolved separately below.
    private static readonly IReadOnlyList<string> DirectLootTables =
    [
        "creature_loot_template",
        "fishing_loot_template",
        "gameobject_loot_template",
        "skinning_loot_template",
        "disenchant_loot_template",
        "pickpocketing_loot_template",
        "prospecting_loot_template",
        "milling_loot_template",
        "mail_loot_template",
        "item_loot_template",
        "player_loot_template",
        "spell_loot_template",
    ];

    public async Task<ItemDetail?> FindItemAsync(int itemId)
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT entry, name, Quality, displayid, ItemLevel, RequiredLevel, class, subclass, InventoryType, description,
                stat_type1, stat_value1, stat_type2, stat_value2, stat_type3, stat_value3,
                stat_type4, stat_value4, stat_type5, stat_value5, stat_type6, stat_value6,
                stat_type7, stat_value7, stat_type8, stat_value8, stat_type9, stat_value9, stat_type10, stat_value10
            FROM item_template
            WHERE entry = @entry
            """;
        command.Parameters.AddWithValue("@entry", itemId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        var stats = new List<ItemStat>();
        for (var i = 0; i < 10; i++)
        {
            var typeOrdinal = 10 + i * 2;
            var valueOrdinal = 11 + i * 2;
            var type = reader.GetByte(typeOrdinal);
            var value = reader.GetInt32(valueOrdinal);
            if (type != 0 && value != 0)
            {
                stats.Add(new ItemStat(type, value));
            }
        }

        return new ItemDetail(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetByte(2),
            reader.GetInt32(3),
            reader.GetInt32(4),
            reader.GetByte(5),
            reader.GetByte(6),
            reader.GetByte(7),
            reader.GetByte(8),
            reader.IsDBNull(9) ? null : reader.GetString(9),
            stats);
    }

    public async Task<IReadOnlyList<ItemSearchResult>> SearchItemsAsync(string term, int limit = 25)
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT entry, name, Quality, displayid, ItemLevel
            FROM item_template
            WHERE name LIKE @term
            ORDER BY name
            LIMIT @limit
            """;
        command.Parameters.AddWithValue("@term", $"%{term}%");
        command.Parameters.AddWithValue("@limit", limit);

        var results = new List<ItemSearchResult>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new ItemSearchResult(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetByte(2),
                reader.GetInt32(3),
                reader.GetInt32(4)));
        }

        return results;
    }

    public async Task<IReadOnlyList<DropSource>> FindDropSourcesAsync(int itemId)
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        var results = new List<DropSource>();

        foreach (var table in DirectLootTables)
        {
            await using var command = connection.CreateCommand();
            if (table == "creature_loot_template")
            {
                command.CommandText = """
                    SELECT clt.Entry, ct.name, clt.Chance, clt.MinCount, clt.MaxCount
                    FROM creature_loot_template clt
                    LEFT JOIN creature_template ct ON ct.entry = clt.Entry
                    WHERE clt.Item = @item
                    """;
            }
            else
            {
                command.CommandText = $"""
                    SELECT Entry, NULL, Chance, MinCount, MaxCount
                    FROM {table}
                    WHERE Item = @item
                    """;
            }
            command.Parameters.AddWithValue("@item", itemId);

            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DropSource(
                    table,
                    reader.GetInt32(0),
                    reader.IsDBNull(1) ? null : reader.GetString(1),
                    reader.GetFloat(2),
                    reader.GetByte(3),
                    reader.GetByte(4)));
            }
        }

        // Resolve indirect drops: a loot-table row with Item = 0 and a nonzero Reference points
        // at reference_loot_template.Entry, a shared loot group reused across many sources.
        await using (var refItemCommand = connection.CreateCommand())
        {
            refItemCommand.CommandText = "SELECT DISTINCT Entry FROM reference_loot_template WHERE Item = @item";
            refItemCommand.Parameters.AddWithValue("@item", itemId);

            var referenceIds = new List<int>();
            await using (var reader = await refItemCommand.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    referenceIds.Add(reader.GetInt32(0));
                }
            }

            foreach (var referenceId in referenceIds)
            {
                foreach (var table in DirectLootTables)
                {
                    await using var command = connection.CreateCommand();
                    if (table == "creature_loot_template")
                    {
                        command.CommandText = """
                            SELECT clt.Entry, ct.name, clt.Chance, clt.MinCount, clt.MaxCount
                            FROM creature_loot_template clt
                            LEFT JOIN creature_template ct ON ct.entry = clt.Entry
                            WHERE clt.Item = 0 AND clt.Reference = @referenceId
                            """;
                    }
                    else
                    {
                        command.CommandText = $"""
                            SELECT Entry, NULL, Chance, MinCount, MaxCount
                            FROM {table}
                            WHERE Item = 0 AND Reference = @referenceId
                            """;
                    }
                    command.Parameters.AddWithValue("@referenceId", referenceId);

                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        results.Add(new DropSource(
                            table,
                            reader.GetInt32(0),
                            reader.IsDBNull(1) ? null : reader.GetString(1),
                            reader.GetFloat(2),
                            reader.GetByte(3),
                            reader.GetByte(4)));
                    }
                }
            }
        }

        return results;
    }

    public async Task<IReadOnlyDictionary<int, ItemSearchResult>> FindItemsAsync(IReadOnlyCollection<int> itemIds)
    {
        if (itemIds.Count == 0)
        {
            return new Dictionary<int, ItemSearchResult>();
        }

        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        var parameterNames = itemIds.Select((_, i) => $"@id{i}").ToArray();
        command.CommandText = $"""
            SELECT entry, name, Quality, displayid, ItemLevel
            FROM item_template
            WHERE entry IN ({string.Join(",", parameterNames)})
            """;
        var idList = itemIds.ToArray();
        for (var i = 0; i < idList.Length; i++)
        {
            command.Parameters.AddWithValue(parameterNames[i], idList[i]);
        }

        var results = new Dictionary<int, ItemSearchResult>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var entry = reader.GetInt32(0);
            results[entry] = new ItemSearchResult(entry, reader.GetString(1), reader.GetByte(2), reader.GetInt32(3), reader.GetInt32(4));
        }

        return results;
    }
}
