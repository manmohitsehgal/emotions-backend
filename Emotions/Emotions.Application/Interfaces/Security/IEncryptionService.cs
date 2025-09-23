namespace Emotions.Application.Interfaces.Security;

public interface IEncryptionService
{
    Task<string> EncryptAsync(string plaintext, CancellationToken ct = default);
    Task<string> DecryptAsync(string ciphertext, CancellationToken ct = default);
}