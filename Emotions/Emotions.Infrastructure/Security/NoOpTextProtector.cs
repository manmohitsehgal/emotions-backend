using Emotions.Application.Interfaces.Security;

namespace Emotions.Infrastructure.Security;

public sealed class NoOpTextProtector : ITextProtector
{
    public string Protect(string plaintext) => plaintext ?? "";
    public string Unprotect(string ciphertext) => ciphertext ?? "";
}