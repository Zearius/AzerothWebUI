using AzerothWebUI.Core.Domain;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// The fixed, known set of config files this app can edit. Deliberately a static
/// list, not user-configurable/discoverable — CLAUDE.md already treats "which
/// modules exist" as fixed for this app's scope.
/// </summary>
public static class ConfigFileRegistry
{
    public sealed record Entry(
        ConfigFileDescriptor Descriptor,
        string? ModuleFileName,
        Func<string, IReadOnlyList<ConfigEntry>> Parser);

    /// <summary>
    /// worldserver.conf has ModuleFileName = null; its path comes from the
    /// dedicated AzerothCore:WorldserverConfigPath setting. Every other entry's
    /// path is AzerothCore:ModuleConfigDirectory + ModuleFileName.
    /// </summary>
    public static readonly IReadOnlyList<Entry> Files =
    [
        new(new ConfigFileDescriptor("worldserver", "worldserver.conf", AlwaysRestartRequired: false),
            null, WorldserverConfigParser.Parse),
        new(new ConfigFileDescriptor("playerbots", "playerbots.conf", AlwaysRestartRequired: true),
            "playerbots.conf", LooseConfigParser.Parse),
        new(new ConfigFileDescriptor("mod_ahbot", "mod_ahbot.conf (Auction House Bot)", AlwaysRestartRequired: true),
            "mod_ahbot.conf", DecoupledConfigParser.Parse),
        new(new ConfigFileDescriptor("mod_talentbutton", "mod_talentbutton.conf (Talent Button)", AlwaysRestartRequired: true),
            "mod_talentbutton.conf", WorldserverConfigParser.Parse),
        new(new ConfigFileDescriptor("mod_ale", "mod_ale.conf (ALE Lua Engine)", AlwaysRestartRequired: true),
            "mod_ale.conf", DecoupledConfigParser.Parse),
        new(new ConfigFileDescriptor("mod_aoe_loot", "mod_aoe_loot.conf (AOE Loot)", AlwaysRestartRequired: true),
            "mod_aoe_loot.conf", WorldserverConfigParser.Parse),
    ];

    public static Entry? Find(string id) =>
        Files.FirstOrDefault(f => string.Equals(f.Descriptor.Id, id, StringComparison.OrdinalIgnoreCase));
}
