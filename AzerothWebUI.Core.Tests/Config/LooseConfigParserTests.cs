using AzerothWebUI.Core.Config;

namespace AzerothWebUI.Core.Tests.Config;

public class LooseConfigParserTests
{
    [Fact]
    public void Parse_PlainCommentLine_BecomesDescription()
    {
        const string fixture = "# Enable or disable Playerbots module\nAiPlayerbot.Enabled = 1\n";

        var entries = LooseConfigParser.Parse(fixture);

        var entry = Assert.Single(entries);
        Assert.Equal("AiPlayerbot.Enabled", entry.Key);
        Assert.Equal("Enable or disable Playerbots module", entry.Description);
        Assert.True(entry.IsToggle);
    }

    [Fact]
    public void Parse_CommentedOutExampleLine_IsSkipped()
    {
        const string fixture = """
            # Randombot count
            #AiPlayerbot.MinRandomBots = 500
            #AiPlayerbot.MaxRandomBots = 500

            AiPlayerbot.RandomBotAccountCount = 0
            """;

        var entries = LooseConfigParser.Parse(fixture);

        Assert.DoesNotContain(entries, e => e.Key == "AiPlayerbot.MinRandomBots");
        Assert.DoesNotContain(entries, e => e.Key == "AiPlayerbot.MaxRandomBots");
        var entry = Assert.Single(entries);
        Assert.Equal("AiPlayerbot.RandomBotAccountCount", entry.Key);
        // Blank line before it severs the "Randombot count" comment from this key.
        Assert.Equal(string.Empty, entry.Description);
    }

    [Fact]
    public void Parse_FreeStandingPreamble_NotAttributedToFirstKey()
    {
        const string fixture = """
            # Overview
            # This describes the whole file, not any one key.

            ####################################################################################################
            # SECTION INDEX
            #   GENERAL SETTINGS
            ####################################################################################################

            # Enable or disable Playerbots module
            AiPlayerbot.Enabled = 1
            """;

        var entries = LooseConfigParser.Parse(fixture);

        var entry = Assert.Single(entries);
        Assert.Equal("Enable or disable Playerbots module", entry.Description);
        Assert.DoesNotContain("Overview", entry.Description);
    }

    [Fact]
    public void Parse_MultiLineComment_JoinedWithSpaces()
    {
        const string fixture = """
            # Disable randombots when no real players are logged in
            # Default: 0 (randombots will login when server starts)
            AiPlayerbot.DisabledWithoutRealPlayer = 0
            """;

        var entries = LooseConfigParser.Parse(fixture);

        var entry = Assert.Single(entries);
        Assert.Equal(
            "Disable randombots when no real players are logged in Default: 0 (randombots will login when server starts)",
            entry.Description);
    }

    [Fact]
    public void Parse_SectionIndexBlock_DoesNotBleedIntoLaterKeys()
    {
        const string fixture = """
            ####################################################################################################
            # SECTION INDEX
            #   GENERAL SETTINGS
            #    PLAYERBOTS SETTINGS
            ####################################################################################################

            ###################################
            #                                 #
            # GENERAL SETTINGS                #
            #                                 #
            ###################################

            # Enable or disable Playerbots module
            AiPlayerbot.Enabled = 1
            """;

        var entries = LooseConfigParser.Parse(fixture);

        var entry = Assert.Single(entries);
        Assert.Equal("Enable or disable Playerbots module", entry.Description);
        Assert.Equal("GENERAL SETTINGS", entry.Section);
    }
}
