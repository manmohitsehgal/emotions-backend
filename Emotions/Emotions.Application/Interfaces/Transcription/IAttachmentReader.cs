namespace Emotions.Application.Interfaces.Transcription;

/// <summary>
/// Abstraction over your storage layer; implement this by delegating to your existing blob/file service.
/// OpenReadAsync should return a seekable stream for the given blobKey.
/// </summary>
public interface IAttachmentReader
{
    Task<Stream> OpenReadAsync(string blobKey, CancellationToken ct = default);
}