using Emotions.Application.Interfaces.Transcription;

namespace Emotions.Infrastructure.Storage;

public sealed class LocalFileAttachmentReader : IAttachmentReader
{
    private readonly string _root;
    public LocalFileAttachmentReader(string root) => _root = root;

    public Task<Stream> OpenReadAsync(string blobKey, CancellationToken ct = default)
    {
        var full = Path.Combine(_root, blobKey.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full)) throw new FileNotFoundException("Blob not found", full);
        return Task.FromResult<Stream>(new FileStream(full, FileMode.Open, FileAccess.Read, FileShare.Read));
    }
}