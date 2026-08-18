using AzerothWebUI.Core.Domain;

namespace AzerothWebUI.Core.Tests.Domain;

public class PublicServerStatusParserTests
{
    [Fact]
    public void Parse_ExtractsSafeFieldsFromServerInfoOutput()
    {
        const string rawOutput = """
            AzerothCore rev. 092e9ba6ff8d+ 2026-08-07 07:08:05 -0700 (Playerbot branch) (Unix, RelWithDebInfo, Static)
            Connected players: 3. Characters in world: 1708.
            Connection peak: 5.
            Minimum account security level to log in: 0.
            Server uptime: 1 hour(s) 21 minute(s) 19 second(s)
            Update time diff: 15ms. Last 500 diffs summary:
            """;

        var status = PublicServerStatusParser.Parse(rawOutput);

        Assert.Equal(3, status.PlayersOnline);
        Assert.Equal(1708, status.CharactersInWorld);
        Assert.Equal("1 hour(s) 21 minute(s) 19 second(s)", status.Uptime);
    }

    [Fact]
    public void Parse_TrimsUptimeAtLineBreak()
    {
        const string rawOutput = "Connected players: 0. Characters in world: 0.\nServer uptime: 5 second(s)\nUpdate time diff: 1ms.";

        var status = PublicServerStatusParser.Parse(rawOutput);

        Assert.Equal("5 second(s)", status.Uptime);
    }

    [Fact]
    public void Parse_ReturnsDefaultsWhenFieldsMissing()
    {
        var status = PublicServerStatusParser.Parse("unexpected output");

        Assert.Equal(0, status.PlayersOnline);
        Assert.Equal(0, status.CharactersInWorld);
        Assert.Null(status.Uptime);
    }
}
