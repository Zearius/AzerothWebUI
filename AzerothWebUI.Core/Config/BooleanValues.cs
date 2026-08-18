namespace AzerothWebUI.Core.Config;

/// <summary>
/// Config files in this codebase use two different boolean vocabularies: 0/1
/// (worldserver.conf and most modules) and true/false (mod_ale.conf). Toggle
/// inference and value-flipping need to recognize both without assuming one.
/// </summary>
public static class BooleanValues
{
    private static readonly (string False, string True)[] Pairs =
    [
        ("0", "1"),
        ("false", "true"),
    ];

    public static bool IsBooleanValue(string value) =>
        Pairs.Any(p => string.Equals(p.False, value, StringComparison.OrdinalIgnoreCase)
            || string.Equals(p.True, value, StringComparison.OrdinalIgnoreCase));

    public static bool IsBooleanPair(IEnumerable<string> values)
    {
        var distinct = values.Select(v => v.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (distinct.Count is < 1 or > 2)
        {
            return false;
        }

        return distinct.All(IsBooleanValue);
    }

    /// <summary>
    /// Flips a boolean value while preserving whichever vocabulary it already used
    /// (e.g. "true" flips to "false", not "0").
    /// </summary>
    public static string Flip(string currentValue)
    {
        foreach (var (falseValue, trueValue) in Pairs)
        {
            if (string.Equals(currentValue, trueValue, StringComparison.OrdinalIgnoreCase))
            {
                return falseValue;
            }

            if (string.Equals(currentValue, falseValue, StringComparison.OrdinalIgnoreCase))
            {
                return trueValue;
            }
        }

        throw new ArgumentException($"'{currentValue}' is not a recognized boolean value.", nameof(currentValue));
    }
}
