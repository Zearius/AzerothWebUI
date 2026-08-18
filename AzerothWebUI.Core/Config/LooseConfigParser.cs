using System.Text.RegularExpressions;
using AzerothWebUI.Core.Domain;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// Parses playerbots.conf's format: no Description:/Default: labels at all, just
/// plain prose comment lines immediately preceding a key assignment. Reuses
/// SectionBannerTracker for section grouping (this file has its own SECTION INDEX
/// block with the same false-positive-header risk worldserver.conf has).
///
/// Because there's no structured label to anchor on, only comment lines with no
/// blank line between them and the assignment are treated as that key's
/// description — this deliberately excludes free-standing file-level prose (like
/// this file's "# Overview" preamble) that's separated from any key by a blank line.
/// </summary>
public static partial class LooseConfigParser
{
    [GeneratedRegex(@"^\s*([A-Za-z0-9_.]+)\s*=\s*(.+?)\s*$")]
    private static partial Regex AssignmentRegex();

    // A commented-out example assignment, e.g. "#AiPlayerbot.MinRandomBots = 500" —
    // not a real active entry, must be skipped rather than parsed as a key.
    [GeneratedRegex(@"^#\s*[A-Za-z0-9_.]+\s*=")]
    private static partial Regex CommentedOutAssignmentRegex();

    public static IReadOnlyList<ConfigEntry> Parse(string fileContent)
    {
        var entries = new List<ConfigEntry>();
        var lines = fileContent.Replace("\r\n", "\n").Split('\n');
        var sectionTracker = new SectionBannerTracker();

        var descriptionLines = new List<string>();

        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith('#'))
            {
                if (sectionTracker.TryConsume(line))
                {
                    descriptionLines.Clear();
                    continue;
                }

                if (CommentedOutAssignmentRegex().IsMatch(line))
                {
                    // A commented-out example key — not real content, not a
                    // description continuation either.
                    descriptionLines.Clear();
                    continue;
                }

                var content = line.TrimStart('#', ' ').Trim();
                if (content.Length > 0)
                {
                    descriptionLines.Add(content);
                }

                continue;
            }

            sectionTracker.ResetDividerState();

            if (string.IsNullOrWhiteSpace(line))
            {
                // A blank line severs any comment block from whatever key comes
                // next — prevents free-standing prose (e.g. file preambles) from
                // being misattributed as a key's description.
                descriptionLines.Clear();
                continue;
            }

            var assignment = AssignmentRegex().Match(line);
            if (!assignment.Success)
            {
                descriptionLines.Clear();
                continue;
            }

            var key = assignment.Groups[1].Value;
            var value = assignment.Groups[2].Value;

            entries.Add(new ConfigEntry(
                Key: key,
                CurrentValue: value,
                Section: sectionTracker.Section,
                Description: string.Join(' ', descriptionLines),
                Defaults: [],
                IsToggle: BooleanValues.IsBooleanValue(value)));

            descriptionLines.Clear();
        }

        return entries;
    }
}
