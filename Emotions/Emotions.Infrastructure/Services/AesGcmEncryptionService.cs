using System.Security.Cryptography;
using System.Text;
using Emotions.Application.Interfaces.Security;
using Microsoft.Extensions.Configuration;

namespace Emotions.Infrastructure.Services;

public sealed class AesGcmEncryptionService : IEncryptionService
{
    // 32-byte key (256-bit) from configuration/KeyVault
    private readonly byte[] _key;

    private const int NonceSizeBytes = 12; // 96-bit nonce (standard for GCM)
    private const int TagSizeBytes = 16; // 128-bit authentication tag

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

        // allocate buffers
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagSizeBytes];

        // ⚠️ use ctor that includes tag size to satisfy SYSLIB0053
        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Encrypt(nonce, plain, cipher, tag); // no AAD for now

        // layout: nonce | tag | ciphertext
        var packed = Convert.ToBase64String(nonce.Concat(tag).Concat(cipher).ToArray());
        return Task.FromResult(packed);
    }

    public Task<string> DecryptAsync(string ciphertext, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(ciphertext);

        var data = Convert.FromBase64String(ciphertext);
        if (data.Length < NonceSizeBytes + TagSizeBytes)
            throw new CryptographicException("Ciphertext too short.");

        var nonce = data.AsSpan(0, NonceSizeBytes);
        var tag = data.AsSpan(NonceSizeBytes, TagSizeBytes);
        var cipher = data.AsSpan(NonceSizeBytes + TagSizeBytes);

        var plain = new byte[cipher.Length];

        // ⚠️ use ctor that includes tag size to satisfy SYSLIB0053
        using var aes = new AesGcm(_key, TagSizeBytes);
        aes.Decrypt(nonce, cipher, tag, plain); // no AAD for now

        return Task.FromResult(Encoding.UTF8.GetString(plain));
    }
}