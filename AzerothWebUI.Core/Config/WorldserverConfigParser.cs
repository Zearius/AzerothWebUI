using System.Text.RegularExpressions;
using AzerothWebUI.Core.Domain;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// Parses worldserver.conf's flat "key = value" lines plus their preceding #-comment
/// metadata blocks (Description:/Default:) into structured entries. Pure/no I/O so it's
/// easily unit-testable against fixture text.
/// </summary>
public static partial class WorldserverConfigParser
{
    [GeneratedRegex(@"^\s*([A-Za-z0-9_.]+)\s*=\s*(.+?)\s*$")]
    private static partial Regex AssignmentRegex();

    [GeneratedRegex(@"^#\s*Description:\s*(.*)$")]
    private static partial Regex DescriptionRegex();

    [GeneratedRegex(@"^#\s*Default:\s*(.*)$")]
    private static partial Regex DefaultRegex();

    [GeneratedRegex(@"^#\s*(Example|Important):")]
    private static partial Regex OtherLabelRegex();

    // A section divider: a long run of '#' characters, e.g. "###################...###".
    // Distinct from a lone "#" used as a spacer between comment blocks, which is only
    // 1-2 characters long.
    [GeneratedRegex(@"^#{10,}\s*$")]
    private static partial Regex DividerLineRegex();

    // Matches one "value - (label)" or bare "value" default line, e.g.
    // `1 - (Enabled)` or `"."` or `1000`.
    [GeneratedRegex(@"^([^-]+?)(?:\s*-\s*\(([^)]*)\))?$")]
    private static partial Regex DefaultLineRegex();

    // A bare "#    KeyName" header line preceding Description:/Default: — a comment
    // block can name more than one key this way when it documents a group together
    // (e.g. LoginDatabase.WorkerThreads / WorldDatabase.WorkerThreads / ...).
    [GeneratedRegex(@"^#\s{2,}([A-Za-z0-9_.]+)\s*$")]
    private static partial Regex KeyHeaderRegex();

    private enum CommentMode { None, Description, Default }

    public static IReadOnlyList<ConfigEntry> Parse(string fileContent)
    {
        var entries = new List<ConfigEntry>();
        var lines = fileContent.Replace("\r\n", "\n").Split('\n');

        var descriptionLines = new List<string>();
        var defaultLines = new List<string>();
        var pendingKeyNames = new List<string>();
        var appliedKeyCount = 0;
        var mode = CommentMode.None;
        var section = string.Empty;
        var previousLineWasDivider = false;

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith('#'))
            {
                if (DividerLineRegex().IsMatch(line))
                {
                    previousLineWasDivider = true;

                    // A divider always starts a fresh section — discard any dangling
                    // key-header names that were never followed by a Description:/Default:
                    // (e.g. the file's own SECTION INDEX block, which lists many bare
                    // section names that look like single-word key headers).
                    if (mode == CommentMode.None)
                    {
                        pendingKeyNames.Clear();
                    }

                    continue;
                }

                if (previousLineWasDivider)
                {
                    // The line right after a divider is a section banner, e.g. "# CONSOLE".
                    section = line.TrimStart('#', ' ').Trim();
                    previousLineWasDivider = false;
                    continue;
                }

                previousLineWasDivider = false;

                var descMatch = DescriptionRegex().Match(line);
                var defaultMatch = DefaultRegex().Match(line);
                var keyHeaderMatch = KeyHeaderRegex().Match(line);

                if (mode == CommentMode.None && keyHeaderMatch.Success)
                {
                    pendingKeyNames.Add(keyHeaderMatch.Groups[1].Value);
                }
                else if (descMatch.Success)
                {
                    descriptionLines.Add(descMatch.Groups[1].Value.Trim());
                    mode = CommentMode.Description;
                }
                else if (defaultMatch.Success)
                {
                    defaultLines.Add(defaultMatch.Groups[1].Value.Trim());
                    mode = CommentMode.Default;
                }
                else if (OtherLabelRegex().IsMatch(line))
                {
                    mode = CommentMode.None;
                }
                else
                {
                    var content = line.TrimStart('#', ' ').Trim();
                    if (content.Length > 0 && mode == CommentMode.Description)
                    {
                        descriptionLines.Add(content);
                    }
                    else if (content.Length > 0 && mode == CommentMode.Default)
                    {
                        defaultLines.Add(content);
                    }
                }

                continue;
            }

            previousLineWasDivider = false;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var assignment = AssignmentRegex().Match(line);
            if (!assignment.Success)
            {
                // A non-comment, non-assignment, non-blank line (e.g. "[worldserver]")
                // ends any in-progress comment block without producing an entry.
                descriptionLines.Clear();
                defaultLines.Clear();
                pendingKeyNames.Clear();
                appliedKeyCount = 0;
                mode = CommentMode.None;
                continue;
            }

            var key = assignment.Groups[1].Value;
            var value = assignment.Groups[2].Value;

            var defaults = ParseDefaultOptions(defaultLines);
            var isToggle = defaults.Count > 0
                && defaults.All(d => d.Value is "0" or "1")
                && value is "0" or "1";

            entries.Add(new ConfigEntry(
                Key: key,
                CurrentValue: value,
                Section: section,
                Description: string.Join(' ', descriptionLines),
                Defaults: defaults,
                IsToggle: isToggle));

            appliedKeyCount++;

            // A shared comment block (multiple "#    KeyName" headers before one
            // Description:/Default:) applies to as many consecutive assignments as it
            // named keys — only clear the buffered metadata once all are consumed.
            if (appliedKeyCount >= Math.Max(pendingKeyNames.Count, 1))
            {
                descriptionLines.Clear();
                defaultLines.Clear();
                pendingKeyNames.Clear();
                appliedKeyCount = 0;
                mode = CommentMode.None;
            }
        }

        return entries;
    }

    private static List<ConfigDefaultOption> ParseDefaultOptions(List<string> defaultLines)
    {
        var options = new List<ConfigDefaultOption>();
        foreach (var line in defaultLines)
        {
            if (line.Length == 0)
            {
                continue;
            }

            var match = DefaultLineRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var value = match.Groups[1].Value.Trim().Trim('"');
            var label = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            options.Add(new ConfigDefaultOption(value, label));
        }

        return options;
    }
}
