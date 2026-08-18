namespace AzerothWebUI.Core.Data;

/// <summary>
/// Abstraction over reading/writing worldserver.conf's raw text. A direct-filesystem
/// implementation is correct for same-host deployments (the common case); the interface
/// exists so a remote (SSH/agent) implementation could be added later without reworking
/// the config parser/writer or API endpoints — not built speculatively.
/// </summary>
public interface IConfigFileStore
{
    Task<string> ReadAllTextAsync();
    Task WriteAllTextAsync(string content);
}
