// SPDX-License-Identifier: GPL-3.0-or-later
using System.Security.Cryptography;
using System.Text;
using Collective.Platform.Secrets;
using Xunit;

namespace Collective.Platform.Secrets.Tests;

public class DeviceKeyCipherTests
{
    private static readonly byte[] Key = new byte[32]; // fixed all-zero key is fine for layout tests

    [Fact]
    public void Round_trips()
    {
        var cipher = new DeviceKeyCipher("CAS1"u8);
        byte[] blob = cipher.Encrypt(Encoding.UTF8.GetBytes("hello"), Key);
        Assert.Equal("hello", Encoding.UTF8.GetString(cipher.Decrypt(blob, Key)));
    }

    [Fact]
    public void Blob_layout_matches_the_legacy_per_repo_format()
    {
        // Hand-build a blob exactly the way the three per-repo DeviceKeyCipher copies did:
        // magic(4) | nonce(12) | ciphertext | tag(16). If the shared cipher reads it, every
        // existing Auth/SSH/Wiki blob stays readable.
        byte[] plaintext = Encoding.UTF8.GetBytes("legacy");
        byte[] nonce = new byte[12]; // fixed nonce – fine for a fixture
        byte[] ct = new byte[plaintext.Length];
        byte[] tag = new byte[16];
        using (var aes = new AesGcm(Key, 16))
            aes.Encrypt(nonce, plaintext, ct, tag);
        byte[] blob = ["C"u8[0], "A"u8[0], "S"u8[0], "1"u8[0], .. nonce, .. ct, .. tag];

        var cipher = new DeviceKeyCipher("CAS1"u8);
        Assert.Equal(plaintext, cipher.Decrypt(blob, Key));
    }

    [Fact]
    public void Wrong_magic_throws()
    {
        byte[] blob = new DeviceKeyCipher("CAS1"u8).Encrypt("x"u8, Key);
        Assert.Throws<CryptographicException>(() => new DeviceKeyCipher("CSS1"u8).Decrypt(blob, Key));
    }

    [Fact]
    public void Tampered_blob_throws()
    {
        var cipher = new DeviceKeyCipher("CWK1"u8);
        byte[] blob = cipher.Encrypt("payload"u8, Key);
        blob[^1] ^= 0xFF;
        // AesGcm throws AuthenticationTagMismatchException, a subclass of CryptographicException.
        Assert.ThrowsAny<CryptographicException>(() => cipher.Decrypt(blob, Key));
    }

    [Fact]
    public void Short_blob_throws()
        => Assert.Throws<CryptographicException>(() => new DeviceKeyCipher("CAS1"u8).Decrypt(new byte[8], Key));

    [Theory]
    [InlineData(16)]
    [InlineData(33)]
    public void Non_32_byte_key_throws(int len)
        => Assert.Throws<ArgumentException>(() => new DeviceKeyCipher("CAS1"u8).Encrypt("x"u8, new byte[len]));

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    public void Magic_must_be_4_bytes(int len)
        => Assert.Throws<ArgumentException>(() => new DeviceKeyCipher(new byte[len]));
}
