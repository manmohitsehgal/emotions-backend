namespace Emotions.Infrastructure.Options
{
    public sealed class AzureSpeechOptions
    {
        public string Key { get; set; } = string.Empty;
        public string Region { get; set; } = "eastus";
        public string DefaultLocale { get; set; } = "en-US";
        public int MaxSessionSeconds { get; set; } = 900;
        public int MaxPendingChunks { get; set; } = 200;
        public int SampleRate { get; set; } = 16000;
        public string Encoding { get; set; } = "pcm16";
        public int Channels { get; set; } = 1;
    }
}