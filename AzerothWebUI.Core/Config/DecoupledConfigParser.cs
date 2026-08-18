using System.Text.RegularExpressions;
using AzerothWebUI.Core.Domain;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// Parses config files where comment blocks documenting keys by name
/// ("#    KeyName" header, then either bare prose + "Default N (note)" as in
/// mod_ahbot.conf, or labeled "Description:"/"Default:" lines as in
/// mod_ale.conf) are completely decoupled from the "Key = Value" assignment
/// lines, which appear later in the file — sometimes not 1:1 with the headers,
/// sometimes not at all. Metadata is looked up by key name, not by position,
/// since the header/assignment ordering isn't reliably parallel in these files.
/// </summary>
public static partial class DecoupledConfigParser
{
    [GeneratedRegex(@"^\s*([A-Za-z0-9_.]+)\s*=\s*(.+?)\s*$")]
    private static partial Regex AssignmentRegex();

    // A bare "#    KeyName" header line, same shape as WorldserverConfigParser's.
    [GeneratedRegex(@"^#\s{2,}([A-Za-z0-9_.]+)\s*$")]
    private static partial Regex KeyHeaderRegex();

    [GeneratedRegex(@"^#\s*Description:\s*(.*)$")]
    private static partial Regex LabeledDescriptionRegex();

    [GeneratedRegex(@"^#\s*Default:?\s*(.*)$")]
    private static partial Regex LabeledDefaultRegex();

    [GeneratedRegex(@"^#\s*(Example|Important|Range):")]
    private static partial Regex OtherLabelRegex();

    // Bare "Default N (note)" or "Default: N (note)" with no leading '#' — caller
    // strips the '#' first. Used only when no "Description:"/"Default:" labels are
    // present at all in the file (mod_ahbot.conf's style).
    [GeneratedRegex(@"^Default:?\s+(.*)$")]
    private static partial Regex BareDefaultLineRegex();

    // Matches one default line in any of: "value - (label)", "value (label)", or
    // bare "value" with no label at all.
    [GeneratedRegex(@"^(\S+)\s*(?:-\s*)?\(([^)]*)\)?\s*$|^(\S+)\s*$")]
    private static partial Regex DefaultOptionRegex();

    private sealed record KeyMetadata(string Description, IReadOnlyList<ConfigDefaultOption> Defaults);

    private enum CommentMode { None, Description, Default }

    public static IReadOnlyList<ConfigEntry> Parse(string fileContent)
    {
        var lines = fileContent.Replace("\r\n", "\n").Split('\n');
        var metadataByKey = new Dictionary<string, KeyMetadata>(StringComparer.OrdinalIgnoreCase);
        var sectionTracker = new SectionBannerTracker();
        var sectionByLine = new string[lines.Length];

        var pendingKeyNames = new List<string>();
        var descriptionLines = new List<string>();
        var defaultLines = new List<string>();
        var mode = CommentMode.None;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            sectionByLine[i] = sectionTracker.Section;

            if (!line.StartsWith('#'))
            {
                sectionTracker.ResetDividerState();
                continue;
            }

            if (sectionTracker.TryConsume(line))
            {
                Flush(metadataByKey, pendingKeyNames, descriptionLines, defaultLines);
                pendingKeyNames.Clear();
                descriptionLines.Clear();
                defaultLines.Clear();
                mode = CommentMode.None;
                sectionByLine[i] = sectionTracker.Section;
                continue;
            }

            var content = line.TrimStart('#', ' ').Trim();
            var keyHeaderMatch = KeyHeaderRegex().Match(line);

            if (keyHeaderMatch.Success)
            {
                // A new key header starts a fresh entry unless it's immediately
                // continuing the same group (back-to-back headers with no
                // description/Default: content between them yet, e.g. a shared
                // block naming several keys before their common description).
                if (defaultLines.Count > 0 || descriptionLines.Count > 0)
                {
                    Flush(metadataByKey, pendingKeyNames, descriptionLines, defaultLines);
                    pendingKeyNames.Clear();
                    descriptionLines.Clear();
                    defaultLines.Clear();
                }

                pendingKeyNames.Add(keyHeaderMatch.Groups[1].Value);
                mode = CommentMode.None;
                continue;
            }

            var labeledDescMatch = LabeledDescriptionRegex().Match(line);
            var labeledDefaultMatch = LabeledDefaultRegex().Match(line);
            var bareDefaultMatch = BareDefaultLineRegex().Match(content);

            if (labeledDescMatch.Success)
            {
                descriptionLines.Add(labeledDescMatch.Groups[1].Value.Trim());
                mode = CommentMode.Description;
            }
            else if (labeledDefaultMatch.Success)
            {
                defaultLines.Add(labeledDefaultMatch.Groups[1].Value.Trim());
                mode = CommentMode.Default;
            }
            else if (OtherLabelRegex().IsMatch(line))
            {
                mode = CommentMode.None;
            }
            else if (mode == CommentMode.None && bareDefaultMatch.Success)
            {
                defaultLines.Add(bareDefaultMatch.Groups[1].Value.Trim());
            }
            else if (content.Length > 0 && mode == CommentMode.Description)
            {
                descriptionLines.Add(content);
            }
            else if (content.Length > 0 && mode == CommentMode.Default)
            {
                defaultLines.Add(content);
            }
            else if (content.Length > 0 && mode == CommentMode.None && pendingKeyNames.Count > 0)
            {
                // Bare-style prose (no Description:/Default: labels at all).
                descriptionLines.Add(content);
            }
        }

        Flush(metadataByKey, pendingKeyNames, descriptionLines, defaultLines);

        var entries = new List<ConfigEntry>();
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd();
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var assignment = AssignmentRegex().Match(line);
            if (!assignment.Success)
            {
                continue;
            }

            var key = assignment.Groups[1].Value;
            var value = assignment.Groups[2].Value;
            metadataByKey.TryGetValue(key, out var metadata);

            var defaults = metadata?.Defaults ?? [];
            var isToggle = defaults.Count > 0
                ? BooleanValues.IsBooleanPair(defaults.Select(d => d.Value)) && BooleanValues.IsBooleanValue(value)
                : BooleanValues.IsBooleanValue(value);

            entries.Add(new ConfigEntry(
                Key: key,
                CurrentValue: value,
                Section: sectionByLine[i],
                Description: metadata?.Description ?? string.Empty,
                Defaults: defaults,
                IsToggle: isToggle));
        }

        return entries;
    }

    private static void Flush(
        Dictionary<string, KeyMetadata> metadataByKey,
        List<string> pendingKeyNames,
        List<string> descriptionLines,
        List<string> defaultLines)
    {
        if (pendingKeyNames.Count == 0)
        {
            return;
        }

        var description = string.Join(' ', descriptionLines);
        var defaults = ParseDefaultOptions(defaultLines);
        foreach (var key in pendingKeyNames)
        {
            metadataByKey[key] = new KeyMetadata(description, defaults);
        }
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

            var match = DefaultOptionRegex().Match(line);
            if (!match.Success)
            {
                continue;
            }

            var value = (match.Groups[1].Success ? match.Groups[1].Value : match.Groups[3].Value).Trim().Trim('"');
            var label = match.Groups[2].Success && match.Groups[2].Value.Length > 0
                ? match.Groups[2].Value.Trim()
                : null;
            options.Add(new ConfigDefaultOption(value, label));
        }

        return options;
    }
}
