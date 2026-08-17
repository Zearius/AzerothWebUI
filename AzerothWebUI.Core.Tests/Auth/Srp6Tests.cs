using AzerothWebUI.Core.Auth;

namespace AzerothWebUI.Core.Tests.Auth;

public class Srp6Tests
{
    [Fact]
    public void ComputeVerifier_MatchesIndependentlyComputedVector()
    {
        var salt = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var expectedVerifier = Convert.FromHexString(
            "388aa0fa07b5252db2f75c032b20fd11d63e417277a0e566cf79acf642ceb771");

        var verifier = Srp6.ComputeVerifier("TESTUSER", "TESTPASS", salt);

        Assert.Equal(expectedVerifier, verifier);
    }

    [Fact]
    public void ComputeVerifier_IsCaseInsensitiveOnInput()
    {
        var salt = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();

        var lower = Srp6.ComputeVerifier("testuser", "testpass", salt);
        var upper = Srp6.ComputeVerifier("TESTUSER", "TESTPASS", salt);

        Assert.Equal(upper, lower);
    }

    [Fact]
    public void ComputeVerifier_IsAlways32Bytes()
    {
        var salt = Srp6.GenerateSalt();

        var verifier = Srp6.ComputeVerifier("A", "B", salt);

        Assert.Equal(32, verifier.Length);
    }

    [Fact]
    public void GenerateSalt_Returns32Bytes()
    {
        var salt = Srp6.GenerateSalt();

        Assert.Equal(32, salt.Length);
    }

    [Fact]
    public void ComputeVerifier_DifferentSaltsProduceDifferentVerifiers()
    {
        var saltA = Srp6.GenerateSalt();
        var saltB = Srp6.GenerateSalt();

        var verifierA = Srp6.ComputeVerifier("SAMEUSER", "SAMEPASS", saltA);
        var verifierB = Srp6.ComputeVerifier("SAMEUSER", "SAMEPASS", saltB);

        Assert.NotEqual(verifierA, verifierB);
    }
}
