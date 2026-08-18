using System.Text.RegularExpressions;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// Rewrites a single "key = value" line in worldserver.conf's text, leaving every other
/// line (including all comments/formatting) byte-for-byte untouched. Pure/no I/O.
/// </summary>
public static partial class WorldserverConfigWriter
{
    [GeneratedRegex(@"^(\s*)([A-Za-z0-9_.]+)(\s*=\s*)(.+?)(\s*)$")]
    private static partial Regex AssignmentLineRegex();

    /// <summary>
    /// Returns null if the key was not found (caller should treat as 404), otherwise the
    /// full file content with only that key's line updated.
    /// </summary>
    public static string? SetValue(string fileContent, string key, string newValue)
    {
        var usesCrlf = fileContent.Contains("\r\n");
        var lines = fileContent.Replace("\r\n", "\n").Split('\n');

        var found = false;
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (line.TrimStart().StartsWith('#'))
            {
                continue;
            }

            var match = AssignmentLineRegex().Match(line);
            if (!match.Success || !string.Equals(match.Groups[2].Value, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var originalValue = match.Groups[4].Value;
            var wasQuoted = originalValue.Length >= 2 && originalValue.StartsWith('"') && originalValue.EndsWith('"');
            var formattedValue = wasQuoted ? $"\"{newValue.Trim('"')}\"" : newValue;

            lines[i] = $"{match.Groups[1].Value}{match.Groups[2].Value}{match.Groups[3].Value}{formattedValue}{match.Groups[5].Value}";
            found = true;
            break;
        }

        if (!found)
        {
            return null;
        }

        var newline = usesCrlf ? "\r\n" : "\n";
        return string.Join(newline, lines);
    }
}
