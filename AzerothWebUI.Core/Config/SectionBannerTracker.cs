using System.Text.RegularExpressions;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// Tracks the current "# SECTION NAME" banner as a parser walks a .conf file line by
/// line. A real section banner is always the comment line immediately after a long
/// divider line (a run of '#' characters), which distinguishes it from a lone "#"
/// spacer used between individual comment blocks. Shared by any parser that needs
/// section grouping, since this format (and its "SECTION INDEX" false-positive trap —
/// see WorldserverConfigParserTests' regression test) appears in multiple config files.
/// </summary>
public partial class SectionBannerTracker
{
    // A section divider: a long run of '#' characters, e.g. "###################...###".
    // Distinct from a lone "#" used as a spacer between comment blocks, which is only
    // 1-2 characters long.
    [GeneratedRegex(@"^#{10,}\s*$")]
    private static partial Regex DividerLineRegex();

    private bool _previousLineWasDivider;

    public string Section { get; private set; } = string.Empty;

    /// <summary>
    /// Feed one line (already known to start with '#') to the tracker. Returns true if
    /// this line was consumed as divider/banner bookkeeping and the caller should skip
    /// any further processing of it; false if the caller should continue handling the
    /// line as ordinary comment content.
    /// </summary>
    public bool TryConsume(string commentLine)
    {
        if (DividerLineRegex().IsMatch(commentLine))
        {
            _previousLineWasDivider = true;
            return true;
        }

        if (_previousLineWasDivider)
        {
            var text = commentLine.Trim('#', ' ').Trim();
            if (text.Length == 0)
            {
                // A padding line inside a "boxed" banner (e.g. "#          #") —
                // keep waiting for the real title line rather than treating this
                // as the section name.
                return true;
            }

            Section = text;
            _previousLineWasDivider = false;
            return true;
        }

        _previousLineWasDivider = false;
        return false;
    }

    public void ResetDividerState() => _previousLineWasDivider = false;
}
