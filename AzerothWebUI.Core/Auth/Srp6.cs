using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace AzerothWebUI.Core.Auth;

/// <summary>
/// Generates AzerothCore-compatible SRP6 salt/verifier pairs for acore_auth.account.
/// Algorithm and constants verified against AzerothCore's own
/// src/common/Cryptography/Authentication/SRP6.cpp — do not change N/g or the
/// hash construction order without re-verifying against that source, since a
/// mismatch produces accounts that silently fail to authenticate.
/// </summary>
public static class Srp6
{
    private const int SaltLength = 32;
    private const int VerifierLength = 32;

    // Read as a normal big-endian hex string.
    private static readonly BigInteger N = BigInteger.Parse(
        "00894B645E89E1535BBDAD5B8B290650530801B18EBFBF5E8FAB3C82872A3E9BB7",
        System.Globalization.NumberStyles.HexNumber);

    private static readonly BigInteger G = 7;

    public static byte[] GenerateSalt() => RandomNumberGenerator.GetBytes(SaltLength);

    /// <summary>
    /// verifier = g ^ SHA1(salt || SHA1(username ':' password)) mod N,
    /// with the SHA1 digest read as a little-endian integer and the result
    /// serialized back to 32 little-endian bytes.
    /// </summary>
    public static byte[] ComputeVerifier(string username, string password, byte[] salt)
    {
        ArgumentNullException.ThrowIfNull(salt);
        if (salt.Length != SaltLength)
        {
            throw new ArgumentException($"Salt must be {SaltLength} bytes.", nameof(salt));
        }

        var identity = UpperLatin(username) + ":" + UpperLatin(password);
        var h1 = SHA1.HashData(Encoding.Latin1.GetBytes(identity));

        var saltedInput = new byte[salt.Length + h1.Length];
        salt.CopyTo(saltedInput, 0);
        h1.CopyTo(saltedInput, salt.Length);
        var h2 = SHA1.HashData(saltedInput);

        var x = new BigInteger(h2, isUnsigned: true, isBigEndian: false);
        var verifier = BigInteger.ModPow(G, x, N);

        return ToFixedLengthLittleEndian(verifier, VerifierLength);
    }

    private static byte[] ToFixedLengthLittleEndian(BigInteger value, int length)
    {
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: false);
        if (bytes.Length == length)
        {
            return bytes;
        }

        var result = new byte[length];
        Array.Copy(bytes, result, Math.Min(bytes.Length, length));
        return result;
    }

    /// <summary>
    /// Mirrors AzerothCore's Utf8ToUpperOnlyLatin: uppercases ASCII/Latin-1
    /// characters only, not a full Unicode-aware uppercase. AzerothCore's own
    /// SRP6.h explicitly requires callers to normalize this way before hashing.
    /// </summary>
    private static string UpperLatin(string value)
    {
        var chars = new char[value.Length];
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            chars[i] = c is >= 'a' and <= 'z' or >= 'à' and <= 'þ'
                ? char.ToUpperInvariant(c)
                : c;
        }

        return new string(chars);
    }
}
