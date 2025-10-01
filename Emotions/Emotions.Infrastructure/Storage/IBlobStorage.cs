namespace Emotions.Infrastructure.Storage;

public sealed record PresignedUpload(
    string BlobKey,
    string UploadUrl,
    string Method,
    IReadOnlyDictionary<string, string> Headers
);

public sealed record BlobProps(long ContentLength, string? ContentType, string? ContentMD5Base64);

public interface IBlobStorage
{
    Task<PresignedUpload> CreatePresignedUploadAsync(string blobKey, string contentType, long contentLength,
        string? contentMD5Base64);

    Task<bool> BlobExistsAsync(string blobKey);
    Task<BlobProps?> GetPropertiesAsync(string blobKey);
    Task PromoteAsync(string sourceKey, string destKey);
    Task DeleteAsync(string blobKey);
    string GetPublicUrl(string blobKey);
    string GetReadSasUrl(string blobKey, TimeSpan ttl); // For Azure; Local returns /dev-files path
}