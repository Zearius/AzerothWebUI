using AzerothWebUI.Core.Config;

namespace AzerothWebUI.Core.Tests.Config;

public class WorldserverConfigParserTests
{
    private const string Fixture = """
        ###################################################################################################
        # DIRECTORIES
        #
        #    DataDir
        #        Description: Data directory setting.
        #        Important:   DataDir needs to be quoted, as the string might contain space characters.
        #        Example:     "@prefix@\home\youruser\azerothcore\data"
        #        Default:     "."

        DataDir = "."

        #
        ###################################################################################################

        ###################################################################################################
        # CONSOLE
        #
        #    Console.Enable
        #        Description: Enable console.
        #        Default:     1 - (Enabled)
        #                     0 - (Disabled)

        Console.Enable = 1

        #
        #    PlayerLimit
        #        Description: Maximum number of players.
        #        Default:     1000 - (Enabled)
        #                     1+   - (Enabled)
        #                     0    - (Disabled, No limit)

        PlayerLimit = 1000

        #
        #    GM.LoginState
        #        Description: GM mode at login.
        #        Default:     2 - (Last save state)
        #                     0 - (Disable)
        #                     1 - (Enable)

        GM.LoginState = 2

        #
        ###################################################################################################
        """;

    [Fact]
    public void Parse_ExtractsDescriptionAndSection()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        var dataDir = entries.Single(e => e.Key == "DataDir");
        Assert.Equal("Data directory setting.", dataDir.Description);
        Assert.Equal("DIRECTORIES", dataDir.Section);
    }

    [Fact]
    public void Parse_QuotedValue_KeepsQuotesInCurrentValue()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        var dataDir = entries.Single(e => e.Key == "DataDir");
        Assert.Equal("\".\"", dataDir.CurrentValue);
    }

    [Fact]
    public void Parse_SimpleBoolean_IsFlaggedAsToggle()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        var consoleEnable = entries.Single(e => e.Key == "Console.Enable");
        Assert.True(consoleEnable.IsToggle);
        Assert.Equal("1", consoleEnable.CurrentValue);
        Assert.Equal(2, consoleEnable.Defaults.Count);
    }

    [Fact]
    public void Parse_MultiOptionEnumWithNonBooleanValue_IsNotFlaggedAsToggle()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        var gmLoginState = entries.Single(e => e.Key == "GM.LoginState");
        Assert.False(gmLoginState.IsToggle);
        Assert.Equal(3, gmLoginState.Defaults.Count);
    }

    [Fact]
    public void Parse_NumericNonBooleanKey_IsNotFlaggedAsToggle()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        var playerLimit = entries.Single(e => e.Key == "PlayerLimit");
        Assert.False(playerLimit.IsToggle);
    }

    [Fact]
    public void Parse_DefaultOptionsIncludeLabels()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        var consoleEnable = entries.Single(e => e.Key == "Console.Enable");
        Assert.Contains(consoleEnable.Defaults, d => d.Value == "1" && d.Label == "Enabled");
        Assert.Contains(consoleEnable.Defaults, d => d.Value == "0" && d.Label == "Disabled");
    }

    [Fact]
    public void Parse_ReturnsAllFourEntries()
    {
        var entries = WorldserverConfigParser.Parse(Fixture);

        Assert.Equal(4, entries.Count);
    }

    [Fact]
    public void Parse_SharedCommentBlock_AppliesDescriptionToEachNamedKey()
    {
        const string fixture = """
            #
            #    LoginDatabase.WorkerThreads
            #    WorldDatabase.WorkerThreads
            #        Description: The amount of worker threads spawned to handle async statements.
            #        Default:     1 - (LoginDatabase.WorkerThreads)
            #                     1 - (WorldDatabase.WorkerThreads)

            LoginDatabase.WorkerThreads = 1
            WorldDatabase.WorkerThreads = 2
            """;

        var entries = WorldserverConfigParser.Parse(fixture);

        Assert.Equal(2, entries.Count);
        Assert.All(entries, e => Assert.Equal(
            "The amount of worker threads spawned to handle async statements.", e.Description));
        Assert.Equal("1", entries[0].CurrentValue);
        Assert.Equal("2", entries[1].CurrentValue);
    }

    [Fact]
    public void Parse_SectionIndexBlock_DoesNotBleedIntoLaterKeys()
    {
        // Regression test: the file's own "SECTION INDEX" comment block lists many
        // single-word section names (e.g. "#    CONSOLE") that look exactly like the
        // "#    KeyName" header lines used for shared comment blocks, but are never
        // followed by a Description:/Default: — they must not be treated as pending
        // key names that block later comment blocks from ever clearing.
        const string fixture = """
            ###################################################################################################
            # SECTION INDEX
            #
            #    DIRECTORIES
            #    CONSOLE
            #    NETWORK
            #
            ###################################################################################################

            ###################################################################################################
            # CONSOLE
            #
            #    Console.Enable
            #        Description: Enable console.
            #        Default:     1 - (Enabled)
            #                     0 - (Disabled)

            Console.Enable = 1
            """;

        var entries = WorldserverConfigParser.Parse(fixture);

        var consoleEnable = entries.Single(e => e.Key == "Console.Enable");
        Assert.Equal("Enable console.", consoleEnable.Description);
        Assert.True(consoleEnable.IsToggle);
    }
}
