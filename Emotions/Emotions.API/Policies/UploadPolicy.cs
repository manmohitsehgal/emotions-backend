namespace Emotions.API.Policies;

public static class UploadPolicy
{
    public const long MaxAudio = 50L * 1024 * 1024;
    public const long MaxVideo = 250L * 1024 * 1024;
    public const long MaxImage = 25L * 1024 * 1024;

    public static bool IsAllowed(string type, string mime) => type switch
    {
        "audio" => mime is "audio/mp4" or "audio/mpeg" or "audio/aac",
        "video" => mime is "video/mp4",
        "image" => mime.StartsWith("image/"),
        _ => false
    };

    public static long MaxFor(string type) => type switch
    {
        "audio" => MaxAudio,
        "video" => MaxVideo,
        "image" => MaxImage,
        _ => 0
    };
}