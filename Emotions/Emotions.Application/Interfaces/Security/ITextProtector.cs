namespace Emotions.Application.Interfaces.Security;

public interface ITextProtector
{
    string Protect(string plaintext);
    string Unprotect(string ciphertext);
}