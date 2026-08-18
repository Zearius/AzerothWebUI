namespace AzerothWebUI.Core.Domain;

public record CharacterSummary(int Guid, string Name, byte Race, byte Class, byte Level, string? GuildName, bool Online);

public record EquippedItem(byte Slot, int ItemEntry, string Name, byte Quality, int DisplayId, int ItemLevel);

public record CharacterDetail(
    int Guid,
    string Name,
    byte Race,
    byte Class,
    byte Gender,
    byte Level,
    string? GuildName,
    bool Online,
    IReadOnlyList<EquippedItem> EquippedItems);

public record ItemStat(byte Type, int Value);

public record ItemDetail(
    int Entry,
    string Name,
    byte Quality,
    int DisplayId,
    int ItemLevel,
    byte RequiredLevel,
    byte Class,
    byte Subclass,
    byte InventoryType,
    string? Description,
    IReadOnlyList<ItemStat> Stats);

public record ItemSearchResult(int Entry, string Name, byte Quality, int DisplayId, int ItemLevel);

public record DropSource(string SourceType, int SourceEntry, string? SourceName, float Chance, byte MinCount, byte MaxCount);
