using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace Emotions.Infrastructure.Storage;

public sealed class AzureBlobStorage : IBlobStorage
{
    private readonly BlobContainerClient _container;
    private readonly StorageOptions _opt;


    public AzureBlobStorage(StorageOptions opt)
    {
        _opt = opt;
        var svc = new BlobServiceClient(opt.AzureConnectionString!);
        _container = svc.GetBlobContainerClient(opt.AzureContainer!);
        _container.CreateIfNotExists();
    }

    public Task<PresignedUpload> CreatePresignedUploadAsync(string blobKey, string contentType, long contentLength,
        string? contentMD5Base64)
    {
        var blob = _container.GetBlobClient(blobKey);
        var builder = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = blob.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresOn = DateTimeOffset.UtcNow.AddMinutes(_opt.PresignMinutes)
        };
        builder.SetPermissions(BlobSasPermissions.Create | BlobSasPermissions.Write);


        var sas = blob.GenerateSasUri(builder).ToString();
        var headers = new Dictionary<string, string>
        {
            { "x-ms-blob-type", "BlockBlob" },
            { "Content-Type", contentType }
        };
        if (!string.IsNullOrWhiteSpace(contentMD5Base64)) headers["Content-MD5"] = contentMD5Base64!;


        return Task.FromResult(new PresignedUpload(blobKey, sas, "PUT", headers));
    }

    public async Task<bool> BlobExistsAsync(string blobKey) =>
        (await _container.GetBlobClient(blobKey).ExistsAsync()).Value;

    public async Task<BlobProps?> GetPropertiesAsync(string blobKey)
    {
        var client = _container.GetBlobClient(blobKey);
        if (!(await client.ExistsAsync()).Value) return null;
        var p = await client.GetPropertiesAsync();
        var md5 = p.Value.ContentHash is null ? null : Convert.ToBase64String(p.Value.ContentHash);
        return new BlobProps(p.Value.ContentLength, p.Value.ContentType, md5);
    }

    public async Task PromoteAsync(string sourceKey, string destKey)
    {
        var src = _container.GetBlobClient(sourceKey);
        var dst = _container.GetBlobClient(destKey);
        await dst.StartCopyFromUriAsync(src.Uri);
        await src.DeleteIfExistsAsync();
    }

    public Task DeleteAsync(string blobKey) => _container.GetBlobClient(blobKey).DeleteIfExistsAsync();

    public string GetPublicUrl(string blobKey) => _container.GetBlobClient(blobKey).Uri.ToString();

    public string GetReadSasUrl(string blobKey, TimeSpan ttl)
    {
        var blob = _container.GetBlobClient(blobKey);
        var b = new BlobSasBuilder
        {
            BlobContainerName = _container.Name,
            BlobName = blob.Name,
            Resource = "b",
            StartsOn = DateTimeOffset.UtcNow.AddMinutes(-1),
            ExpiresOn = DateTimeOffset.UtcNow.Add(ttl)
        };
        b.SetPermissions(BlobSasPermissions.Read);
        return blob.GenerateSasUri(b).ToString();
    }
}