using AzerothWebUI.Core.Data;
using AzerothWebUI.Core.Domain;

namespace AzerothWebUI.Core.Config;

/// <summary>
/// Resolves a known config file id (see ConfigFileRegistry) to its file store and
/// parser, and applies each entry's per-key restart-required classification.
/// </summary>
public class ConfigFileService(string worldserverConfigPath, string moduleConfigDirectory)
{
    public IReadOnlyList<ConfigFileDescriptor> ListFiles() =>
        ConfigFileRegistry.Files.Select(f => f.Descriptor).ToList();

    public ConfigFileRegistry.Entry? FindEntry(string fileId) => ConfigFileRegistry.Find(fileId);

    public IConfigFileStore GetStore(ConfigFileRegistry.Entry entry)
    {
        var path = entry.ModuleFileName is null
            ? worldserverConfigPath
            : Path.Combine(moduleConfigDirectory, entry.ModuleFileName);
        return new FileSystemConfigFileStore(path);
    }

    public async Task<IReadOnlyList<ConfigEntry>> ReadEntriesAsync(ConfigFileRegistry.Entry entry)
    {
        var store = GetStore(entry);
        var content = await store.ReadAllTextAsync();
        var parsed = entry.Parser(content);

        return parsed.Select(e => e with
        {
            SourceFile = entry.Descriptor.DisplayName,
            RequiresRestart = entry.Descriptor.AlwaysRestartRequired || RestartRequiredKeys.Keys.Contains(e.Key),
        }).ToList();
    }
}
