using AzerothWebUI.Core.Config;

namespace AzerothWebUI.Core.Tests.Config;

public class WorldserverConfigWriterTests
{
    private const string Fixture = "# Comment\nConsole.Enable = 1\n\nPlayerLimit = 1000\n";

    [Fact]
    public void SetValue_ReplacesOnlyTargetKeysValue()
    {
        var result = WorldserverConfigWriter.SetValue(Fixture, "Console.Enable", "0");

        Assert.NotNull(result);
        Assert.Contains("Console.Enable = 0", result);
        Assert.Contains("PlayerLimit = 1000", result);
        Assert.Contains("# Comment", result);
    }

    [Fact]
    public void SetValue_PreservesQuotingStyle()
    {
        var quoted = "DataDir = \".\"\n";

        var result = WorldserverConfigWriter.SetValue(quoted, "DataDir", "/new/path");

        Assert.Equal("DataDir = \"/new/path\"\n", result);
    }

    [Fact]
    public void SetValue_UnknownKey_ReturnsNull()
    {
        var result = WorldserverConfigWriter.SetValue(Fixture, "DoesNotExist", "1");

        Assert.Null(result);
    }

    [Fact]
    public void SetValue_IgnoresKeysInsideComments()
    {
        var content = "# PlayerLimit = 999 (example)\nPlayerLimit = 1000\n";

        var result = WorldserverConfigWriter.SetValue(content, "PlayerLimit", "500");

        Assert.NotNull(result);
        Assert.Contains("# PlayerLimit = 999 (example)", result);
        Assert.Contains("PlayerLimit = 500", result);
        Assert.DoesNotContain("PlayerLimit = 1000", result);
    }
}
