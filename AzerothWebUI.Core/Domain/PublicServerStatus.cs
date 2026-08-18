using System.Text.RegularExpressions;

namespace AzerothWebUI.Core.Domain;

public record PublicServerStatus(int PlayersOnline, int CharactersInWorld, string? Uptime);

/// <summary>
/// Extracts a small, safe subset of AzerothCore's "server info" SOAP output for public display -
/// deliberately drops the revision hash, build flavor, connection peak, and diff-timing
/// percentiles, which aren't meaningful to a public visitor and needlessly expose server
/// internals.
/// </summary>
public static partial class PublicServerStatusParser
{
    public static PublicServerStatus Parse(string rawOutput)
    {
        var players = PlayersRegex().Match(rawOutput);
        var characters = CharactersRegex().Match(rawOutput);
        var uptime = UptimeRegex().Match(rawOutput);

        return new PublicServerStatus(
            players.Success ? int.Parse(players.Groups[1].Value) : 0,
            characters.Success ? int.Parse(characters.Groups[1].Value) : 0,
            uptime.Success ? uptime.Groups[1].Value.Trim() : null);
    }

    [GeneratedRegex(@"Connected players:\s*(\d+)")]
    private static partial Regex PlayersRegex();

    [GeneratedRegex(@"Characters in world:\s*(\d+)")]
    private static partial Regex CharactersRegex();

    [GeneratedRegex(@"Server uptime:\s*(.+)")]
    private static partial Regex UptimeRegex();
}
