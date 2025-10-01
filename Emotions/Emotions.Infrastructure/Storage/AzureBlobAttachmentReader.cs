using Azure.Storage.Blobs;
using Emotions.Application.Interfaces.Transcription;

namespace Emotions.Infrastructure.Storage;

public sealed class AzureBlobAttachmentReader : IAttachmentReader
{
    private readonly BlobServiceClient _svc;
    private readonly string _defaultContainer;

    public AzureBlobAttachmentReader(string connectionString, string defaultContainer = "journal")
    {
        _svc = new BlobServiceClient(connectionString);
        _defaultContainer = defaultContainer;
    }

    public async Task<Stream> OpenReadAsync(string blobKey, CancellationToken ct = default)
    {
        string container = _defaultContainer;
        string name = blobKey;
        var idx = blobKey.IndexOf('/');
        if (idx > 0)
        {
            container = blobKey[..idx];
            name = blobKey[(idx + 1)..];
        }

        var blob = _svc.GetBlobContainerClient(container).GetBlobClient(name);
        var resp = await blob.DownloadStreamingAsync(cancellationToken: ct);
        return resp.Value.Content;
    }
}