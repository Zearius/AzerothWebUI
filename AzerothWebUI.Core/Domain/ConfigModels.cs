namespace AzerothWebUI.Core.Domain;

public record ConfigDefaultOption(string Value, string? Label);

public record ConfigEntry(
    string Key,
    string CurrentValue,
    string Section,
    string Description,
    IReadOnlyList<ConfigDefaultOption> Defaults,
    bool IsToggle,
    string SourceFile = "",
    bool RequiresRestart = false);

public record ConfigFileDescriptor(string Id, string DisplayName, bool AlwaysRestartRequired);

public record UpdateConfigValueRequest(string Value);
