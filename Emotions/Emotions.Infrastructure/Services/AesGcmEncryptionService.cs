using System.Security.Cryptography;
using System.Text;
using Emotions.Application.Interfaces.Security;
using Microsoft.Extensions.Configuration;

namespace Emotions.Infrastructure.Services;

public sealed class AesGcmEncryptionService : IEncryptionService
{
    // 32-byte key (256-bit) from configuration/KeyVault
    private readonly byte[] _key;

    public AesGcmEncryptionService(IConfiguration cfg)
    {
        var b64 = cfg["Encryption:ContentKeyBase64"]
                  ?? throw new InvalidOperationException("Missing Encryption:ContentKeyBase64");
        _key = Convert.FromBase64String(b64);
        if (_key.Length != 32) throw new InvalidOperationException("Key must be 32 bytes (256-bit).");
    }

    public Task<string> EncryptAsync(string plaintext, CancellationToken ct = default)
    {
        plaintext ??= string.Empty;
        using var aes = new AesGcm(_key);
        var nonce = RandomNumberGenerator.GetBytes(12); // 96-bit nonce
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plain, cipher, tag);

        // layout: nonce | tag | ciphertext (all base64)
        var packed = Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
        return Task.FromResult(packed);
    }

    public Task<string> DecryptAsync(string ciphertext, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);
        var data = Convert.FromBase64String(ciphertext);
        var nonce = data[..12];
        var tag = data[12..28];
        var cipher = data[28..];

        using var aes = new AesGcm(_key);
        var plain = new byte[cipher.Length];
        aes.Decrypt(nonce, cipher, tag, plain);

        return Task.FromResult(Encoding.UTF8.GetString(plain));
    }
}