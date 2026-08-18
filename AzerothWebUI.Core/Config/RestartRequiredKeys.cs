namespace AzerothWebUI.Core.Config;

/// <summary>
/// worldserver.conf keys that do NOT take effect via the SOAP/console "reload config"
/// command, even though most other keys do (AzerothCore's World::LoadConfigSettings
/// re-reads the file live). Verified against AzerothCore's own source
/// (WorldConfig.cpp's Reloadable::No tags, World.cpp's reload guards) rather than
/// inferred from the .conf file's comments, which are unreliable here — see the
/// "doc/code mismatch" entries below.
/// </summary>
public static class RestartRequiredKeys
{
    public static readonly IReadOnlySet<string> Keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Explicitly tagged Reloadable::No in WorldConfig.cpp, or guarded directly in
        // World.cpp's LoadConfigSettings (DataDir, PlayerLimit).
        "DataDir",
        "PlayerLimit",
        "WorldServerPort",
        "GameType",
        "RealmZone",
        "MaxPlayerLevel",
        "Expansion",
        "Wintergrasp.KickVoAPlayers",
        "AuctionHouse.WorkerThreads",

        // NOT tagged Reloadable::No in code (so a reload would silently accept the new
        // value into the cache), but confirmed restart-required either by the .conf
        // file's own comment ("It is necessary to restart the server after changing
        // this value") or by tracing the consuming code (MapUpdate.Threads only sizes
        // the map-update thread pool once, at MapMgr::Initialize()).
        "Battleground.Alterac.Reinforcements",
        "Battleground.Alterac.ReputationOnBossDeath",
        "ICC.Buff.Horde",
        "ICC.Buff.Alliance",
        "MapUpdate.Threads",
    };
}
