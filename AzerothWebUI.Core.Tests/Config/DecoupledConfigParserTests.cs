using AzerothWebUI.Core.Config;

namespace AzerothWebUI.Core.Tests.Config;

public class DecoupledConfigParserTests
{
    private const string BareStyleFixture = """
        [worldserver]

        ###############################################################################
        # AUCTION HOUSE BOT SETTINGS
        #
        #    AuctionHouseBot.DEBUG
        #        Enable/Disable Debugging output
        #    Default 0 (disabled)
        #
        #    AuctionHouseBot.EnableSeller
        #        Enable/Disable the part of AHBot that puts items up for auction
        #    Default 0 (disabled)
        #
        #    AuctionHouseBot.MarketResetThreshold
        #        How many auctions of the same item are necessary before the price is adopted.
        #    Default 25
        #
        ###############################################################################

        AuctionHouseBot.DEBUG = 0
        AuctionHouseBot.EnableSeller = 1
        AuctionHouseBot.MarketResetThreshold = 50

        ###############################################################################
        # AUCTION HOUSE BOT FILTERS PART 2
        #
        #     These Filters are boolean (0 or 1).
        #     Default 0 (allowed)
        #
        ###############################################################################

        AuctionHouseBot.DisableWarriorItems = 0
        """;

    [Fact]
    public void Parse_BareStyle_JoinsMetadataByKeyName_NotPosition()
    {
        var entries = DecoupledConfigParser.Parse(BareStyleFixture);

        var debug = entries.Single(e => e.Key == "AuctionHouseBot.DEBUG");
        Assert.Equal("Enable/Disable Debugging output", debug.Description);

        var seller = entries.Single(e => e.Key == "AuctionHouseBot.EnableSeller");
        Assert.Equal("Enable/Disable the part of AHBot that puts items up for auction", seller.Description);
    }

    [Fact]
    public void Parse_BareStyle_TogglesBasedOnCurrentValue()
    {
        var entries = DecoupledConfigParser.Parse(BareStyleFixture);

        var debug = entries.Single(e => e.Key == "AuctionHouseBot.DEBUG");
        Assert.True(debug.IsToggle);

        var threshold = entries.Single(e => e.Key == "AuctionHouseBot.MarketResetThreshold");
        Assert.False(threshold.IsToggle);
    }

    [Fact]
    public void Parse_BareStyle_KeyWithNoAssignmentLine_IsNotEmittedAsEntry()
    {
        var entries = DecoupledConfigParser.Parse(BareStyleFixture);

        // MarketResetThreshold's header exists but so does its assignment (=50) —
        // confirm we don't duplicate or fabricate entries for headers alone.
        Assert.Single(entries, e => e.Key == "AuctionHouseBot.MarketResetThreshold");
    }

    [Fact]
    public void Parse_BareStyle_UnmatchedAssignment_HasEmptyDescriptionNotAnException()
    {
        var entries = DecoupledConfigParser.Parse(BareStyleFixture);

        var warrior = entries.Single(e => e.Key == "AuctionHouseBot.DisableWarriorItems");
        Assert.Equal(string.Empty, warrior.Description);
    }

    [Fact]
    public void Parse_BareStyle_AssignsCorrectSectionPerRegion()
    {
        var entries = DecoupledConfigParser.Parse(BareStyleFixture);

        var debug = entries.Single(e => e.Key == "AuctionHouseBot.DEBUG");
        Assert.Equal("AUCTION HOUSE BOT SETTINGS", debug.Section);

        var warrior = entries.Single(e => e.Key == "AuctionHouseBot.DisableWarriorItems");
        Assert.Equal("AUCTION HOUSE BOT FILTERS PART 2", warrior.Section);
    }

    [Fact]
    public void Parse_BareStyle_KeyHeaderWithNoDefaultLine_DoesNotBleedIntoNextKey()
    {
        // Regression test: AuctionHouseBot.DisabledItems has no "Default" line at
        // all before the next key header starts — the flush must trigger on seeing
        // accumulated description content, not only on seeing a Default: line.
        const string fixture = """
            ###############################################################################
            # AUCTION HOUSE BOT FILTERS PART 3
            #
            #    AuctionHouseBot.DisabledItems
            #        Prevent Seller from listing specific item(s)
            #        (not used anymore, see table "mod_auctionhousebot_disabled_items")
            #
            #    AuctionHouseBot.DisableItemsBelowLevel
            #        Prevent Seller from listing Items below this Level
            #    Default 0 (Off)
            #
            ###############################################################################

            AuctionHouseBot.DisableItemsBelowLevel = 0
            """;

        var entries = DecoupledConfigParser.Parse(fixture);

        var entry = entries.Single(e => e.Key == "AuctionHouseBot.DisableItemsBelowLevel");
        Assert.Equal("Prevent Seller from listing Items below this Level", entry.Description);
    }

    private const string LabeledStyleFixture = """
        [worldserver]

        ###################################################################################################
        #  ALE SETTINGS
        #
        #   ALE.Enabled
        #       Description: Enable or disable ALE LuaEngine
        #       Default:    true  - (enabled)
        #                   false - (disabled)
        #
        #   ALE.TraceBack
        #       Description: Sets whether to use debug.traceback function on a lua error or not.
        #                    Notice that you can redefine the function.
        #       Default:    false - (use default error output)
        #                   true  - (use debug.traceback function)
        #
        #   ALE.ScriptPath
        #       Description: Sets the location of the script folder to load scripts from
        #       Default:    "lua_scripts"
        #

        ALE.Enabled = true
        ALE.TraceBack = false
        ALE.ScriptPath = "/azerothcore/env/dist/etc/modules/lua_scripts"
        """;

    [Fact]
    public void Parse_LabeledStyle_JoinsMetadataByKeyName()
    {
        var entries = DecoupledConfigParser.Parse(LabeledStyleFixture);

        var traceback = entries.Single(e => e.Key == "ALE.TraceBack");
        Assert.Equal(
            "Sets whether to use debug.traceback function on a lua error or not. Notice that you can redefine the function.",
            traceback.Description);
    }

    [Fact]
    public void Parse_LabeledStyle_TrueFalseValuesAreRecognizedAsToggle()
    {
        var entries = DecoupledConfigParser.Parse(LabeledStyleFixture);

        var enabled = entries.Single(e => e.Key == "ALE.Enabled");
        Assert.True(enabled.IsToggle);
        Assert.Equal("true", enabled.CurrentValue);
    }

    [Fact]
    public void Parse_LabeledStyle_NonBooleanDefault_IsNotToggle()
    {
        var entries = DecoupledConfigParser.Parse(LabeledStyleFixture);

        var scriptPath = entries.Single(e => e.Key == "ALE.ScriptPath");
        Assert.False(scriptPath.IsToggle);
    }
}
