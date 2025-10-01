namespace Emotions.Infrastructure.Storage;

public sealed class StorageOptions
{
    public string Provider { get; set; } = "Local"; // "Local" | "AzureBlob"
    public string LocalRoot { get; set; } = "App_Data/Uploads";
    public string? AzureConnectionString { get; set; }
    public string? AzureContainer { get; set; } = "journal";
    public int PresignMinutes { get; set; } = 30;
}