namespace Emotions.Application.DTOs;

public record PresignRequest(
    Guid EntryId,
    string Type,
    string FileName,
    string MimeType,
    long SizeBytes,
    string? ContentMD5Base64
);

public record PresignResponse(
    string blobKey,
    string uploadUrl,
    string method,
    IDictionary<string, string> headers
);

public record CommitRequest(
    Guid EntryId,
    string BlobKey,
    string Type,
    string? FileName,
    string? MimeType,
    long SizeBytes,
    int? DurationSec,
    int? Width,
    int? Height,
    string? ContentMD5Base64
);