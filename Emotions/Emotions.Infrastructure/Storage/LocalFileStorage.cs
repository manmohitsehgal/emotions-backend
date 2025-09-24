using Emotions.Application.Storage;

namespace Emotions.Infrastructure.Storage;

public sealed class LocalFileStorage : IBlobStorage
{
    private readonly string _root;


    public LocalFileStorage(StorageOptions opt)
    {
        _root = Path.GetFullPath(opt.LocalRoot);
        Directory.CreateDirectory(_root);
    }

    public Task<PresignedUpload> CreatePresignedUploadAsync(string blobKey, string contentType, long contentLength,
        string? md5)
    {
        var url = $"/api/uploads/{Uri.EscapeDataString(blobKey)}";
        var headers = new Dictionary<string, string> { { "Content-Type", contentType } };
        if (!string.IsNullOrWhiteSpace(md5)) headers["Content-MD5"] = md5!;
        return Task.FromResult(new PresignedUpload(blobKey, url, "POST", headers));
    }

    public Task<bool> BlobExistsAsync(string blobKey) => Task.FromResult(File.Exists(Abs(blobKey)));

    public Task<BlobProps?> GetPropertiesAsync(string blobKey)
    {
        var f = Abs(blobKey);
        if (!File.Exists(f)) return Task.FromResult<BlobProps?>(null);
        var fi = new FileInfo(f);
        return Task.FromResult<BlobProps?>(new BlobProps(fi.Length, null, null));
    }

    public Task PromoteAsync(string sourceKey, string destKey)
    {
        var src = Abs(sourceKey);
        var dst = Abs(destKey);
        Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
        if (File.Exists(dst)) File.Delete(dst);
        File.Move(src, dst);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string blobKey)
    {
        var p = Abs(blobKey);
        if (File.Exists(p)) File.Delete(p);
        return Task.CompletedTask;
    }

    public string GetPublicUrl(string blobKey) => $"/dev-files/{Uri.EscapeDataString(blobKey)}";

    private string Abs(string key)
    {
        var abs = Path.GetFullPath(Path.Combine(_root, key));
        if (!abs.StartsWith(_root)) throw new InvalidOperationException("Invalid path");
        return abs;
    }

    public string GetReadSasUrl(string blobKey, TimeSpan ttl)
    {
        // dev-only static server; no SAS. This is fine for dev.
        return GetPublicUrl(blobKey);
    }
}