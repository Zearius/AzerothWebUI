namespace AzerothWebUI.Core.Domain;

/// <summary>
/// mod_auctionhousebot's per-quality-tier rate/price settings for one auction house. Column
/// names mirror the table's "{field}{quality}" convention (e.g. minpricegrey) grouped here by
/// quality instead of one flat 59-column record, since every field in the table repeats once
/// per WoW item-quality tier (grey/white/green/blue/purple/orange/yellow).
/// </summary>
public record AhBotQualitySettings(
    int PercentTradeGoods,
    int PercentItems,
    int MinPrice,
    int MaxPrice,
    int MinBidPrice,
    int MaxBidPrice,
    int MaxStack,
    int BuyerPrice);

public record AhBotHouse(
    int AuctionHouse,
    string Name,
    int MinItems,
    int MaxItems,
    int BuyerBiddingInterval,
    int BuyerBidsPerInterval,
    AhBotQualitySettings Grey,
    AhBotQualitySettings White,
    AhBotQualitySettings Green,
    AhBotQualitySettings Blue,
    AhBotQualitySettings Purple,
    AhBotQualitySettings Orange,
    AhBotQualitySettings Yellow);

public record AhBotDisabledItem(int ItemId, string? ItemName);
