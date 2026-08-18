using AzerothWebUI.Core.Domain;
using MySqlConnector;

namespace AzerothWebUI.Core.Data;

public class AhBotRepository(string worldConnectionString)
{
    // mod_auctionhousebot's column-name suffix per WoW item quality tier, in display order.
    private static readonly (string Suffix, Func<AhBotHouse, AhBotQualitySettings> Select)[] Qualities =
    [
        ("grey", h => h.Grey),
        ("white", h => h.White),
        ("green", h => h.Green),
        ("blue", h => h.Blue),
        ("purple", h => h.Purple),
        ("orange", h => h.Orange),
        ("yellow", h => h.Yellow),
    ];

    public async Task<IReadOnlyList<AhBotHouse>> ListHousesAsync()
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT * FROM mod_auctionhousebot ORDER BY auctionhouse";

        var results = new List<AhBotHouse>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(ReadHouse(reader));
        }

        return results;
    }

    public async Task<bool> UpdateHouseAsync(int auctionHouse, AhBotHouse settings)
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        var setClauses = new List<string> { "minitems = @minitems", "maxitems = @maxitems",
            "buyerbiddinginterval = @buyerbiddinginterval", "buyerbidsperinterval = @buyerbidsperinterval" };
        command.Parameters.AddWithValue("@minitems", settings.MinItems);
        command.Parameters.AddWithValue("@maxitems", settings.MaxItems);
        command.Parameters.AddWithValue("@buyerbiddinginterval", settings.BuyerBiddingInterval);
        command.Parameters.AddWithValue("@buyerbidsperinterval", settings.BuyerBidsPerInterval);

        foreach (var (suffix, select) in Qualities)
        {
            var q = select(settings);
            setClauses.Add($"percent{suffix}tradegoods = @percent{suffix}tradegoods");
            setClauses.Add($"percent{suffix}items = @percent{suffix}items");
            setClauses.Add($"minprice{suffix} = @minprice{suffix}");
            setClauses.Add($"maxprice{suffix} = @maxprice{suffix}");
            setClauses.Add($"minbidprice{suffix} = @minbidprice{suffix}");
            setClauses.Add($"maxbidprice{suffix} = @maxbidprice{suffix}");
            setClauses.Add($"maxstack{suffix} = @maxstack{suffix}");
            setClauses.Add($"buyerprice{suffix} = @buyerprice{suffix}");

            command.Parameters.AddWithValue($"@percent{suffix}tradegoods", q.PercentTradeGoods);
            command.Parameters.AddWithValue($"@percent{suffix}items", q.PercentItems);
            command.Parameters.AddWithValue($"@minprice{suffix}", q.MinPrice);
            command.Parameters.AddWithValue($"@maxprice{suffix}", q.MaxPrice);
            command.Parameters.AddWithValue($"@minbidprice{suffix}", q.MinBidPrice);
            command.Parameters.AddWithValue($"@maxbidprice{suffix}", q.MaxBidPrice);
            command.Parameters.AddWithValue($"@maxstack{suffix}", q.MaxStack);
            command.Parameters.AddWithValue($"@buyerprice{suffix}", q.BuyerPrice);
        }

        command.CommandText = $"UPDATE mod_auctionhousebot SET {string.Join(", ", setClauses)} WHERE auctionhouse = @auctionhouse";
        command.Parameters.AddWithValue("@auctionhouse", auctionHouse);

        var rowsAffected = await command.ExecuteNonQueryAsync();
        return rowsAffected > 0;
    }

    public async Task<IReadOnlyList<AhBotDisabledItem>> ListDisabledItemsAsync()
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT d.item, it.name
            FROM mod_auctionhousebot_disabled_items d
            LEFT JOIN item_template it ON it.entry = d.item
            ORDER BY d.item
            """;

        var results = new List<AhBotDisabledItem>();
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new AhBotDisabledItem(reader.GetInt32(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return results;
    }

    public async Task AddDisabledItemAsync(int itemId)
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "INSERT IGNORE INTO mod_auctionhousebot_disabled_items (item) VALUES (@item)";
        command.Parameters.AddWithValue("@item", itemId);
        await command.ExecuteNonQueryAsync();
    }

    public async Task RemoveDisabledItemAsync(int itemId)
    {
        await using var connection = new MySqlConnection(worldConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM mod_auctionhousebot_disabled_items WHERE item = @item";
        command.Parameters.AddWithValue("@item", itemId);
        await command.ExecuteNonQueryAsync();
    }

    private static AhBotHouse ReadHouse(MySqlDataReader reader)
    {
        AhBotQualitySettings ReadQuality(string suffix) => new(
            reader.GetInt32($"percent{suffix}tradegoods"),
            reader.GetInt32($"percent{suffix}items"),
            reader.GetInt32($"minprice{suffix}"),
            reader.GetInt32($"maxprice{suffix}"),
            reader.GetInt32($"minbidprice{suffix}"),
            reader.GetInt32($"maxbidprice{suffix}"),
            reader.GetInt32($"maxstack{suffix}"),
            reader.GetInt32($"buyerprice{suffix}"));

        return new AhBotHouse(
            reader.GetInt32("auctionhouse"),
            reader.IsDBNull(reader.GetOrdinal("name")) ? string.Empty : reader.GetString("name"),
            reader.GetInt32("minitems"),
            reader.GetInt32("maxitems"),
            reader.GetInt32("buyerbiddinginterval"),
            reader.GetInt32("buyerbidsperinterval"),
            ReadQuality("grey"),
            ReadQuality("white"),
            ReadQuality("green"),
            ReadQuality("blue"),
            ReadQuality("purple"),
            ReadQuality("orange"),
            ReadQuality("yellow"));
    }
}
